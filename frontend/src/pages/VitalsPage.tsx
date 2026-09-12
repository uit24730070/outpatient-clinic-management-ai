import { forwardRef, useCallback, useEffect, useState, type InputHTMLAttributes } from 'react'
import { useForm } from 'react-hook-form'
import { Activity } from 'lucide-react'
import { listAppointments } from '../services/appointmentService'
import { getVitals, upsertVitals } from '../services/vitalsService'
import { applyServerErrors } from '../lib/form'
import { toastSuccess } from '../lib/toast'
import { AppointmentStatus, type Appointment } from '../types/appointment'
import type { VitalsFormValues } from '../types/vitals'
import { PageHeader } from '../components/PageHeader'
import { AppointmentStatusBadge } from '../components/StatusBadge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent } from '@/components/ui/card'
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

function todayLocal(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset()
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 10)
}

// Chuyển chuỗi input → số | null (ô trống = không đo).
function num(value: string): number | null {
  const trimmed = value.trim()
  if (trimmed === '') return null
  const n = Number(trimmed)
  return Number.isFinite(n) ? n : null
}

type FormFields = {
  heightCm: string
  weightKg: string
  temperatureC: string
  pulse: string
  bloodPressureSystolic: string
  bloodPressureDiastolic: string
  spO2: string
  respiratoryRate: string
  notes: string
}

const EMPTY: FormFields = {
  heightCm: '',
  weightKg: '',
  temperatureC: '',
  pulse: '',
  bloodPressureSystolic: '',
  bloodPressureDiastolic: '',
  spO2: '',
  respiratoryRate: '',
  notes: '',
}

/** Màn nhập sinh hiệu (Điều dưỡng/Admin): chọn bệnh nhân đã check-in → nhập chỉ số. */
export default function VitalsPage() {
  const [date, setDate] = useState(todayLocal())
  const [appointments, setAppointments] = useState<Appointment[]>([])
  const [loading, setLoading] = useState(false)
  const [selected, setSelected] = useState<Appointment | null>(null)

  const form = useForm<FormFields>({ defaultValues: EMPTY })
  const { register, handleSubmit, reset } = form

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listAppointments({ page: 1, pageSize: 100, date: date || undefined })
      // Sinh hiệu đo sau check-in, trước/trong khi khám.
      setAppointments(
        result.items.filter(
          (a) =>
            a.status === AppointmentStatus.CheckedIn ||
            a.status === AppointmentStatus.InProgress,
        ),
      )
    } catch {
      setAppointments([])
    } finally {
      setLoading(false)
    }
  }, [date])

  useEffect(() => {
    void load()
  }, [load])

  const openForm = async (appt: Appointment) => {
    setSelected(appt)
    reset(EMPTY)
    try {
      const existing = await getVitals(appt.id)
      if (existing) {
        reset({
          heightCm: existing.heightCm?.toString() ?? '',
          weightKg: existing.weightKg?.toString() ?? '',
          temperatureC: existing.temperatureC?.toString() ?? '',
          pulse: existing.pulse?.toString() ?? '',
          bloodPressureSystolic: existing.bloodPressureSystolic?.toString() ?? '',
          bloodPressureDiastolic: existing.bloodPressureDiastolic?.toString() ?? '',
          spO2: existing.spO2?.toString() ?? '',
          respiratoryRate: existing.respiratoryRate?.toString() ?? '',
          notes: existing.notes ?? '',
        })
      }
    } catch {
      // Chưa đo / không đọc được: giữ form trống.
    }
  }

  const onSubmit = handleSubmit(async (values) => {
    if (!selected) return
    const payload: VitalsFormValues = {
      heightCm: num(values.heightCm),
      weightKg: num(values.weightKg),
      temperatureC: num(values.temperatureC),
      pulse: num(values.pulse),
      bloodPressureSystolic: num(values.bloodPressureSystolic),
      bloodPressureDiastolic: num(values.bloodPressureDiastolic),
      spO2: num(values.spO2),
      respiratoryRate: num(values.respiratoryRate),
      notes: values.notes.trim() || null,
    }
    try {
      await upsertVitals(selected.id, payload)
      toastSuccess('Đã lưu sinh hiệu.')
      setSelected(null)
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

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
                      <Button size="sm" variant="secondary" onClick={() => void openForm(a)}>
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

      <Dialog open={selected !== null} onOpenChange={(open) => !open && setSelected(null)}>
        <DialogContent className="max-w-lg">
          <DialogHeader>
            <DialogTitle>Sinh hiệu · {selected?.patientName ?? ''}</DialogTitle>
          </DialogHeader>
          <form onSubmit={onSubmit} noValidate className="grid grid-cols-2 gap-3">
            <Field label="Chiều cao (cm)" {...register('heightCm')} />
            <Field label="Cân nặng (kg)" {...register('weightKg')} />
            <Field label="Nhiệt độ (°C)" {...register('temperatureC')} />
            <Field label="Mạch (l/p)" {...register('pulse')} />
            <Field label="HA tâm thu (mmHg)" {...register('bloodPressureSystolic')} />
            <Field label="HA tâm trương (mmHg)" {...register('bloodPressureDiastolic')} />
            <Field label="SpO2 (%)" {...register('spO2')} />
            <Field label="Nhịp thở (l/p)" {...register('respiratoryRate')} />
            <div className="col-span-2 grid gap-1.5">
              <Label htmlFor="notes">Ghi chú</Label>
              <Textarea id="notes" rows={2} {...register('notes')} />
            </div>
            <DialogFooter className="col-span-2">
              <Button type="button" variant="ghost" onClick={() => setSelected(null)}>
                Đóng
              </Button>
              <Button type="submit">Lưu sinh hiệu</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </section>
  )
}

// Ô nhập số nhỏ (nhãn + input), forward ref cho react-hook-form register.
const Field = forwardRef<
  HTMLInputElement,
  { label: string; name?: string } & InputHTMLAttributes<HTMLInputElement>
>(function Field({ label, ...props }, ref) {
  return (
    <div className="grid gap-1.5">
      <Label>{label}</Label>
      <Input type="number" step="any" ref={ref} {...props} />
    </div>
  )
})
