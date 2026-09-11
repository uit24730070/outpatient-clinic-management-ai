import { useCallback, useEffect, useState } from 'react'
import { Plus, Trash2 } from 'lucide-react'
import {
  createDoctorSchedule,
  deleteDoctorSchedule,
  listDoctorSchedules,
} from '../services/scheduleService'
import { listRooms } from '../services/roomService'
import { toastError, toastInfo, toastSuccess } from '../lib/toast'
import {
  DayOfWeek,
  dayOfWeekLabels,
  dayOfWeekOrder,
  toTimeInput,
  toTimeSpanString,
  type DayOfWeekValue,
  type DoctorWorkSchedule,
} from '../types/schedule'
import type { Room } from '../types/room'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
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

const NO_ROOM = '__none__'

/** Khối quản lý lịch làm việc (mẫu tuần) của một bác sĩ — WS-02. Chỉ hiện khi đang sửa hồ sơ. */
export function DoctorScheduleManager({ doctorId }: { doctorId: string }) {
  const [schedules, setSchedules] = useState<DoctorWorkSchedule[]>([])
  const [rooms, setRooms] = useState<Room[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  // Form thêm khung.
  const [day, setDay] = useState<DayOfWeekValue>(DayOfWeek.Monday)
  const [start, setStart] = useState('08:00')
  const [end, setEnd] = useState('12:00')
  const [roomId, setRoomId] = useState<string>('')

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const [list, roomPage] = await Promise.all([
        listDoctorSchedules(doctorId),
        listRooms({ page: 1, pageSize: 100 }),
      ])
      setSchedules(list)
      setRooms(roomPage.items)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [doctorId])

  useEffect(() => {
    void load()
  }, [load])

  const onAdd = async () => {
    if (end <= start) {
      toastInfo('Giờ kết thúc phải sau giờ bắt đầu.')
      return
    }
    setSaving(true)
    try {
      await createDoctorSchedule(doctorId, {
        dayOfWeek: day,
        startTime: toTimeSpanString(start),
        endTime: toTimeSpanString(end),
        roomId: roomId || null,
      })
      toastSuccess('Đã thêm khung giờ làm việc.')
      await load()
    } catch (err) {
      toastError(err)
    } finally {
      setSaving(false)
    }
  }

  const onDelete = async (scheduleId: string) => {
    try {
      await deleteDoctorSchedule(doctorId, scheduleId)
      toastSuccess('Đã xoá khung giờ làm việc.')
      await load()
    } catch (err) {
      toastError(err)
    }
  }

  const sorted = [...schedules].sort(
    (a, b) =>
      dayOfWeekOrder.indexOf(a.dayOfWeek) - dayOfWeekOrder.indexOf(b.dayOfWeek) ||
      a.startTime.localeCompare(b.startTime),
  )

  return (
    <Card className="mt-4">
      <CardHeader>
        <CardTitle>Lịch làm việc (mẫu tuần)</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {/* Form thêm khung */}
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-5 sm:items-end">
          <div className="grid gap-1">
            <Label>Thứ</Label>
            <Select value={String(day)} onValueChange={(v) => setDay(Number(v) as DayOfWeekValue)}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {dayOfWeekOrder.map((d) => (
                  <SelectItem key={d} value={String(d)}>
                    {dayOfWeekLabels[d]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="grid gap-1">
            <Label>Bắt đầu</Label>
            <Input type="time" value={start} onChange={(e) => setStart(e.target.value)} />
          </div>
          <div className="grid gap-1">
            <Label>Kết thúc</Label>
            <Input type="time" value={end} onChange={(e) => setEnd(e.target.value)} />
          </div>
          <div className="grid gap-1">
            <Label>Phòng</Label>
            <Select
              value={roomId || NO_ROOM}
              onValueChange={(v) => setRoomId(v === NO_ROOM ? '' : v)}
            >
              <SelectTrigger>
                <SelectValue placeholder="—" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={NO_ROOM}>— Không gán —</SelectItem>
                {rooms.map((r) => (
                  <SelectItem key={r.id} value={r.id}>
                    {r.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <Button type="button" onClick={() => void onAdd()} disabled={saving}>
            <Plus className="size-4" />
            Thêm
          </Button>
        </div>

        {/* Danh sách khung */}
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Thứ</TableHead>
              <TableHead>Giờ</TableHead>
              <TableHead>Phòng</TableHead>
              <TableHead className="text-right">Thao tác</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading && (
              <TableRow>
                <TableCell colSpan={4} className="h-16 text-center text-muted-foreground">
                  Đang tải…
                </TableCell>
              </TableRow>
            )}
            {!loading && sorted.length === 0 && (
              <TableRow>
                <TableCell colSpan={4} className="h-16 text-center text-muted-foreground">
                  Chưa khai lịch làm việc. Khi chưa khai, bác sĩ có thể được đặt lịch tự do.
                </TableCell>
              </TableRow>
            )}
            {!loading &&
              sorted.map((s) => (
                <TableRow key={s.id}>
                  <TableCell className="font-medium">{dayOfWeekLabels[s.dayOfWeek]}</TableCell>
                  <TableCell>
                    {toTimeInput(s.startTime)} – {toTimeInput(s.endTime)}
                  </TableCell>
                  <TableCell className="text-muted-foreground">{s.roomName ?? '—'}</TableCell>
                  <TableCell className="text-right">
                    <Button
                      size="sm"
                      variant="ghost"
                      className="text-destructive hover:text-destructive"
                      onClick={() => void onDelete(s.id)}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  )
}
