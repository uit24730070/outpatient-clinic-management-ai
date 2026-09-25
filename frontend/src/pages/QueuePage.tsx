import { useCallback, useEffect, useMemo, useState } from 'react'
import { Plus, PhoneCall, Play, Check, SkipForward, Settings2, X, RefreshCw } from 'lucide-react'
import {
  assignQueueTicket,
  createQueueTicket,
  listQueue,
  transitionQueueTicket,
  type QueueAction,
} from '../services/queueService'
import { listDoctors } from '../services/doctorService'
import { listRooms } from '../services/roomService'
import { listPatients } from '../services/patientService'
import { toastError, toastSuccess } from '../lib/toast'
import { useAutoRefresh } from '../hooks/useAutoRefresh'
import {
  QueueTicketStatus,
  queueStatusLabels,
  type QueueTicket,
} from '../types/queue'
import type { Doctor } from '../types/doctor'
import type { Room } from '../types/room'
import type { Patient } from '../types/patient'
import { PageHeader } from '../components/PageHeader'
import { QueueTicketStatusBadge } from '../components/StatusBadge'
import { Combobox } from '../components/Combobox'
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
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const ALL = 'all'
const NONE = 'none'

function todayLocal(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset()
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 10)
}

// Hành động khả dụng theo trạng thái vé (khớp máy trạng thái ADR 0019).
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

