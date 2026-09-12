import { useCallback, useEffect, useState } from 'react'
import { Activity } from 'lucide-react'
import { listAppointments } from '../services/appointmentService'
import { AppointmentStatus, type Appointment } from '../types/appointment'
import { PageHeader } from '../components/PageHeader'
import { AppointmentStatusBadge } from '../components/StatusBadge'
import { VitalsFormDialog } from '../components/VitalsFormDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
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

/** Màn nhập sinh hiệu (Điều dưỡng/Admin): chọn bệnh nhân đã check-in → nhập chỉ số. */
export default function VitalsPage() {
  const [date, setDate] = useState(todayLocal())
  const [appointments, setAppointments] = useState<Appointment[]>([])
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<Appointment | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listAppointments({ page: 1, pageSize: 100, date: date || undefined })
      // Sinh hiệu đo sau check-in, trước/trong khi khám.
      const checkedIn = result.items.filter(
        (a) =>
          a.status === AppointmentStatus.CheckedIn || a.status === AppointmentStatus.InProgress,
      )
      // Gom theo Lượt tiếp đón — sinh hiệu chỉ đo MỘT LẦN cho cả lượt (nhiều dịch vụ khám/chuyên khoa
      // cùng lần đến dùng chung), nên chỉ hiện 1 dòng/lượt (giữ lịch đầu tiên làm đại diện để mở form).
      const byGroup = new Map<string, Appointment>()
      for (const a of checkedIn) {
        const key = a.visitId ?? a.id
        if (!byGroup.has(key)) byGroup.set(key, a)
      }
      setAppointments(Array.from(byGroup.values()))
    } catch {
      setAppointments([])
    } finally {
      setLoading(false)
    }
  }, [date])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <section>
      <PageHeader title="Sinh hiệu" description="Nhập sinh hiệu cho bệnh nhân đã tiếp đón" />

      <Card className="mb-4">
        <CardContent className="flex flex-wrap items-center gap-3">
          <Label htmlFor="date">Ngày</Label>
          <Input
            id="date"
            type="date"
            className="w-auto"
            value={date}
            onChange={(e) => setDate(e.target.value)}
          />
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead>Bác sĩ</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={4} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && appointments.length === 0 && (
                <TableRow>
                  <TableCell colSpan={4} className="h-24 text-center text-muted-foreground">
                    Không có bệnh nhân đã tiếp đón trong ngày.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                appointments.map((a) => (
                  <TableRow key={a.id}>
                    <TableCell className="font-medium">{a.patientName ?? '—'}</TableCell>
                    <TableCell>{a.doctorName ?? '—'}</TableCell>
                    <TableCell>
                      <AppointmentStatusBadge status={a.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button size="sm" variant="secondary" onClick={() => setSelected(a)}>
                        <Activity className="size-4" />
                        Nhập sinh hiệu
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <VitalsFormDialog
        appointmentId={selected?.id ?? null}
        patientName={selected?.patientName}
        onOpenChange={(open) => !open && setSelected(null)}
      />
    </section>
  )
}
