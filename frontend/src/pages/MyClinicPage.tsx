import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Activity, CalendarClock, History, Play, RefreshCw, Stethoscope, TriangleAlert, Users } from 'lucide-react'
import { listAppointments, transitionAppointment } from '../services/appointmentService'
import { listQueue } from '../services/queueService'
import { useAuth } from '../store/auth'
import { toastError } from '../lib/toast'
import { AppointmentStatus, type Appointment } from '../types/appointment'
import type { QueueTicket } from '../types/queue'
import { PageHeader } from '../components/PageHeader'
import { AppointmentStatusBadge, QueueTicketStatusBadge } from '../components/StatusBadge'
import { EncounterForm } from '../components/EncounterForm'
import { useAutoRefresh } from '../hooks/useAutoRefresh'
import { useWorkspaceTabs } from '../components/workspace/useWorkspaceTabs'
import { WorkspaceTabs } from '../components/workspace/WorkspaceTabs'
import { WorkspaceSummaryBar } from '../components/workspace/WorkspaceSummaryBar'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

// Các trạng thái thuộc "phòng khám của tôi": đã tiếp nhận hoặc đang khám.
const CLINIC_STATUSES: number[] = [AppointmentStatus.CheckedIn, AppointmentStatus.InProgress]

function todayLocal(): string {
  return dateKeyLocal(new Date())
}