export default function QueuePage() {
  const [date, setDate] = useState(todayLocal())
  const [status, setStatus] = useState('')
  const [doctorFilter, setDoctorFilter] = useState('')
  const [tickets, setTickets] = useState<QueueTicket[]>([])
  const [loading, setLoading] = useState(false)

  const [doctors, setDoctors] = useState<Doctor[]>([])
  const [rooms, setRooms] = useState<Room[]>([])

  // Dialog lấy số mới.
  const [takeOpen, setTakeOpen] = useState(false)
  const [search, setSearch] = useState('')
  const [patients, setPatients] = useState<Patient[]>([])
  const [patientId, setPatientId] = useState('')
  const [newRoomId, setNewRoomId] = useState('')
  const [newDoctorId, setNewDoctorId] = useState('')

  // Dialog điều phối (assign) một vé.
  const [assignTicket, setAssignTicket] = useState<QueueTicket | null>(null)
  const [assignRoomId, setAssignRoomId] = useState('')
  const [assignDoctorId, setAssignDoctorId] = useState('')

  useEffect(() => {
    void (async () => {
      try {
        const [d, r] = await Promise.all([
          listDoctors({ page: 1, pageSize: 100 }),
          listRooms({ page: 1, pageSize: 100 }),
        ])
        setDoctors(d.items)
        setRooms(r.items)
      } catch {
        // Danh sách phụ trợ bộ lọc; lỗi không chặn hàng đợi.
      }
    })()
  }, [])

  const load = useCallback(async (opts?: { silent?: boolean }) => {
    if (!opts?.silent) setLoading(true)
    try {
      const result = await listQueue({
        date: date || undefined,
        doctorId: doctorFilter || undefined,
        status: status === '' ? undefined : (Number(status) as QueueTicket['status']),
      })
      setTickets(result)
    } catch (err) {
      if (!opts?.silent) toastError(err)
    } finally {
      if (!opts?.silent) setLoading(false)
    }
  }, [date, doctorFilter, status])

  useEffect(() => {
    void load()
  }, [load])

  // Lấy số/gọi số có thể diễn ra ở màn khác (Lễ tân, Điều dưỡng) — tự làm mới hàng đợi.
  useAutoRefresh(() => void load({ silent: true }))

  // Tìm bệnh nhân trong dialog lấy số (debounce nhẹ qua effect).
  useEffect(() => {
    if (!takeOpen) return
    let active = true
    void (async () => {
      try {
        const result = await listPatients({ page: 1, pageSize: 20, search: search || undefined })
        if (active) setPatients(result.items)
      } catch {
        if (active) setPatients([])
      }
    })()
    return () => {
      active = false
    }
  }, [takeOpen, search])

  const doctorName = useMemo(() => new Map(doctors.map((d) => [d.id, d.fullName])), [doctors])

  const onAction = async (t: QueueTicket, action: QueueAction) => {
    try {
      await transitionQueueTicket(t.id, action)
      toastSuccess('Đã cập nhật hàng đợi.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const onTake = async () => {
    if (!patientId) {
      toastError(new Error('Vui lòng chọn bệnh nhân.'))
      return
    }
    try {
      await createQueueTicket({
        patientId,
        roomId: newRoomId || null,
        doctorId: newDoctorId || null,
      })
      toastSuccess('Đã lấy số hàng đợi.')
      setTakeOpen(false)
      setPatientId('')
      setSearch('')
      setNewRoomId('')
      setNewDoctorId('')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const openAssign = (t: QueueTicket) => {
    setAssignTicket(t)
    setAssignRoomId(t.roomId ?? '')
    setAssignDoctorId(t.doctorId ?? '')
  }

  const onAssign = async () => {
    if (!assignTicket) return
    try {
      await assignQueueTicket(assignTicket.id, {
        roomId: assignRoomId || null,
        doctorId: assignDoctorId || null,
      })
      toastSuccess('Đã điều phối vé.')
      setAssignTicket(null)
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const hasFilter = Boolean(status || doctorFilter)

  return (
    <section>
      <PageHeader
        title="Hàng đợi khám"
        description="Lấy số, gọi số & điều phối phòng/bác sĩ"
        actions={
          <div className="flex gap-2">
            <Button variant="outline" onClick={() => void load()} disabled={loading}>
              <RefreshCw className={loading ? 'size-4 animate-spin' : 'size-4'} />
              Làm mới
            </Button>
            <Button onClick={() => setTakeOpen(true)}>
              <Plus className="size-4" />
              Lấy số
            </Button>
          </div>
        }
      />

      <Card className="mb-4">
        <CardContent className="flex flex-wrap items-center gap-3">
          <Input
            type="date"
            className="w-auto"
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
          <Combobox
            className="w-[200px]"
            value={doctorFilter || ALL}
            onValueChange={(v) => setDoctorFilter(v === ALL ? '' : v)}
            options={[
              { value: ALL, label: 'Tất cả bác sĩ' },
              ...doctors.map((d) => ({ value: d.id, label: d.fullName })),
            ]}
            searchPlaceholder="Tìm bác sĩ…"
          />
          <Select value={status === '' ? ALL : status} onValueChange={(v) => setStatus(v === ALL ? '' : v)}>
            <SelectTrigger className="w-[170px]">
              <SelectValue placeholder="Trạng thái" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>Tất cả trạng thái</SelectItem>
              {Object.values(QueueTicketStatus).map((v) => (
                <SelectItem key={v} value={String(v)}>
                  {queueStatusLabels[v]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {hasFilter && (
            <Button
              variant="ghost"
              onClick={() => {
                setStatus('')
                setDoctorFilter('')
              }}
            >
              <X className="size-4" />
              Xoá lọc
            </Button>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-16">Số</TableHead>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead>Phòng</TableHead>
                <TableHead>Bác sĩ</TableHead>
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
              {!loading && tickets.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Chưa có vé nào trong ngày.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                tickets.map((t) => (
                  <TableRow key={t.id}>
                    <TableCell className="text-lg font-semibold tabular-nums">{t.number}</TableCell>
                    <TableCell>{t.patientName ?? '—'}</TableCell>
                    <TableCell className="text-muted-foreground">{t.roomName ?? '—'}</TableCell>
                    <TableCell className="text-muted-foreground">
                      {t.doctorName ?? (t.doctorId ? doctorName.get(t.doctorId) : '—') ?? '—'}
                    </TableCell>
                    <TableCell>
                      <QueueTicketStatusBadge status={t.status} />
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap items-center justify-end gap-1">
                        {actionsByStatus[t.status].map((x) => (
                          <Button
                            key={x.action}
                            size="sm"
                            variant={x.action === 'skip' ? 'ghost' : 'outline'}
                            onClick={() => void onAction(t, x.action)}
                          >
                            <x.icon className="size-4" />
                            {x.label}
                          </Button>
                        ))}
                        {t.status !== QueueTicketStatus.Done &&
                          t.status !== QueueTicketStatus.Skipped && (
                            <Button size="sm" variant="ghost" onClick={() => openAssign(t)}>
                              <Settings2 className="size-4" />
                              Điều phối
                            </Button>
                          )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Dialog lấy số mới */}
      <Dialog open={takeOpen} onOpenChange={setTakeOpen}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Lấy số hàng đợi</DialogTitle>
          </DialogHeader>
          <div className="flex flex-col gap-3">
            <div className="grid gap-1.5">
              <Label>Tìm bệnh nhân</Label>
              <Input
                placeholder="Tên hoặc mã bệnh nhân…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <div className="max-h-56 overflow-y-auto rounded-md border">
              {patients.length === 0 && (
                <p className="p-3 text-sm text-muted-foreground">Không có bệnh nhân phù hợp.</p>
              )}
              {patients.map((p) => (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => setPatientId(p.id)}
                  className={`flex w-full items-center justify-between px-3 py-2 text-left text-sm hover:bg-muted ${
                    patientId === p.id ? 'bg-primary/10 font-medium' : ''
                  }`}
                >
                  <span>{p.fullName}</span>
                  <span className="text-xs text-muted-foreground">{p.code}</span>
                </button>
              ))}
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div className="grid gap-1.5">
                <Label>Phòng (tuỳ chọn)</Label>
                <Select value={newRoomId || NONE} onValueChange={(v) => setNewRoomId(v === NONE ? '' : v)}>
                  <SelectTrigger>
                    <SelectValue placeholder="Chưa gán" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={NONE}>Chưa gán</SelectItem>
                    {rooms.map((r) => (
                      <SelectItem key={r.id} value={r.id}>
                        {r.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="grid gap-1.5">
                <Label>Bác sĩ (tuỳ chọn)</Label>
                <Combobox
                  value={newDoctorId || NONE}
                  onValueChange={(v) => setNewDoctorId(v === NONE ? '' : v)}
                  options={[
                    { value: NONE, label: 'Chưa gán' },
                    ...doctors.map((d) => ({ value: d.id, label: d.fullName })),
                  ]}
                  searchPlaceholder="Tìm bác sĩ…"
                />
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setTakeOpen(false)}>
              Đóng
            </Button>
            <Button onClick={() => void onTake()} disabled={!patientId}>
              Lấy số
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialog điều phối vé */}
      <Dialog open={assignTicket !== null} onOpenChange={(open) => !open && setAssignTicket(null)}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Điều phối · số {assignTicket?.number}</DialogTitle>
          </DialogHeader>
          <div className="grid grid-cols-2 gap-3">
            <div className="grid gap-1.5">
              <Label>Phòng</Label>
              <Select value={assignRoomId || NONE} onValueChange={(v) => setAssignRoomId(v === NONE ? '' : v)}>
                <SelectTrigger>
                  <SelectValue placeholder="Chưa gán" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NONE}>Chưa gán</SelectItem>
                  {rooms.map((r) => (
                    <SelectItem key={r.id} value={r.id}>
                      {r.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="grid gap-1.5">
              <Label>Bác sĩ</Label>
              <Combobox
                value={assignDoctorId || NONE}
                onValueChange={(v) => setAssignDoctorId(v === NONE ? '' : v)}
                options={[
                  { value: NONE, label: 'Chưa gán' },
                  ...doctors.map((d) => ({ value: d.id, label: d.fullName })),
                ]}
                searchPlaceholder="Tìm bác sĩ…"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setAssignTicket(null)}>
              Đóng
            </Button>
            <Button onClick={() => void onAssign()}>Lưu</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </section>
  )
}
