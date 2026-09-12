import { useEffect, useState } from 'react'
import { Activity } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { getVitals } from '../services/vitalsService'
import type { Vitals } from '../types/vitals'

/** Một ô chỉ số sinh hiệu (nhãn + giá trị + đơn vị). */
function Metric({ label, value, unit }: { label: string; value: string | number | null; unit?: string }) {
  return (
    <div className="rounded-md border bg-muted/30 px-3 py-2">
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="font-medium">
        {value === null || value === '' ? '—' : value}
        {value !== null && value !== '' && unit ? <span className="text-xs text-muted-foreground"> {unit}</span> : null}
      </div>
    </div>
  )
}

/**
 * Thẻ hiển thị sinh hiệu (đọc-chỉ) của một lượt khám — dùng cho bác sĩ xem trong bệnh án (QN-04).
 * Sinh hiệu do điều dưỡng nhập sau tiếp đón; null nếu chưa đo.
 */
export function VitalsCard({ appointmentId }: { appointmentId: string }) {
  const [vitals, setVitals] = useState<Vitals | null>(null)
  const [loaded, setLoaded] = useState(false)

  useEffect(() => {
    if (!appointmentId) return
    let active = true
    void (async () => {
      try {
        const v = await getVitals(appointmentId)
        if (active) setVitals(v)
      } catch {
        // Không có quyền đọc / lỗi: ẩn thẻ, không chặn màn khám.
      } finally {
        if (active) setLoaded(true)
      }
    })()
    return () => {
      active = false
    }
  }, [appointmentId])

  if (!loaded || !vitals) return null

  const bp =
    vitals.bloodPressureSystolic !== null && vitals.bloodPressureDiastolic !== null
      ? `${vitals.bloodPressureSystolic}/${vitals.bloodPressureDiastolic}`
      : vitals.bloodPressureSystolic ?? vitals.bloodPressureDiastolic

  return (
    <Card>
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-center gap-2 text-sm font-medium">
          <Activity className="size-4 text-primary" />
          Sinh hiệu
          {vitals.measuredByName && (
            <span className="font-normal text-muted-foreground">
              · {vitals.measuredByName} lúc{' '}
              {new Date(vitals.measuredAt).toLocaleString('vi-VN', {
                day: '2-digit',
                month: '2-digit',
                hour: '2-digit',
                minute: '2-digit',
              })}
            </span>
          )}
        </div>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4 lg:grid-cols-5">
          <Metric label="Chiều cao" value={vitals.heightCm} unit="cm" />
          <Metric label="Cân nặng" value={vitals.weightKg} unit="kg" />
          <Metric label="BMI" value={vitals.bmi} />
          <Metric label="Nhiệt độ" value={vitals.temperatureC} unit="°C" />
          <Metric label="Mạch" value={vitals.pulse} unit="l/p" />
          <Metric label="Huyết áp" value={bp} unit="mmHg" />
          <Metric label="SpO2" value={vitals.spO2} unit="%" />
          <Metric label="Nhịp thở" value={vitals.respiratoryRate} unit="l/p" />
        </div>
        {vitals.notes && <p className="text-sm text-muted-foreground">Ghi chú: {vitals.notes}</p>}
      </CardContent>
    </Card>
  )
}