/** Ngày (yyyy-MM-dd) theo giờ địa phương của một thời điểm — dùng để so trùng ngày với `todayLocal()`. */
function dateKeyLocal(d: Date | string): string {
  const date = typeof d === 'string' ? new Date(d) : d
  const offset = date.getTimezoneOffset()
  return new Date(date.getTime() - offset * 60_000).toISOString().slice(0, 10)
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** Tên bệnh nhân kèm lối tắt xem lịch sử khám — dùng chung cho cả hai bảng bên dưới. */
function PatientCell({ patientId, patientName }: { patientId: string; patientName: string | null }) {
  return (
    <div className="flex items-center gap-2">
      <span>{patientName ?? '—'}</span>
      <Button asChild size="icon" variant="ghost" className="size-6 text-muted-foreground">
        <Link to={`/patients/${patientId}/encounters`} title="Lịch sử khám">
          <History className="size-3.5" />
        </Link>
      </Button>
    </div>
  )
}

/**
 * Khám bệnh (Epic 17, UX-05): danh sách bệnh nhân đang chờ/đang khám của chính bác sĩ
 * (lọc doctorId, ADR 0009). Bác sĩ tự "Bắt đầu khám" (CheckedIn → InProgress, action `start` sẵn có
 * ở Lịch khám) ngay tại đây. Kể từ khi khép kín vai trò Điều dưỡng, server chặn "start" nếu lịch khám
 * chưa được đo sinh hiệu (Appointment.VitalsRequired) — nút ở đây tự vô hiệu hoá trước khi bác sĩ
 * bấm nhầm, dựa trên `hasVitals` của vé hàng đợi (ADR 0019) nối theo lịch khám. Có thêm mục lịch hôm
 * nay còn lại để nắm ca làm, và lối tắt xem lịch sử khám mỗi bệnh nhân. Mở nhiều phiếu khám song song
 * dạng tab; nháp lưu server-side nên đóng/mở lại tab (kể cả F5) vẫn nạp đúng.
 */
export default function MyClinicPage() {
  const { doctorId } = useAuth()
  const [items, setItems] = useState<Appointment[]>([])
  const [upcoming, setUpcoming] = useState<Appointment[]>([])
  const [ticketsByAppointment, setTicketsByAppointment] = useState<Record<string, QueueTicket>>({})
  const [loading, setLoading] = useState(false)
  const { tabs, active, setActive, openTab, closeTab } = useWorkspaceTabs<Appointment>()

  const load = useCallback(async (opts?: { silent?: boolean }) => {
    if (!doctorId) return
    if (!opts?.silent) setLoading(true)
    try {
      const [result, tickets] = await Promise.all([
        listAppointments({ page: 1, pageSize: 100, doctorId }),
        listQueue({ date: todayLocal(), doctorId }),
      ])
      const clinic = result.items
        .filter((a) => CLINIC_STATUSES.includes(a.status))
        .sort((x, y) => x.startTime.localeCompare(y.startTime))
      const today = todayLocal()
      const rest = result.items
        .filter((a) => a.status === AppointmentStatus.Scheduled && dateKeyLocal(a.startTime) === today)
        .sort((x, y) => x.startTime.localeCompare(y.startTime))
      setItems(clinic)
      setUpcoming(rest)
      const byAppointment: Record<string, QueueTicket> = {}
      for (const t of tickets) {
        if (t.appointmentId) byAppointment[t.appointmentId] = t
      }
      setTicketsByAppointment(byAppointment)
    } catch (err) {
      if (!opts?.silent) toastError(err)
    } finally {
      if (!opts?.silent) setLoading(false)
    }
  }, [doctorId])

  useEffect(() => {
    void load()
  }, [load])

  // Lễ tân/Điều dưỡng tiếp nhận ở màn khác — tự làm mới hàng đợi để bác sĩ thấy ngay, không cần F5.
  useAutoRefresh(() => void load({ silent: true }))

  // "Bắt đầu khám" tự phục vụ: bác sĩ chuyển thẳng CheckedIn → InProgress (như ở Lịch khám),
  // không cần chờ Lễ tân/Điều dưỡng thao tác ở màn khác trước.
  const onStartExam = async (a: Appointment) => {
    try {
      await transitionAppointment(a.id, 'start')
      openTab(a.id, a.patientName ?? '—', a)
      void load()
    } catch (err) {
      toastError(err)
      void load()
    }
  }

  // Bác sĩ chưa được gắn hồ sơ Doctor → không lọc được "của tôi".
  if (!doctorId) {
    return (
      <section>
        <PageHeader title="Khám bệnh" />
        <Card>
          <CardContent className="flex items-start gap-3 text-amber-800">
            <TriangleAlert className="mt-0.5 size-5 shrink-0" />
            <p>
              Tài khoản của bạn chưa được gắn với hồ sơ bác sĩ. Vui lòng nhờ quản trị viên liên
              kết tài khoản với hồ sơ bác sĩ để xem danh sách bệnh nhân của bạn.
            </p>
          </CardContent>
        </Card>
      </section>
    )
  }

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title="Khám bệnh"
        description="Bệnh nhân đang chờ và đang khám của bạn — mở nhiều phiếu song song (UX-05)"
        actions={
          <Button variant="outline" onClick={() => void load()} disabled={loading}>
            <RefreshCw className={loading ? 'size-4 animate-spin' : 'size-4'} />
            Làm mới
          </Button>
        }
      />

      <WorkspaceSummaryBar
        items={[
          {
            icon: Users,
            label: 'Đã tiếp nhận, chờ khám',
            value: String(items.filter((a) => a.status === AppointmentStatus.CheckedIn).length),
          },
          {
            icon: Stethoscope,
            label: 'Đang khám',
            value: String(items.filter((a) => a.status === AppointmentStatus.InProgress).length),
          },
          { icon: CalendarClock, label: 'Lịch hôm nay còn lại', value: String(upcoming.length) },
        ]}
      />

      <Card>
        <CardContent className="p-0">
          <div className="px-4 pt-4">
            <h3 className="font-semibold">Đang chờ / đang khám</h3>
          </div>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-16">Số</TableHead>
                <TableHead>Thời gian</TableHead>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead>Lý do</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Hiện không có bệnh nhân nào đang chờ/đang khám.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                items.map((a) => {
                  const ticket = ticketsByAppointment[a.id]
                  return (
                    <TableRow key={a.id}>
                      <TableCell className="text-lg font-semibold tabular-nums">
                        {ticket?.number ?? '—'}
                      </TableCell>
                      <TableCell className="whitespace-nowrap font-medium">
                        {formatTime(a.startTime)}
                      </TableCell>
                      <TableCell>
                        <PatientCell patientId={a.patientId} patientName={a.patientName} />
                      </TableCell>
                      <TableCell className="text-muted-foreground">{a.reason ?? '—'}</TableCell>
                      <TableCell>
                        {ticket ? (
                          <QueueTicketStatusBadge status={ticket.status} />
                        ) : (
                          <AppointmentStatusBadge status={a.status} />
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        {a.status === AppointmentStatus.InProgress ? (
                          <Button size="sm" onClick={() => openTab(a.id, a.patientName ?? '—', a)}>
                            <Stethoscope className="size-4" />
                            {tabs.some((t) => t.key === a.id) ? 'Mở lại' : 'Khám'}
                          </Button>
                        ) : ticket && !ticket.hasVitals ? (
                          <Button size="sm" variant="outline" disabled title="Cần điều dưỡng đo sinh hiệu trước">
                            <Activity className="size-4" />
                            Chờ đo sinh hiệu
                          </Button>
                        ) : (
                          <Button size="sm" onClick={() => void onStartExam(a)}>
                            <Play className="size-4" />
                            Bắt đầu khám
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  )
                })}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <div className="px-4 pt-4">
            <h3 className="font-semibold">Lịch hôm nay còn lại ({upcoming.length})</h3>
          </div>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Thời gian</TableHead>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead>Lý do</TableHead>
                <TableHead>Trạng thái</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {!loading && upcoming.length === 0 && (
                <TableRow>
                  <TableCell colSpan={4} className="h-16 text-center text-muted-foreground">
                    Không còn lịch nào khác trong hôm nay.
                  </TableCell>
                </TableRow>
              )}
              {upcoming.map((a) => (
                <TableRow key={a.id}>
                  <TableCell className="whitespace-nowrap font-medium">
                    {formatTime(a.startTime)}
                  </TableCell>
                  <TableCell>
                    <PatientCell patientId={a.patientId} patientName={a.patientName} />
                  </TableCell>
                  <TableCell className="text-muted-foreground">{a.reason ?? '—'}</TableCell>
                  <TableCell>
                    <AppointmentStatusBadge status={a.status} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <WorkspaceTabs
        tabs={tabs}
        active={active}
        onActiveChange={setActive}
        onClose={closeTab}
        renderContent={(t) => (
          <EncounterForm
            appointmentId={t.key}
            hideHeader
            onBack={() => closeTab(t.key)}
            onCompleted={() => {
              closeTab(t.key)
              void load()
            }}
          />
        )}
      />
    </section>
  )
}
