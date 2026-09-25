import { useEffect, useMemo, useState } from 'react'
import { Plus, Trash2, UserPlus } from 'lucide-react'
import { createVisit } from '../services/visitService'
import { createInvoice } from '../services/invoiceService'
import { listPatients } from '../services/patientService'
import { listDoctors } from '../services/doctorService'
import { listServicePrices } from '../services/servicePriceService'
import { toastError, toastInfo, toastSuccess } from '../lib/toast'
import { formatVnd } from '../lib/format'
import { ServiceCategory, type ServicePrice } from '../types/invoice'
import type { Patient } from '../types/patient'
import type { Doctor } from '../types/doctor'
import type { Visit, VisitServiceLineInput } from '../types/visit'
import { PageHeader } from './PageHeader'
import { PatientQuickCreateDialog } from './PatientQuickCreateDialog'
import { Combobox } from './Combobox'
import { ServiceMultiPicker } from './ServiceMultiPicker'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent } from '@/components/ui/card'

interface Row {
  doctorId: string
  servicePriceId: string
  reason: string
}

function defaultRow(): Row {
  return { doctorId: '', servicePriceId: '', reason: '' }
}

interface Props {
  /** Sau khi tạo lượt thành công. */
  onCreated: (visit: Visit) => void
  /** Quay lại (mặc định điều hướng ở page wrapper; đóng tab ở workspace Lễ tân). */
  onBack?: () => void
  /** Ẩn tiêu đề trang (khi nhúng trong tab đã có tiêu đề riêng). */
  hideHeader?: boolean
}

/**
 * Thân màn "Tiếp nhận" (walk-in, ADR 0017): chọn bệnh nhân + đăng ký NHIỀU dịch vụ khám (mỗi dòng =
 * một bác sĩ + dịch vụ + khung giờ) → tạo một lượt gom tất cả. Dùng làm tab "Tiếp nhận mới" trong
 * workspace Lễ tân (`/front-desk`, Epic 17) — không còn route riêng.
 */
