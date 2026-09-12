import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { Plus, Receipt, Stethoscope } from 'lucide-react'
import { addVisitService, cancelVisit, closeVisit, getVisit } from '../services/visitService'
import {
  createInvoiceFromEncounter,
  createInvoiceFromLabOrder,
  getInvoicesByVisit,
  payVisitInvoices,
} from '../services/invoiceService'
import { createWalkInLabOrder } from '../services/labOrderService'
import { getEncounterByAppointment } from '../services/encounterService'
import { listDoctors } from '../services/doctorService'
import { listServicePrices } from '../services/servicePriceService'
import { transitionAppointment, type AppointmentAction } from '../services/appointmentService'
import { useAuth } from '../store/auth'
import { canManageBilling } from '../config/access'
import { toastError, toastInfo, toastSuccess } from '../lib/toast'
import { formatVnd } from '../lib/format'
import { AppointmentStatus, type Appointment } from '../types/appointment'
import type { Encounter } from '../types/encounter'
import {
  PaymentMethod,
  paymentMethodLabels,
  ServiceCategory,
  type PaymentMethodValue,
  type ServicePrice,
  type VisitInvoices,
} from '../types/invoice'
import { VisitStatus, type Visit } from '../types/visit'
import type { Doctor } from '../types/doctor'
import { PageHeader } from '../components/PageHeader'
import {
  AppointmentStatusBadge,
  InvoiceStatusBadge,
  LabOrderStatusBadge,
  VisitStatusBadge,
} from '../components/StatusBadge'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Combobox } from '../components/Combobox'
import { ServiceMultiPicker } from '../components/ServiceMultiPicker'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
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

const actionsByStatus: Record<number, { action: AppointmentAction; label: string; danger?: boolean }[]> = {
  [AppointmentStatus.Scheduled]: [
    { action: 'check-in', label: 'Check-in' },
    { action: 'cancel', label: 'Huỷ', danger: true },
  ],
  [AppointmentStatus.CheckedIn]: [{ action: 'start', label: 'Bắt đầu khám' }],
  [AppointmentStatus.InProgress]: [{ action: 'complete', label: 'Hoàn tất' }],
  [AppointmentStatus.Completed]: [],
  [AppointmentStatus.Cancelled]: [],
  [AppointmentStatus.NoShow]: [],
}

