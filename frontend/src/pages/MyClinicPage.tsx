import { useCallback, useEffect, useState } from 'react'
import { RefreshCw, Stethoscope, TriangleAlert, X } from 'lucide-react'
import { listAppointments } from '../services/appointmentService'
import { useAuth } from '../store/auth'
import { toastError } from '../lib/toast'
import { AppointmentStatus, type Appointment } from '../types/appointment'
import { PageHeader } from '../components/PageHeader'
import { AppointmentStatusBadge } from '../components/StatusBadge'
import { EncounterForm } from '../components/EncounterForm'
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'

// Các trạng thái thuộc "phòng khám của tôi": đã tiếp đón hoặc đang khám.
const CLINIC_STATUSES: number[] = [AppointmentStatus.CheckedIn, AppointmentStatus.InProgress]

function formatTime(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

interface OpenTab {
  appointmentId: string
  patientName: string
}

/**
 * Phòng khám của tôi (Bác sĩ) — màn khám đa tab (CLS-06): danh sách bệnh nhân đang chờ/đang khám
 * của chính bác sĩ (lọc doctorId, ADR 0009), mở song song nhiều phiếu khám dạng tab. Nháp lưu
 * server-side nên đóng/mở lại tab (kể cả F5) vẫn nạp đúng — không cần global state.
 */
export default function MyClinicPage() {
  const { doctorId } = useAuth()
  const [items, setItems] = useState<Appointment[]>([])
  const [loading, setLoading] = useState(false)
  const [tabs, setTabs] = useState<OpenTab[]>([])
  const [active, setActive] = useState<string>('')

  const load = useCallback(async () => {
    if (!doctorId) return
    setLoading(true)
    try {
      // Lấy lịch của bác sĩ rồi lọc trạng thái "đang trong phòng khám" phía client.
      const result = await listAppointments({ page: 1, pageSize: 100, doctorId })
      const clinic = result.items
        .filter((a) => CLINIC_STATUSES.includes(a.status))
        .sort((x, y) => x.startTime.localeCompare(y.startTime))
      setItems(clinic)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [doctorId])

  useEffect(() => {
    void load()
  }, [load])

  const openTab = (a: Appointment) => {
    setTabs((cur) =>
      cur.some((t) => t.appointmentId === a.id)
        ? cur
        : [...cur, { appointmentId: a.id, patientName: a.patientName ?? '—' }],
    )
    setActive(a.id)
  }

  const closeTab = (appointmentId: string) => {
    setTabs((cur) => {
      const next = cur.filter((t) => t.appointmentId !== appointmentId)
      setActive((curActive) =>
        curActive === appointmentId ? next[next.length - 1]?.appointmentId ?? '' : curActive,
      )
      return next
    })
  }

  // Bác sĩ chưa được gắn hồ sơ Doctor → không lọc được "của tôi".
  if (!doctorId) {
    return (
      <section>
        <PageHeader title="Phòng khám của tôi" />
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
        title="Phòng khám của tôi"
        description="Bệnh nhân đang chờ và đang khám của bạn — mở nhiều phiếu song song"
        actions={
          <Button variant="outline" onClick={() => void load()} disabled={loading}>
            <RefreshCw className={loading ? 'size-4 animate-spin' : 'size-4'} />
            Làm mới
          </Button>
        }
      />

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
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
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    Hiện không có bệnh nhân nào đang chờ/đang khám.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                items.map((a) => (
                  <TableRow key={a.id}>
                    <TableCell className="whitespace-nowrap font-medium">
                      {formatTime(a.startTime)}
                    </TableCell>
                    <TableCell>{a.patientName ?? '—'}</TableCell>
                    <TableCell className="text-muted-foreground">{a.reason ?? '—'}</TableCell>
                    <TableCell>
                      <AppointmentStatusBadge status={a.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      {a.status === AppointmentStatus.InProgress ? (
                        <Button size="sm" onClick={() => openTab(a)}>
                          <Stethoscope className="size-4" />
                          {tabs.some((t) => t.appointmentId === a.id) ? 'Mở lại' : 'Khám'}
                        </Button>
                      ) : (
                        <span className="text-sm text-muted-foreground">
                          Chờ tiếp đón bắt đầu khám
                        </span>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {tabs.length > 0 && (
        <Tabs value={active} onValueChange={setActive}>
          <TabsList className="h-auto flex-wrap">
            {tabs.map((t) => (
              <TabsTrigger key={t.appointmentId} value={t.appointmentId} className="gap-2">
                {t.patientName}
                <span
                  role="button"
                  tabIndex={-1}
                  aria-label="Đóng tab"
                  className="rounded p-0.5 hover:bg-muted-foreground/20"
                  onClick={(e) => {
                    e.stopPropagation()
                    closeTab(t.appointmentId)
                  }}
                >
                  <X className="size-3.5" />
                </span>
              </TabsTrigger>
            ))}
          </TabsList>
          {/* keep-mounted (forceMount) để không mất input khi chuyển tab. */}
          {tabs.map((t) => (
            <TabsContent key={t.appointmentId} value={t.appointmentId} forceMount className="data-[state=inactive]:hidden">
              <EncounterForm
                appointmentId={t.appointmentId}
                hideHeader
                onBack={() => closeTab(t.appointmentId)}
                onCompleted={() => {
                  closeTab(t.appointmentId)
                  void load()
                }}
              />
            </TabsContent>
          ))}
        </Tabs>
      )}
    </section>
  )
}
