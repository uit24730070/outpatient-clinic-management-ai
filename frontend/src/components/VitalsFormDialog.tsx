import { forwardRef, useEffect, useState, type InputHTMLAttributes } from 'react'
import { useForm } from 'react-hook-form'
import { getVitalsHistory, recordVitals } from '../services/vitalsService'
import { applyServerErrors } from '../lib/form'
import { toastSuccess } from '../lib/toast'
import type { Vitals, VitalsFormValues } from '../types/vitals'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

// Chuyển chuỗi input → số | null (ô trống = không đo).
function num(value: string): number | null {
  const trimmed = value.trim()
  if (trimmed === '') return null
  const n = Number(trimmed)
  return Number.isFinite(n) ? n : null
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
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

interface Props {
  /** null = đóng dialog. */
  appointmentId: string | null
  patientName?: string | null
  onOpenChange: (open: boolean) => void
  onSaved?: () => void
}

/**
 * Dialog ghi một lần đo sinh hiệu mới cho một lượt khám — tách khỏi `VitalsPage` (Epic 17) để dùng lại
 * được ở workspace Điều dưỡng (`/nurse`) lẫn màn `/vitals` cũ, tránh trùng lặp form.
 *
 * Mỗi lần lưu tạo MỘT bản ghi mới (không ghi đè lần đo trước — bệnh nhân có thể yêu cầu đo lại nhiều
 * lần), nên form luôn bắt đầu trống; các lần đo trước hiển thị bên dưới để tham khảo.
 */
export function VitalsFormDialog({ appointmentId, patientName, onOpenChange, onSaved }: Props) {
  const form = useForm<FormFields>({ defaultValues: EMPTY })
  const { register, handleSubmit, reset } = form
  const [history, setHistory] = useState<Vitals[]>([])

  useEffect(() => {
    if (!appointmentId) return
    reset(EMPTY)
    setHistory([])
    void (async () => {
      try {
        setHistory(await getVitalsHistory(appointmentId))
      } catch {
        // Không đọc được lịch sử: vẫn cho đo mới bình thường.
      }
    })()
  }, [appointmentId, reset])

  const onSubmit = handleSubmit(async (values) => {
    if (!appointmentId) return
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
      await recordVitals(appointmentId, payload)
      toastSuccess('Đã ghi nhận lần đo sinh hiệu.')
      onOpenChange(false)
      onSaved?.()
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  return (
    <Dialog open={appointmentId !== null} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Sinh hiệu · {patientName ?? ''}</DialogTitle>
        </DialogHeader>

        {history.length > 0 && (
          <div className="flex flex-col gap-1 rounded-md border bg-muted/30 p-2 text-xs">
            <span className="font-medium text-muted-foreground">
              Đã đo {history.length} lần trước đó
            </span>
            <div className="flex max-h-24 flex-col gap-1 overflow-y-auto">
              {history.map((v) => (
                <div key={v.id} className="flex flex-wrap items-center gap-x-2 text-muted-foreground">
                  <span className="font-medium text-foreground">{formatTime(v.measuredAt)}</span>
                  {v.temperatureC != null && <span>{v.temperatureC}°C</span>}
                  {(v.bloodPressureSystolic != null || v.bloodPressureDiastolic != null) && (
                    <span>
                      HA {v.bloodPressureSystolic ?? '—'}/{v.bloodPressureDiastolic ?? '—'}
                    </span>
                  )}
                  {v.pulse != null && <span>Mạch {v.pulse}</span>}
                  {v.spO2 != null && <span>SpO2 {v.spO2}%</span>}
                  {v.measuredByName && <span>· {v.measuredByName}</span>}
                </div>
              ))}
            </div>
          </div>
        )}

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
            <Button type="button" variant="ghost" onClick={() => onOpenChange(false)}>
              Đóng
            </Button>
            <Button type="submit">Ghi nhận lần đo</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
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