export function VisitForm({ onCreated, onBack, hideHeader }: Props) {
  const [patientSearch, setPatientSearch] = useState('')
  const [patients, setPatients] = useState<Patient[]>([])
  const [patientId, setPatientId] = useState('')
  const [doctors, setDoctors] = useState<Doctor[]>([])
  const [services, setServices] = useState<ServicePrice[]>([])
  const [clsServices, setClsServices] = useState<ServicePrice[]>([])
  const [pickedCls, setPickedCls] = useState<string[]>([])
  const [note, setNote] = useState('')
  const [rows, setRows] = useState<Row[]>([defaultRow()])
  const [submitting, setSubmitting] = useState(false)
  const [quickCreateOpen, setQuickCreateOpen] = useState(false)

  useEffect(() => {
    void (async () => {
      try {
        const [d, s, cls] = await Promise.all([
          listDoctors({ page: 1, pageSize: 100 }),
          listServicePrices({ page: 1, pageSize: 100, category: ServiceCategory.Consultation }),
          listServicePrices({ page: 1, pageSize: 100, category: ServiceCategory.Paraclinical }),
        ])
        setDoctors(d.items)
        setServices(s.items)
        setClsServices(cls.items)
      } catch (err) {
        toastError(err)
      }
    })()
  }, [])

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const res = await listPatients({ page: 1, pageSize: 20, search: patientSearch.trim() || undefined })
        if (active) setPatients(res.items)
      } catch {
        // Bỏ qua lỗi tìm kiếm.
      }
    })()
    return () => {
      active = false
    }
  }, [patientSearch])

  const serviceMap = useMemo(() => new Map(services.map((s) => [s.id, s])), [services])
  const clsMap = useMemo(() => new Map(clsServices.map((s) => [s.id, s])), [clsServices])
  const consultTotal = rows.reduce((sum, r) => sum + (serviceMap.get(r.servicePriceId)?.unitPrice ?? 0), 0)
  const clsTotal = pickedCls.reduce((sum, id) => sum + (clsMap.get(id)?.unitPrice ?? 0), 0)
  const total = consultTotal + clsTotal
  const toggleCls = (id: string) =>
    setPickedCls((cur) => (cur.includes(id) ? cur.filter((x) => x !== id) : [...cur, id]))

  const updateRow = (idx: number, patch: Partial<Row>) =>
    setRows((cur) => cur.map((r, i) => (i === idx ? { ...r, ...patch } : r)))
  const removeRow = (idx: number) => setRows((cur) => cur.filter((_, i) => i !== idx))
  const addRow = () => setRows((cur) => [...cur, defaultRow()])

  const submit = async () => {
    if (!patientId) {
      toastInfo('Hãy chọn bệnh nhân.')
      return
    }
    if (rows.some((r) => !r.doctorId)) {
      toastInfo('Mỗi dịch vụ khám phải chọn bác sĩ.')
      return
    }
    setSubmitting(true)
    try {
      const servicesInput: VisitServiceLineInput[] = rows.map((r) => ({
        doctorId: r.doctorId,
        reason: r.reason.trim() || null,
        servicePriceId: r.servicePriceId || null,
      }))
      const visit = await createVisit({
        patientId,
        note: note.trim() || null,
        services: servicesInput,
        paraclinicalServiceIds: pickedCls,
      })
      toastSuccess(`Đã tạo lượt tiếp nhận ${visit.code}.`)

      // Lập hoá đơn gộp ngay (dịch vụ khám có giá + phiếu CLS vừa tạo) để panel thu tiền mở ra sau đó
      // đã sẵn sàng thu — khỏi phải qua "Lập hoá đơn" ở màn Chi tiết lượt như một thao tác rời.
      const billableAppointments = visit.appointments.filter((a) => a.servicePriceId && !a.invoicedAt)
      const labOrder = visit.labOrders[0]
      if (billableAppointments.length > 0 || labOrder) {
        try {
          await createInvoice({
            patientId,
            note: null,
            items: billableAppointments.map((a) => ({ servicePriceId: a.servicePriceId!, quantity: 1 })),
            appointmentIds: billableAppointments.map((a) => a.id),
            labOrderId: labOrder?.id ?? null,
          })
        } catch (err) {
          // Lượt đã tạo thành công — lỗi lập hoá đơn không nên chặn cả luồng, để lễ tân lập tay ở
          // màn Chi tiết lượt như phương án dự phòng (giữ nguyên đường cũ).
          toastError(err, 'Đã tạo lượt nhưng chưa lập được hoá đơn — vào chi tiết lượt để lập tay.')
        }
      }

      onCreated(visit)
    } catch (err) {
      toastError(err)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="flex flex-col gap-4">
      {!hideHeader && (
        <PageHeader
          title="Tiếp nhận bệnh nhân"
          description="Đăng ký một lượt tiếp nhận gồm một hoặc nhiều dịch vụ khám (nhiều bác sĩ/chuyên khoa)."
        />
      )}

      <Card>
        <CardContent className="flex flex-col gap-4 p-6">
          {/* Bệnh nhân */}
          <div className="grid gap-2">
            <Label>Bệnh nhân *</Label>
            <div className="flex gap-2">
              <Combobox
                className="flex-1"
                value={patientId}
                onValueChange={setPatientId}
                onSearchChange={setPatientSearch}
                options={patients.map((p) => ({ value: p.id, label: `${p.fullName} (${p.code})` }))}
                placeholder="— Chọn bệnh nhân —"
                searchPlaceholder="Tìm theo tên, mã…"
                emptyText="Không tìm thấy bệnh nhân."
              />
              <Button type="button" variant="outline" onClick={() => setQuickCreateOpen(true)}>
                <UserPlus className="size-4" />
                Bệnh nhân mới
              </Button>
            </div>
          </div>

          <PatientQuickCreateDialog
            open={quickCreateOpen}
            onOpenChange={setQuickCreateOpen}
            onCreated={(patient) => {
              setPatientId(patient.id)
              setPatientSearch(patient.fullName)
            }}
          />

          <div className="grid gap-2">
            <Label>Ghi chú tiếp nhận</Label>
            <Input placeholder="Tuỳ chọn" value={note} onChange={(e) => setNote(e.target.value)} />
          </div>
        </CardContent>
      </Card>

      {/* Các dịch vụ khám */}
      <div className="flex flex-col gap-3">
        {rows.map((row, idx) => (
          <Card key={idx}>
            <CardContent className="grid gap-3 p-4 md:grid-cols-2">
              <div className="grid gap-2">
                <Label>Bác sĩ *</Label>
                <Combobox
                  value={row.doctorId}
                  onValueChange={(v) => updateRow(idx, { doctorId: v })}
                  options={doctors.map((d) => ({
                    value: d.id,
                    label: d.fullName,
                    description: d.specialtyName ?? undefined,
                  }))}
                  placeholder="— Chọn bác sĩ —"
                  searchPlaceholder="Tìm bác sĩ…"
                  emptyText="Không tìm thấy bác sĩ."
                />
              </div>

              <div className="grid gap-2">
                <Label>Dịch vụ khám</Label>
                <Combobox
                  value={row.servicePriceId}
                  onValueChange={(v) => updateRow(idx, { servicePriceId: v })}
                  options={services.map((s) => ({
                    value: s.id,
                    label: s.name,
                    description: formatVnd(s.unitPrice),
                  }))}
                  placeholder="— Không gắn dịch vụ —"
                  searchPlaceholder="Tìm dịch vụ…"
                  emptyText="Không tìm thấy dịch vụ."
                />
              </div>

              <div className="grid gap-2 md:col-span-2">
                <Label>Lý do khám</Label>
                <div className="flex gap-2">
                  <Input
                    placeholder="Tuỳ chọn"
                    value={row.reason}
                    onChange={(e) => updateRow(idx, { reason: e.target.value })}
                  />
                  {rows.length > 1 && (
                    <Button
                      type="button"
                      variant="ghost"
                      className="text-destructive hover:text-destructive"
                      onClick={() => removeRow(idx)}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        ))}

        <div>
          <Button type="button" variant="outline" onClick={addRow}>
            <Plus className="size-4" />
            Thêm dịch vụ khám
          </Button>
        </div>
      </div>

      {/* Cận lâm sàng đăng ký ngay lúc tiếp nhận (tạo phiếu CLS walk-in gắn lượt) */}
      <Card>
        <CardContent className="flex flex-col gap-3 p-4">
          <Label>Cận lâm sàng (tuỳ chọn)</Label>
          <ServiceMultiPicker
            services={clsServices}
            picked={pickedCls}
            onToggle={toggleCls}
            emptyText="Chưa có dịch vụ cận lâm sàng trong bảng giá."
          />
        </CardContent>
      </Card>

      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          {onBack && (
            <Button type="button" variant="outline" onClick={onBack} disabled={submitting}>
              Quay lại
            </Button>
          )}
          <span className="text-sm text-muted-foreground">
            Tạm tính: <span className="font-semibold text-foreground">{formatVnd(total)}</span>
            {clsTotal > 0 && <span className="ml-1">(khám {formatVnd(consultTotal)} + CLS {formatVnd(clsTotal)})</span>}
          </span>
        </div>
        <Button type="button" onClick={() => void submit()} disabled={submitting}>
          Tạo lượt tiếp nhận ({rows.length} khám{pickedCls.length > 0 ? ` + ${pickedCls.length} CLS` : ''})
        </Button>
      </div>
    </section>
  )
}