function toLocalInput(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

export default function VisitDetailPage() {
  const { id = '' } = useParams()
  const { user, canManage, canRecordEncounter } = useAuth()
  const canBilling = canManageBilling(user?.role)

  const [visit, setVisit] = useState<Visit | null>(null)
  const [invoices, setInvoices] = useState<VisitInvoices | null>(null)
  // Phiếu khám theo từng dịch vụ khám đã hoàn tất — để biết có đơn thuốc chờ lập hoá đơn không.
  const [encountersByAppointment, setEncountersByAppointment] = useState<Record<string, Encounter>>({})
  const [payMethod, setPayMethod] = useState<PaymentMethodValue>(PaymentMethod.Cash)
  const [loading, setLoading] = useState(false)
  const [doctors, setDoctors] = useState<Doctor[]>([])
  const [services, setServices] = useState<ServicePrice[]>([])
  const [clsServices, setClsServices] = useState<ServicePrice[]>([])

  // Thêm dịch vụ (một dòng gọn).
  const [adding, setAdding] = useState(false)
  const [newDoctorId, setNewDoctorId] = useState('')
  const [newServiceId, setNewServiceId] = useState('')
  const [newStart, setNewStart] = useState(toLocalInput(new Date()))

  // Thêm phiếu CLS cho lượt đang mở (thay cho trang /lab/walk-in cũ).
  const [addingCls, setAddingCls] = useState(false)
  const [pickedCls, setPickedCls] = useState<string[]>([])
  const [submittingCls, setSubmittingCls] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      setVisit(await getVisit(id))
      if (canBilling) setInvoices(await getInvoicesByVisit(id))
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [id, canBilling])

  useEffect(() => {
    void load()
  }, [load])

  useEffect(() => {
    if (!canBilling || !visit) return
    const completed = visit.appointments.filter((a) => a.status === AppointmentStatus.Completed)
    if (completed.length === 0) return
    void (async () => {
      const pairs = await Promise.all(
        completed.map(async (a) => [a.id, await getEncounterByAppointment(a.id)] as const),
      )
      setEncountersByAppointment(
        Object.fromEntries(pairs.filter((p): p is [string, Encounter] => p[1] != null)),
      )
    })()
  }, [canBilling, visit])

  useEffect(() => {
    if (!canManage) return
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
      } catch {
        // Danh mục chỉ phục vụ thao tác thêm dịch vụ.
      }
    })()
  }, [canManage])

  const serviceMap = useMemo(() => new Map(services.map((s) => [s.id, s])), [services])

  const onAction = async (a: Appointment, action: AppointmentAction) => {
    try {
      await transitionAppointment(a.id, action)
      toastSuccess('Đã cập nhật trạng thái.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const onAddService = async () => {
    if (!newDoctorId) {
      toastInfo('Hãy chọn bác sĩ.')
      return
    }
    try {
      const start = new Date(newStart)
      const end = new Date(start)
      end.setMinutes(end.getMinutes() + 30)
      await addVisitService(id, {
        doctorId: newDoctorId,
        servicePriceId: newServiceId || null,
        startTime: start.toISOString(),
        endTime: end.toISOString(),
        reason: null,
      })
      toastSuccess('Đã thêm dịch vụ khám vào lượt.')
      setAdding(false)
      setNewDoctorId('')
      setNewServiceId('')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const toggleCls = (serviceId: string) =>
    setPickedCls((cur) => (cur.includes(serviceId) ? cur.filter((x) => x !== serviceId) : [...cur, serviceId]))

  const onAddCls = async () => {
    if (!visit || pickedCls.length === 0) return
    setSubmittingCls(true)
    try {
      await createWalkInLabOrder({
        patientId: visit.patientId,
        appointmentId: null,
        visitId: id,
        note: null,
        items: pickedCls.map((servicePriceId) => ({ servicePriceId })),
      })
      toastSuccess('Đã đăng ký phiếu CLS cho lượt.')
      setAddingCls(false)
      setPickedCls([])
      void load()
    } catch (err) {
      toastError(err)
    } finally {
      setSubmittingCls(false)
    }
  }

  const onBillLab = async (labOrderId: string) => {
    try {
      await createInvoiceFromLabOrder(labOrderId)
      toastSuccess('Đã lập hoá đơn phí cận lâm sàng.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const onBillMedication = async (encounterId: string) => {
    try {
      await createInvoiceFromEncounter(encounterId)
      toastSuccess('Đã lập hoá đơn thuốc.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const onPayAll = async () => {
    try {
      const result = await payVisitInvoices(id, payMethod)
      setInvoices(result)
      setVisit(await getVisit(id))
      toastSuccess('Đã thu tiền toàn bộ hoá đơn còn nợ của lượt.')
    } catch (err) {
      toastError(err)
    }
  }

  const onClose = async () => {
    try {
      await closeVisit(id)
      toastSuccess('Đã đóng lượt.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const onCancel = async () => {
    try {
      await cancelVisit(id)
      toastSuccess('Đã huỷ lượt.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  if (loading && !visit) return <p className="text-muted-foreground">Đang tải…</p>
  if (!visit) return <p className="text-muted-foreground">Không tìm thấy lượt tiếp đón.</p>

  const isOpen = visit.status === VisitStatus.Open
  // Còn gì để lập hoá đơn không (dịch vụ khám chưa lập + phiếu CLS chưa lập) — quyết định hiện nút
  // "Lập hoá đơn" ở cấp Lượt (UX-05): gộp mọi thứ còn nợ vào một hoá đơn, thay vì lập từng dịch vụ.
  const hasBillable =
    visit.appointments.some(
      (a) =>
        a.status !== AppointmentStatus.Cancelled &&
        a.status !== AppointmentStatus.NoShow &&
        a.servicePriceId &&
        a.invoicedAt == null,
    ) || visit.labOrders.some((o) => o.invoicedAt == null)

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title={`Lượt ${visit.code}`}
        description={visit.patientName ?? undefined}
        actions={
          <div className="flex items-center gap-2">
            <VisitStatusBadge status={visit.status} />
            {canBilling && hasBillable && (
              <Button asChild size="sm">
                <Link to={`/invoices/new?patientId=${visit.patientId}&visitId=${visit.id}`}>
                  <Receipt className="size-4" />
                  Lập hoá đơn
                </Link>
              </Button>
            )}
            {canManage && isOpen && (
              <Button size="sm" variant="outline" onClick={() => setAddingCls((cur) => !cur)}>
                Đăng ký CLS
              </Button>
            )}
            {canManage && isOpen && (
              <>
                <ConfirmDialog
                  trigger={<Button size="sm" variant="outline">Đóng lượt</Button>}
                  title="Đóng lượt tiếp đón?"
                  description="Xác nhận lượt khám đã hoàn tất."
                  confirmText="Đóng lượt"
                  onConfirm={() => void onClose()}
                />
                <ConfirmDialog
                  trigger={
                    <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive">
                      Huỷ lượt
                    </Button>
                  }
                  title="Huỷ lượt tiếp đón?"
                  description="Hành động này không thể hoàn tác."
                  confirmText="Huỷ lượt"
                  destructive
                  onConfirm={() => void onCancel()}
                />
              </>
            )}
          </div>
        }
      />

      {/* Tổng viện phí gom cả lượt */}
      <Card>
        <CardContent className="grid grid-cols-3 gap-4 p-6 text-center">
          <div>
            <p className="text-sm text-muted-foreground">Đã lập</p>
            <p className="text-lg font-semibold">{formatVnd(visit.totalBilled)}</p>
          </div>
          <div>
            <p className="text-sm text-muted-foreground">Đã thu</p>
            <p className="text-lg font-semibold text-green-700">{formatVnd(visit.totalPaid)}</p>
          </div>
          <div>
            <p className="text-sm text-muted-foreground">Còn nợ</p>
            <p className="text-lg font-semibold text-red-700">{formatVnd(visit.totalOutstanding)}</p>
          </div>
        </CardContent>
      </Card>

      {/* Hoá đơn của lượt + thu tiền cả lượt (chỉ thu ngân) */}
      {canBilling && invoices && (
        <Card>
          <CardContent className="flex flex-col gap-3 p-4">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <h3 className="font-semibold">Hoá đơn của lượt</h3>
              {invoices.totalOutstanding > 0 && (
                <div className="flex items-center gap-2">
                  <Select value={String(payMethod)} onValueChange={(v) => setPayMethod(Number(v) as PaymentMethodValue)}>
                    <SelectTrigger className="w-[150px]">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {Object.values(PaymentMethod).map((m) => (
                        <SelectItem key={m} value={String(m)}>
                          {paymentMethodLabels[m]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <ConfirmDialog
                    trigger={<Button size="sm">Thu tiền cả lượt ({formatVnd(invoices.totalOutstanding)})</Button>}
                    title="Thu tiền cả lượt?"
                    description={`Thu ${formatVnd(invoices.totalOutstanding)} cho toàn bộ hoá đơn còn nợ của lượt bằng ${paymentMethodLabels[payMethod]}?`}
                    confirmText="Thu tiền"
                    onConfirm={() => void onPayAll()}
                  />
                </div>
              )}
            </div>
            {invoices.invoices.length === 0 ? (
              <p className="text-sm text-muted-foreground">Chưa có hoá đơn nào cho lượt này.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Mã HĐ</TableHead>
                    <TableHead className="text-right">Tổng tiền</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {invoices.invoices.map((inv) => (
                    <TableRow key={inv.id}>
                      <TableCell className="font-medium">{inv.code}</TableCell>
                      <TableCell className="text-right">{formatVnd(inv.totalAmount)}</TableCell>
                      <TableCell>
                        <InvoiceStatusBadge status={inv.status} />
                      </TableCell>
                      <TableCell className="text-right">
                        <Button asChild size="sm" variant="ghost">
                          <Link to={`/invoices/${inv.id}`}>Chi tiết</Link>
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}

      {/* Danh sách dịch vụ khám trong lượt */}
      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-16">Số TT</TableHead>
                <TableHead>Giờ</TableHead>
                <TableHead>Bác sĩ / Phòng</TableHead>
                <TableHead>Dịch vụ khám</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {visit.appointments.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="h-20 text-center text-muted-foreground">
                    Chưa có dịch vụ khám.
                  </TableCell>
                </TableRow>
              )}
              {visit.appointments.map((a) => (
                <TableRow key={a.id}>
                  <TableCell className="text-lg font-semibold tabular-nums">{a.queueNumber ?? '—'}</TableCell>
                  <TableCell className="whitespace-nowrap font-medium">
                    {formatTime(a.startTime)}–{formatTime(a.endTime)}
                  </TableCell>
                  <TableCell>
                    {a.doctorName ?? '—'}
                    {a.roomName && <span className="text-muted-foreground"> · {a.roomName}</span>}
                  </TableCell>
                  <TableCell>
                    {a.serviceName ?? '—'}
                    {a.servicePrice != null && (
                      <span className="text-muted-foreground"> · {formatVnd(a.servicePrice)}</span>
                    )}
                  </TableCell>
                  <TableCell>
                    <AppointmentStatusBadge status={a.status} />
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-wrap items-center justify-end gap-1">
                      {canRecordEncounter && a.status === AppointmentStatus.InProgress && (
                        <Button asChild size="sm" variant="secondary">
                          <Link to={`/appointments/${a.id}/encounter`}>
                            <Stethoscope className="size-4" />
                            Khám
                          </Link>
                        </Button>
                      )}
                      {canBilling && a.invoicedAt != null && (
                        <span className="text-xs text-muted-foreground">Đã lập HĐ</span>
                      )}
                      {canBilling &&
                        (() => {
                          const encounter = encountersByAppointment[a.id]
                          if (!encounter || encounter.prescriptionItems.length === 0) return null
                          if (encounter.medicationInvoicedAt != null) {
                            return <span className="text-xs text-muted-foreground">Đã lập HĐ thuốc</span>
                          }
                          return (
                            <Button size="sm" variant="outline" onClick={() => void onBillMedication(encounter.id)}>
                              Lập HĐ thuốc
                            </Button>
                          )
                        })()}
                      {canManage &&
                        actionsByStatus[a.status].map((x) =>
                          x.danger ? (
                            <ConfirmDialog
                              key={x.action}
                              trigger={
                                <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive">
                                  {x.label}
                                </Button>
                              }
                              title={`${x.label} dịch vụ?`}
                              description={`Xác nhận ${x.label.toLowerCase()} dịch vụ khám của "${a.doctorName ?? ''}"?`}
                              confirmText={x.label}
                              destructive
                              onConfirm={() => void onAction(a, x.action)}
                            />
                          ) : (
                            <Button
                              key={x.action}
                              size="sm"
                              variant="outline"
                              onClick={() => void onAction(a, x.action)}
                            >
                              {x.label}
                            </Button>
                          ),
                        )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Cận lâm sàng của lượt */}
      {visit.labOrders.length > 0 && (
        <Card>
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Phiếu CLS</TableHead>
                  <TableHead className="text-center">Số mục</TableHead>
                  <TableHead className="text-right">Phí</TableHead>
                  <TableHead>Trạng thái</TableHead>
                  <TableHead className="text-right">Thao tác</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {visit.labOrders.map((o) => (
                  <TableRow key={o.id}>
                    <TableCell className="font-medium">{o.code}</TableCell>
                    <TableCell className="text-center">{o.itemCount}</TableCell>
                    <TableCell className="text-right">{formatVnd(o.totalAmount)}</TableCell>
                    <TableCell>
                      <LabOrderStatusBadge status={o.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex flex-wrap items-center justify-end gap-1">
                        <Button asChild size="sm" variant="ghost">
                          <Link to={`/lab-orders/${o.id}/print`} target="_blank">
                            In phiếu
                          </Link>
                        </Button>
                        {canBilling && o.invoicedAt == null && (
                          <Button size="sm" variant="outline" onClick={() => void onBillLab(o.id)}>
                            Lập HĐ CLS
                          </Button>
                        )}
                        {o.invoicedAt != null && (
                          <span className="text-xs text-muted-foreground">Đã lập HĐ</span>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      {/* Thêm phiếu CLS cho lượt đang mở (thay trang /lab/walk-in cũ) */}
      {canManage && isOpen && addingCls && (
        <Card>
          <CardContent className="flex flex-col gap-3 p-4">
            <Label>Đăng ký cận lâm sàng</Label>
            <ServiceMultiPicker services={clsServices} picked={pickedCls} onToggle={toggleCls} />
            <div className="flex gap-2">
              <Button onClick={() => void onAddCls()} disabled={submittingCls || pickedCls.length === 0}>
                Đăng ký ({pickedCls.length})
              </Button>
              <Button
                variant="ghost"
                onClick={() => {
                  setAddingCls(false)
                  setPickedCls([])
                }}
              >
                Huỷ
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {/* Thêm dịch vụ khám (khi lượt còn mở) */}
      {canManage && isOpen && (
        <Card>
          <CardContent className="p-4">
            {!adding ? (
              <Button variant="outline" onClick={() => setAdding(true)}>
                <Plus className="size-4" />
                Thêm dịch vụ khám
              </Button>
            ) : (
              <div className="grid gap-3 md:grid-cols-4 md:items-end">
                <div className="grid gap-2">
                  <Label>Bác sĩ *</Label>
                  <Combobox
                    value={newDoctorId}
                    onValueChange={setNewDoctorId}
                    options={doctors.map((d) => ({ value: d.id, label: d.fullName }))}
                    placeholder="— Chọn —"
                    searchPlaceholder="Tìm bác sĩ…"
                    emptyText="Không tìm thấy bác sĩ."
                  />
                </div>
                <div className="grid gap-2">
                  <Label>Dịch vụ</Label>
                  <Combobox
                    value={newServiceId}
                    onValueChange={setNewServiceId}
                    options={services.map((s) => ({
                      value: s.id,
                      label: s.name,
                      description: formatVnd(serviceMap.get(s.id)?.unitPrice ?? s.unitPrice),
                    }))}
                    placeholder="— Không gắn —"
                    searchPlaceholder="Tìm dịch vụ…"
                    emptyText="Không tìm thấy dịch vụ."
                  />
                </div>
                <div className="grid gap-2">
                  <Label>Giờ bắt đầu</Label>
                  <Input type="datetime-local" value={newStart} onChange={(e) => setNewStart(e.target.value)} />
                </div>
                <div className="flex gap-2">
                  <Button onClick={() => void onAddService()}>Thêm</Button>
                  <Button variant="ghost" onClick={() => setAdding(false)}>
                    Huỷ
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      )}
    </section>
  )
}
