import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Clock, PhoneCall, Plus, Receipt, RefreshCw, Wallet, X } from 'lucide-react'
import { getVisit, listVisits } from '../services/visitService'
import { getInvoicesByVisit, payVisitInvoices } from '../services/invoiceService'
import { listQueue, transitionQueueTicket } from '../services/queueService'
import { useAuth } from '../store/auth'
import { canManageBilling, canManageQueue } from '../config/access'
import { toastError, toastSuccess } from '../lib/toast'
import { formatVnd } from '../lib/format'
import { VisitStatus, visitStatusLabels, type Visit, type VisitListItem } from '../types/visit'
import { QueueTicketStatus, type QueueTicket } from '../types/queue'
import type { PagedResult } from '../types/common'
import {
  PaymentMethod,
  paymentMethodLabels,
  type PaymentMethodValue,
  type VisitInvoices,
} from '../types/invoice'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { VisitStatusBadge, QueueTicketStatusBadge } from '../components/StatusBadge'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { VisitForm } from '../components/VisitForm'
import { useWorkspaceTabs } from '../components/workspace/useWorkspaceTabs'
import { WorkspaceTabs } from '../components/workspace/WorkspaceTabs'
import { WorkspaceSummaryBar } from '../components/workspace/WorkspaceSummaryBar'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
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

const PAGE_SIZE = 10
const ALL_STATUS = 'all'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

// Nội dung một tab mở trong workspace: thu tiền nhanh 1 lượt, hoặc tiếp đón lượt mới.
type FrontDeskTab = { kind: 'pay'; visitId: string } | { kind: 'new' }
const NEW_VISIT_KEY = 'new-visit'

function todayLocal(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset()
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 10)
}

/** Panel chi tiết một lượt (mở trong tab): tổng viện phí + thu tiền nhanh cả lượt. */
function VisitQuickPayPanel({ visitId, onChanged }: { visitId: string; onChanged: () => void }) {
  const [visit, setVisit] = useState<Visit | null>(null)
  const [invoices, setInvoices] = useState<VisitInvoices | null>(null)
  const [payMethod, setPayMethod] = useState<PaymentMethodValue>(PaymentMethod.Cash)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [v, inv] = await Promise.all([getVisit(visitId), getInvoicesByVisit(visitId)])
      setVisit(v)
      setInvoices(inv)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [visitId])

  useEffect(() => {
    void load()
  }, [load])

  const onPayAll = async () => {
    try {
      const result = await payVisitInvoices(visitId, payMethod)
      setInvoices(result)
      toastSuccess('Đã thu tiền toàn bộ hoá đơn còn nợ của lượt.')
      onChanged()
    } catch (err) {
      toastError(err)
    }
  }

  if (loading && !visit) return <p className="p-4 text-sm text-muted-foreground">Đang tải…</p>
  if (!visit) return <p className="p-4 text-sm text-muted-foreground">Không tìm thấy lượt.</p>

  return (
    <Card>
      <CardContent className="flex flex-col gap-4 p-4">
        {visit.appointments.length > 0 && (
          <div className="flex flex-wrap gap-2 border-b pb-3">
            {visit.appointments.map((a) => (
              <div
                key={a.id}
                className="flex items-center gap-2 rounded-md border bg-muted/40 px-3 py-1.5 text-sm"
              >
                <span className="text-xl font-bold tabular-nums">{a.queueNumber ?? '—'}</span>
                <span className="text-muted-foreground">
                  {a.doctorName ?? '—'}
                  {a.roomName ? ` · ${a.roomName}` : ''}
                </span>
              </div>
            ))}
          </div>
        )}
        <div className="grid grid-cols-3 gap-4 text-center">
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
        </div>

        {invoices && invoices.totalOutstanding > 0 && (
          <div className="flex flex-wrap items-center gap-2 border-t pt-3">
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
              trigger={
                <Button size="sm">
                  <Receipt className="size-4" />
                  Thu tiền cả lượt ({formatVnd(invoices.totalOutstanding)})
                </Button>
              }
              title="Thu tiền cả lượt?"
              description={`Thu ${formatVnd(invoices.totalOutstanding)} cho toàn bộ hoá đơn còn nợ bằng ${paymentMethodLabels[payMethod]}?`}
              confirmText="Thu tiền"
              onConfirm={() => void onPayAll()}
            />
          </div>
        )}
        {invoices && invoices.totalOutstanding === 0 && (
          <p className="border-t pt-3 text-sm text-muted-foreground">Lượt này không còn nợ.</p>
        )}

        <Button asChild size="sm" variant="ghost" className="self-start">
          <Link to={`/visits/${visitId}`}>Xem đầy đủ lượt tiếp đón →</Link>
        </Button>
      </CardContent>
    </Card>
  )
}

