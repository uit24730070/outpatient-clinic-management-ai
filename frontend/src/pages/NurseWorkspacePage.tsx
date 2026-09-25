import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Activity, Check, CheckCheck, Clock, PhoneCall, Play, RefreshCw, SkipForward } from 'lucide-react'
import { listQueue, transitionQueueTicket, type QueueAction } from '../services/queueService'
import { toastError, toastSuccess } from '../lib/toast'
import { useAutoRefresh } from '../hooks/useAutoRefresh'
import { QueueTicketStatus, type QueueTicket } from '../types/queue'
import { PageHeader } from '../components/PageHeader'
import { QueueTicketStatusBadge } from '../components/StatusBadge'
import { VitalsFormDialog } from '../components/VitalsFormDialog'
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

function todayLocal(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset()
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 10)
}

// Hành động điều phối khả dụng theo trạng thái vé (khớp máy trạng thái ADR 0019).
const actionsByStatus: Record<number, { action: QueueAction; label: string; icon: typeof Play }[]> = {
  [QueueTicketStatus.Waiting]: [
    { action: 'call', label: 'Gọi', icon: PhoneCall },
    { action: 'skip', label: 'Bỏ qua', icon: SkipForward },
  ],
  [QueueTicketStatus.Called]: [
    { action: 'start', label: 'Bắt đầu', icon: Play },
    { action: 'skip', label: 'Bỏ qua', icon: SkipForward },
  ],
  [QueueTicketStatus.InProgress]: [{ action: 'done', label: 'Hoàn tất', icon: Check }],
  [QueueTicketStatus.Done]: [],
  [QueueTicketStatus.Skipped]: [],
}

/**
 * Workspace Điều dưỡng (Epic 17, UX-04): gộp hàng đợi hôm nay (gọi/bắt đầu/hoàn tất số) + đo sinh
 * hiệu ngay tại dòng — thay cho việc chuyển qua lại `/queue` · `/vitals`. Hai trang cũ vẫn giữ
 * nguyên; đây là bổ sung, cùng pattern với `/front-desk` (UX-03).
 */
export default function NurseWorkspacePage() {
  const [tickets, setTickets] = useState<QueueTicket[]>([])
  const [loading, setLoading] = useState(false)
  const [vitalsFor, setVitalsFor] = useState<QueueTicket | null>(null)

  const load = useCallback(async (opts?: { silent?: boolean }) => {
    if (!opts?.silent) setLoading(true)
    try {
      const result = await listQueue({ date: todayLocal() })
      setTickets(result)
    } catch (err) {
      if (!opts?.silent) toastError(err)
    } finally {
      if (!opts?.silent) setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  // Lễ tân đăng ký/bác sĩ chuyển trạng thái ở màn khác — tự làm mới hàng đợi hôm nay.
  useAutoRefresh(() => void load({ silent: true }))

  const onAction = async (t: QueueTicket, action: QueueAction) => {
    try {
      await transitionQueueTicket(t.id, action)
      toastSuccess('Đã cập nhật hàng đợi.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const active = tickets.filter(
    (t) => t.status !== QueueTicketStatus.Done && t.status !== QueueTicketStatus.Skipped,
  )

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title="Sinh hiệu & Hàng đợi"
        description="Hàng đợi hôm nay & đo sinh hiệu ngay tại dòng — thí điểm workspace theo vai trò (UX-04)"
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
            icon: Clock,
            label: 'Đang chờ',
            value: String(tickets.filter((t) => t.status === QueueTicketStatus.Waiting).length),
          },
          {
            icon: Activity,
            label: 'Đã gọi/đang khám',
            value: String(
              tickets.filter(
                (t) => t.status === QueueTicketStatus.Called || t.status === QueueTicketStatus.InProgress,
              ).length,
            ),
          },
          {
            icon: CheckCheck,
            label: 'Hoàn tất hôm nay',
            value: String(tickets.filter((t) => t.status === QueueTicketStatus.Done).length),
          },
        ]}
      />

      <Card>
        <CardContent className="p-0">
          <div className="flex items-center justify-between px-4 pt-4">
            <h3 className="font-semibold">Đang chờ/đang khám ({active.length})</h3>
            <Button asChild size="sm" variant="ghost">
              <Link to="/queue">Xem đầy đủ hàng đợi →</Link>
            </Button>
          </div>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-16">Số</TableHead>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead>Bác sĩ</TableHead>
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
              {!loading && active.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    Không có ai đang chờ/đang khám hôm nay.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                active.map((t) => (
                  <TableRow key={t.id}>
                    <TableCell className="text-lg font-semibold tabular-nums">{t.number}</TableCell>
                    <TableCell>{t.patientName ?? '—'}</TableCell>
                    <TableCell className="text-muted-foreground">{t.doctorName ?? '—'}</TableCell>
                    <TableCell>
                      <QueueTicketStatusBadge status={t.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-1.5">
                        {t.appointmentId && (
                          <Button size="sm" variant="outline" onClick={() => setVitalsFor(t)}>
                            <Activity className="size-4" />
                            Sinh hiệu
                          </Button>
                        )}
                        {actionsByStatus[t.status]?.map(({ action, label, icon: Icon }) => (
                          <Button
                            key={action}
                            size="sm"
                            variant={action === 'skip' ? 'ghost' : 'default'}
                            onClick={() => void onAction(t, action)}
                          >
                            <Icon className="size-4" />
                            {label}
                          </Button>
                        ))}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <VitalsFormDialog
        appointmentId={vitalsFor?.appointmentId ?? null}
        patientName={vitalsFor?.patientName}
        onOpenChange={(open) => !open && setVitalsFor(null)}
      />
    </section>
  )
}
