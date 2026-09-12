import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Trash2 } from 'lucide-react'
import {
  createInvoice,
  getInvoice,
  updateInvoice,
} from '../services/invoiceService'
import { listServicePrices } from '../services/servicePriceService'
import { listPatients, getPatient } from '../services/patientService'
import { getVisit } from '../services/visitService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import type { Patient } from '../types/patient'
import type { ServicePrice } from '../types/invoice'
import { InvoiceItemType } from '../types/invoice'
import type { VisitLabOrder } from '../types/visit'
import { AppointmentStatus } from '../types/appointment'
import { formatVnd } from '../lib/format'
import { PageHeader } from '../components/PageHeader'
import { Combobox } from '../components/Combobox'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const itemSchema = z.object({
  servicePriceId: z.string().min(1, 'Chọn dịch vụ'),
  quantity: z.number().min(1, 'SL ≥ 1'),
})
const schema = z.object({
  patientId: z.string().min(1, 'Vui lòng chọn bệnh nhân.'),
  note: z.string(),
  items: z.array(itemSchema).min(1, 'Cần ít nhất một dòng dịch vụ.'),
})
type FormValues = z.infer<typeof schema>

const emptyItem = () => ({ servicePriceId: '', quantity: 1 })
const NONE = 'none'

export default function InvoiceFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  // Chế độ tiếp đón: điều hướng từ một lượt khám kèm ?patientId=&appointmentId=
  // → khoá bệnh nhân + gắn hoá đơn vào lượt.
  const prefillPatientId = searchParams.get('patientId')
  const prefillAppointmentId = searchParams.get('appointmentId')
  const prefillVisitId = searchParams.get('visitId')
  const lockedPatient = !isEdit && Boolean(prefillPatientId)

  const [services, setServices] = useState<ServicePrice[]>([])
  const [patients, setPatients] = useState<Patient[]>([])
  const [patientSearch, setPatientSearch] = useState('')
  const [patientName, setPatientName] = useState('')
  const [loading, setLoading] = useState(true)
  // Gộp phí CLS chưa lập hoá đơn của cùng lượt (UX-03, ADR 0021) — Invoice chỉ gắn được 1 LabOrderId.
  const [visitLabOrders, setVisitLabOrders] = useState<VisitLabOrder[]>([])
  const [labOrderId, setLabOrderId] = useState('')
  // Dịch vụ khám của lượt chưa lập hoá đơn (UX-05) — dùng để suy ra appointmentIds lúc submit,
  // khớp đúng dòng nào còn lại trong bảng "Dòng dịch vụ" sau khi người dùng có thể đã sửa/xoá dòng.
  const [billableAppointments, setBillableAppointments] = useState<{ id: string; servicePriceId: string }[]>([])

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { patientId: '', note: '', items: [emptyItem()] },
  })
  const { register, handleSubmit, control, watch, setValue, reset, formState } = form
  const { fields, append, remove, replace } = useFieldArray({ control, name: 'items' })
  const items = watch('items')
  const patientId = watch('patientId')
  const errors = formState.errors

  // Nạp bảng giá dịch vụ (dùng cho các dòng) + dữ liệu hoá đơn khi sửa.
  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const svc = await listServicePrices({ page: 1, pageSize: 100 })
        if (active) setServices(svc.items)

        if (id) {
          const inv = await getInvoice(id)
          if (active) {
            setPatientName(inv.patientName ? `${inv.patientName}` : '—')
            const serviceLines = inv.items
              .filter((it) => it.itemType === InvoiceItemType.ServiceFee && it.referenceId)
              .map((it) => ({ servicePriceId: it.referenceId as string, quantity: it.quantity }))
            reset({
              patientId: inv.patientId,
              note: inv.note ?? '',
              items: serviceLines.length > 0 ? serviceLines : [emptyItem()],
            })
          }
        } else if (prefillPatientId) {
          // Tiếp đón: nạp tên bệnh nhân để hiển thị + đặt sẵn patientId (khoá field).
          const p = await getPatient(prefillPatientId)
          if (active) {
            setPatientName(`${p.fullName} (${p.code})`)
            setValue('patientId', p.id, { shouldValidate: true })
          }
        }
      } catch (err) {
        if (active) toastError(err)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => {
      active = false
    }
  }, [id, reset, prefillPatientId, setValue])

  // Lượt tiếp đón (tuỳ chọn, UX-05 — lập hoá đơn ở cấp Lượt): tự nạp sẵn dòng dịch vụ khám + phiếu CLS
  // chưa lập hoá đơn của lượt vào hoá đơn (thay vì phải chọn lại từ đầu) — dựa thẳng vào cờ
  // Appointment.invoicedAt (đáng tin cậy, không suy diễn qua Invoice.appointmentId neo).
  useEffect(() => {
    if (isEdit || !prefillVisitId) return
    let active = true
    void (async () => {
      try {
        const visit = await getVisit(prefillVisitId)
        if (!active) return

        const unbilledLabOrders = visit.labOrders.filter((o) => o.invoicedAt == null)
        setVisitLabOrders(unbilledLabOrders)
        if (unbilledLabOrders.length === 1) setLabOrderId(unbilledLabOrders[0].id)

        const unbilledAppointments = visit.appointments.filter(
          (a) =>
            a.status !== AppointmentStatus.Cancelled &&
            a.status !== AppointmentStatus.NoShow &&
            a.servicePriceId &&
            a.invoicedAt == null,
        )
        setBillableAppointments(
          unbilledAppointments.map((a) => ({ id: a.id, servicePriceId: a.servicePriceId as string })),
        )

        const quantityByService = new Map<string, number>()
        for (const a of unbilledAppointments) {
          const svcId = a.servicePriceId as string
          quantityByService.set(svcId, (quantityByService.get(svcId) ?? 0) + 1)
        }
        if (quantityByService.size > 0) {
          replace(Array.from(quantityByService, ([servicePriceId, quantity]) => ({ servicePriceId, quantity })))
        }
      } catch {
        // Không tải được lượt: vẫn lập hoá đơn bình thường, chỉ không tự nạp sẵn dòng/gộp.
      }
    })()
    return () => {
      active = false
    }
  }, [isEdit, prefillVisitId, replace])

  // Tìm kiếm bệnh nhân cho dropdown (chỉ khi tạo mới, chưa khoá bệnh nhân).
  useEffect(() => {
    if (isEdit || lockedPatient) return
    let active = true
    void (async () => {
      try {
        const result = await listPatients({
          page: 1,
          pageSize: 20,
          search: patientSearch.trim() || undefined,
        })
        if (active) setPatients(result.items)
      } catch {
        // Bỏ qua lỗi tìm kiếm; người dùng thử lại.
      }
    })()
    return () => {
      active = false
    }
  }, [isEdit, lockedPatient, patientSearch])

  const serviceById = useMemo(
    () => new Map(services.map((s) => [s.id, s])),
    [services],
  )

  const selectedLabOrder = visitLabOrders.find((o) => o.id === labOrderId)

  // Tổng tạm tính phía client (server sẽ snapshot + tính lại chính thức).
  const estimatedTotal =
    (items ?? []).reduce((sum, it) => {
      const price = serviceById.get(it.servicePriceId)?.unitPrice ?? 0
      return sum + price * (Number(it.quantity) || 0)
    }, 0) + (selectedLabOrder?.totalAmount ?? 0)

  const onSubmit = handleSubmit(async (values) => {
    const payload = {
      note: values.note.trim() || null,
      items: values.items.map((it) => ({
        servicePriceId: it.servicePriceId,
        quantity: Number(it.quantity),
      })),
    }
    try {
      if (isEdit && id) {
        await updateInvoice(id, payload)
        toastSuccess('Đã cập nhật hoá đơn.')
        navigate(`/invoices/${id}`)
      } else {
        // Suy appointmentIds từ các dòng CÒN LẠI trong bảng (người dùng có thể đã sửa/xoá dòng tự nạp
        // sẵn): mỗi dịch vụ khám ứng viên "tiêu" một suất số lượng cùng servicePriceId — hết suất thì
        // dịch vụ khám đó không được tính là thu trong hoá đơn này (không đánh dấu invoicedAt).
        const remainingQuantity = new Map<string, number>()
        for (const it of payload.items) {
          remainingQuantity.set(it.servicePriceId, (remainingQuantity.get(it.servicePriceId) ?? 0) + it.quantity)
        }
        const appointmentIds = prefillAppointmentId
          ? [prefillAppointmentId]
          : billableAppointments.reduce<string[]>((acc, a) => {
              const left = remainingQuantity.get(a.servicePriceId) ?? 0
              if (left > 0) {
                acc.push(a.id)
                remainingQuantity.set(a.servicePriceId, left - 1)
              }
              return acc
            }, [])

        const created = await createInvoice({
          patientId: values.patientId,
          appointmentIds: appointmentIds.length > 0 ? appointmentIds : undefined,
          labOrderId: labOrderId || null,
          ...payload,
        })
        toastSuccess('Đã tạo hoá đơn.')
        navigate(`/invoices/${created.id}`)
      }
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  return (
    <section className="mx-auto max-w-3xl">
      <PageHeader
        title={isEdit ? 'Sửa hoá đơn' : lockedPatient ? 'Lập hoá đơn dịch vụ' : 'Tạo hoá đơn lẻ'}
        description={
          prefillVisitId
            ? 'Đã tự nạp sẵn các dịch vụ khám + CLS chưa lập hoá đơn của lượt — rà lại rồi tạo hoá đơn.'
            : prefillAppointmentId
              ? 'Hoá đơn gắn với lượt tiếp đón — chọn dịch vụ từ bảng giá (khám/tái khám/CLS).'
              : undefined
        }
      />

      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <Card>
          <CardContent className="grid gap-4">
            <div className="grid gap-2">
              <Label>Bệnh nhân *</Label>
              {isEdit || lockedPatient ? (
                <Input value={patientName} disabled />
              ) : (
                <Combobox
                  value={patientId}
                  onValueChange={(v) => setValue('patientId', v, { shouldValidate: true })}
                  onSearchChange={setPatientSearch}
                  options={patients.map((p) => ({ value: p.id, label: `${p.fullName} (${p.code})` }))}
                  placeholder="— Chọn bệnh nhân —"
                  searchPlaceholder="Tìm theo tên, mã…"
                  emptyText="Không tìm thấy bệnh nhân."
                />
              )}
              {errors.patientId && (
                <p className="text-sm text-destructive">{errors.patientId.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="note">Ghi chú</Label>
              <Textarea id="note" rows={2} {...register('note')} />
            </div>
          </CardContent>
        </Card>

        {isEdit && (
          <p className="text-sm text-muted-foreground">
            Lưu ý: cập nhật sẽ thay toàn bộ dòng bằng các dịch vụ chọn dưới đây (đơn giá snapshot lại
            theo bảng giá hiện tại). Dòng tiền thuốc từ phiếu khám sẽ không còn.
          </p>
        )}

        {visitLabOrders.length > 0 && (
          <Card>
            <CardContent className="flex flex-col gap-2 p-4">
              <Label>Gộp phiếu cận lâm sàng chưa lập hoá đơn (tuỳ chọn)</Label>
              <Select value={labOrderId || NONE} onValueChange={(v) => setLabOrderId(v === NONE ? '' : v)}>
                <SelectTrigger>
                  <SelectValue placeholder="— Không gộp —" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>— Không gộp —</SelectItem>
                  {visitLabOrders.map((o) => (
                    <SelectItem key={o.id} value={o.id}>
                      {o.code} · {o.itemCount} mục · {formatVnd(o.totalAmount)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-muted-foreground">
                Thu chung 1 hoá đơn cho cả công khám lẫn CLS — chọn phiếu chỉ định cần gộp; thu tiền
                hoá đơn này cũng mở khoá nhập kết quả cho phiếu CLS đó.
              </p>
            </CardContent>
          </Card>
        )}

        <Card>
          <CardContent className="p-0">
            <div className="flex items-center justify-between px-6 py-3">
              <div>
                <h3 className="font-semibold">Dòng dịch vụ *</h3>
                {errors.items?.message && (
                  <p className="text-sm text-destructive">{errors.items.message}</p>
                )}
              </div>
              <Button type="button" size="sm" variant="outline" onClick={() => append(emptyItem())}>
                <Plus className="size-4" />
                Thêm dòng
              </Button>
            </div>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Dịch vụ</TableHead>
                  <TableHead className="w-[120px]">Đơn giá</TableHead>
                  <TableHead className="w-[100px]">Số lượng</TableHead>
                  <TableHead className="w-[130px] text-right">Thành tiền</TableHead>
                  <TableHead className="w-[60px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {fields.map((f, i) => {
                  const svc = serviceById.get(items[i]?.servicePriceId)
                  const line = (svc?.unitPrice ?? 0) * (Number(items[i]?.quantity) || 0)
                  return (
                    <TableRow key={f.id}>
                      <TableCell className="align-top">
                        <Combobox
                          value={items[i]?.servicePriceId ?? ''}
                          onValueChange={(v) =>
                            setValue(`items.${i}.servicePriceId`, v, { shouldValidate: true })
                          }
                          options={services.map((s) => ({
                            value: s.id,
                            label: s.name,
                            description: formatVnd(s.unitPrice),
                          }))}
                          placeholder="— Chọn dịch vụ —"
                          searchPlaceholder="Tìm dịch vụ…"
                          emptyText="Không tìm thấy dịch vụ."
                        />
                        {errors.items?.[i]?.servicePriceId && (
                          <p className="mt-1 text-xs text-destructive">
                            {errors.items[i]?.servicePriceId?.message}
                          </p>
                        )}
                      </TableCell>
                      <TableCell className="align-top tabular-nums text-muted-foreground">
                        {formatVnd(svc?.unitPrice ?? 0)}
                      </TableCell>
                      <TableCell className="align-top">
                        <Input
                          type="number"
                          min={1}
                          {...register(`items.${i}.quantity`, { valueAsNumber: true })}
                        />
                      </TableCell>
                      <TableCell className="align-top text-right tabular-nums">
                        {formatVnd(line)}
                      </TableCell>
                      <TableCell className="align-top">
                        {fields.length > 1 && (
                          <Button
                            type="button"
                            size="icon"
                            variant="ghost"
                            className="text-destructive hover:text-destructive"
                            onClick={() => remove(i)}
                          >
                            <Trash2 className="size-4" />
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
            <div className="flex items-center justify-end gap-4 px-6 py-3 text-sm">
              <span className="text-muted-foreground">
                Tạm tính{selectedLabOrder && ` (đã gồm CLS ${formatVnd(selectedLabOrder.totalAmount)})`}
              </span>
              <span className="text-lg font-semibold tabular-nums">{formatVnd(estimatedTotal)}</span>
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate(isEdit ? `/invoices/${id}` : '/invoices')}
            disabled={formState.isSubmitting}
          >
            Huỷ
          </Button>
          <Button type="submit" disabled={formState.isSubmitting}>
            {formState.isSubmitting ? 'Đang lưu…' : isEdit ? 'Lưu' : 'Tạo hoá đơn'}
          </Button>
        </div>
      </form>
    </section>
  )
}