/**
 * Workspace Lễ tân (Epic 17, UX-03; gộp `/visits` + `/visits/new` vào đây): tiếp đón mới + lượt
 * tiếp đón (mặc định hôm nay/đang mở, có thể lọc lại để tra toàn bộ lịch sử) + hàng đợi + thu tiền
 * nhanh trên 1 màn, thay cho việc chuyển qua lại nhiều trang riêng (điểm nghẽn ghi nhận ở UX-01).
 */
export default function FrontDeskPage() {
  const { user } = useAuth()
  const billing = canManageBilling(user?.role)
  const queueManage = canManageQueue(user?.role)

  const [visitDate, setVisitDate] = useState(todayLocal())
  const [visitStatus, setVisitStatus] = useState<string>(String(VisitStatus.Open))
  const [visitPage, setVisitPage] = useState(1)
  const [visitData, setVisitData] = useState<PagedResult<VisitListItem> | null>(null)
  const [queue, setQueue] = useState<QueueTicket[]>([])
  const [openTodayCount, setOpenTodayCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const { tabs, active, setActive, openTab, closeTab } = useWorkspaceTabs<FrontDeskTab>()

  const hasVisitFilter = visitDate !== todayLocal() || visitStatus !== String(VisitStatus.Open)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const today = todayLocal()
      const [v, q, openToday] = await Promise.all([
        listVisits({
          page: visitPage,
          pageSize: PAGE_SIZE,
          date: visitDate || undefined,
          status: visitStatus === ALL_STATUS ? undefined : (Number(visitStatus) as VisitListItem['status']),
        }),
        listQueue({ date: today }),
        // Đếm riêng, không phụ thuộc bộ lọc bảng bên dưới — để thẻ tóm tắt luôn phản ánh đúng "hôm nay".
        listVisits({ page: 1, pageSize: 1, date: today, status: VisitStatus.Open }),
      ])
      setVisitData(v)
      setQueue(q)
      setOpenTodayCount(openToday.totalCount)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [visitPage, visitDate, visitStatus])

  useEffect(() => {
    void load()
  }, [load])

  const onCall = async (t: QueueTicket) => {
    try {
      await transitionQueueTicket(t.id, 'call')
      toastSuccess('Đã gọi số.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const waiting = queue.filter(
    (t) => t.status === QueueTicketStatus.Waiting || t.status === QueueTicketStatus.Called,
  )

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title="Lễ tân — Một màn"
        description="Lượt tiếp đón đang mở, hàng đợi & thu tiền nhanh trong ca — thí điểm workspace theo vai trò (UX-03)"
        actions={
          <div className="flex gap-2">
            <Button
              variant="outline"
              onClick={() => openTab(NEW_VISIT_KEY, 'Tiếp đón mới', { kind: 'new' })}
            >
              <Plus className="size-4" />
              Tiếp đón mới
            </Button>
            <Button variant="outline" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={loading ? 'size-4 animate-spin' : 'size-4'} />
              Làm mới
            </Button>
          </div>
        }
      />

      <WorkspaceSummaryBar
        items={[
          { icon: Clock, label: 'Đang chờ/đã gọi', value: String(waiting.length) },
          { icon: Wallet, label: 'Lượt đang mở hôm nay', value: String(openTodayCount) },
        ]}
      />

      {queueManage && (
        <Card>
          <CardContent className="p-0">
            <div className="flex items-center justify-between px-4 pt-4">
              <h3 className="font-semibold">Hàng đợi hôm nay ({waiting.length} đang chờ/đã gọi)</h3>
              <Button asChild size="sm" variant="ghost">
                <Link to="/queue">Xem đầy đủ →</Link>
              </Button>
            </div>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-16">Số</TableHead>
                  <TableHead>Bệnh nhân</TableHead>
                  <TableHead>Trạng thái</TableHead>
                  <TableHead className="text-right">Thao tác</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {waiting.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={4} className="h-16 text-center text-muted-foreground">
                      Không có ai đang chờ/đã gọi.
                    </TableCell>
                  </TableRow>
                )}
                {waiting.map((t) => (
                  <TableRow key={t.id}>
                    <TableCell className="text-lg font-semibold tabular-nums">{t.number}</TableCell>
                    <TableCell>{t.patientName ?? '—'}</TableCell>
                    <TableCell>
                      <QueueTicketStatusBadge status={t.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      {t.status === QueueTicketStatus.Waiting && (
                        <Button size="sm" variant="outline" onClick={() => void onCall(t)}>
                          <PhoneCall className="size-4" />
                          Gọi
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent className="p-0">
          <div className="flex flex-wrap items-center justify-between gap-3 px-4 pt-4">
            <h3 className="font-semibold">Lượt tiếp đón</h3>
            <div className="flex flex-wrap items-center gap-2">
              <Input
                type="date"
                className="w-auto"
                value={visitDate}
                onChange={(e) => {
                  setVisitPage(1)
                  setVisitDate(e.target.value)
                }}
              />
              <Select
                value={visitStatus === '' ? ALL_STATUS : visitStatus}
                onValueChange={(v) => {
                  setVisitPage(1)
                  setVisitStatus(v)
                }}
              >
                <SelectTrigger className="w-[160px]">
                  <SelectValue placeholder="Trạng thái" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={ALL_STATUS}>Tất cả trạng thái</SelectItem>
                  {Object.values(VisitStatus).map((s) => (
                    <SelectItem key={s} value={String(s)}>
                      {visitStatusLabels[s]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {hasVisitFilter && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    setVisitPage(1)
                    setVisitDate(todayLocal())
                    setVisitStatus(String(VisitStatus.Open))
                  }}
                >
                  <X className="size-4" />
                  Về hôm nay
                </Button>
              )}
            </div>
          </div>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã lượt</TableHead>
                <TableHead>Thời gian</TableHead>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead className="text-center">Số dịch vụ</TableHead>
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
              {!loading && visitData?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Không có lượt tiếp đón nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                visitData?.items.map((v) => (
                  <TableRow key={v.id}>
                    <TableCell className="font-medium">{v.code}</TableCell>
                    <TableCell className="whitespace-nowrap">{formatDate(v.createdAt)}</TableCell>
                    <TableCell>{v.patientName ?? '—'}</TableCell>
                    <TableCell className="text-center">{v.serviceCount}</TableCell>
                    <TableCell>
                      <VisitStatusBadge status={v.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      {billing ? (
                        <Button
                          size="sm"
                          onClick={() =>
                            openTab(v.id, `${v.code} · ${v.patientName ?? '—'}`, {
                              kind: 'pay',
                              visitId: v.id,
                            })
                          }
                        >
                          <Receipt className="size-4" />
                          {tabs.some((t) => t.key === v.id) ? 'Mở lại' : 'Thu tiền'}
                        </Button>
                      ) : (
                        <Button asChild size="sm" variant="ghost">
                          <Link to={`/visits/${v.id}`}>Chi tiết</Link>
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
          {visitData && (
            <div className="p-4 pt-0">
              <Pager
                page={visitData.page}
                totalPages={visitData.totalPages}
                totalCount={visitData.totalCount}
                onPageChange={setVisitPage}
              />
            </div>
          )}
        </CardContent>
      </Card>

      <WorkspaceTabs
        tabs={tabs}
        active={active}
        onActiveChange={setActive}
        onClose={closeTab}
        renderContent={(t) =>
          t.data.kind === 'new' ? (
            <VisitForm
              hideHeader
              onBack={() => closeTab(t.key)}
              onCreated={(visit) => {
                closeTab(t.key)
                void load()
                openTab(visit.id, `${visit.code} · ${visit.patientName ?? '—'}`, {
                  kind: 'pay',
                  visitId: visit.id,
                })
              }}
            />
          ) : (
            <VisitQuickPayPanel visitId={t.data.visitId} onChanged={() => void load()} />
          )
        }
      />
    </section>
  )
}
