import { useEffect, useState } from 'react'
import { Activity, ChevronDown, ChevronUp, TriangleAlert } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { getVitalsHistory } from '../services/vitalsService'
import type { Vitals } from '../types/vitals'
import {
  bloodPressureTone,
  pulseTone,
  respiratoryRateTone,
  spO2Tone,
  temperatureTone,
  worstTone,
  type VitalTone,
} from '../lib/vitalsThresholds'

// Ngôn ngữ màu cảnh báo lâm sàng dùng chung với KpiCard — chỉ trực quan hoá, không phải cảnh báo
// y khoa chính thức (ngưỡng cố định đơn giản, xem `lib/vitalsThresholds.ts`).
const metricToneClasses: Record<VitalTone, string> = {
  default: 'border bg-muted/30',
  warning: 'border-amber-500/40 bg-amber-500/10',
  danger: 'border-destructive/40 bg-destructive/10',
}
const metricValueToneClasses: Record<VitalTone, string> = {
  default: '',
  warning: 'text-amber-700 dark:text-amber-400',
  danger: 'text-destructive',
}

/** Một ô chỉ số sinh hiệu (nhãn + giá trị + đơn vị), tô cảnh báo khi ngoài ngưỡng tham khảo. */
function Metric({
  label,
  value,
  unit,
  tone = 'default',
}: {
  label: string
  value: string | number | null
  unit?: string
  tone?: VitalTone
}) {
  return (
    <div className={cn('rounded-md px-3 py-2', metricToneClasses[tone])}>
      <div className="flex items-center gap-1 text-xs text-muted-foreground">
        {label}
        {tone !== 'default' && (
          <TriangleAlert className={cn('size-3', tone === 'danger' ? 'text-destructive' : 'text-amber-600')} />
        )}
      </div>
      <div className={cn('font-medium', metricValueToneClasses[tone])}>
        {value === null || value === '' ? '—' : value}
        {value !== null && value !== '' && unit ? <span className="text-xs text-muted-foreground"> {unit}</span> : null}
      </div>
    </div>
  )
}

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function bloodPressure(v: Vitals): string | number | null {
  return v.bloodPressureSystolic !== null && v.bloodPressureDiastolic !== null
    ? `${v.bloodPressureSystolic}/${v.bloodPressureDiastolic}`
    : v.bloodPressureSystolic ?? v.bloodPressureDiastolic
}

/**
 * Thẻ hiển thị sinh hiệu (đọc-chỉ) của một lượt khám — dùng cho bác sĩ xem trong bệnh án (QN-04).
 * Sinh hiệu do điều dưỡng nhập sau tiếp đón, có thể đo lại nhiều lần (lịch sử) — thẻ hiện lần đo gần
 * nhất làm chính, các lần trước có thể mở rộng để xem lại (vd đối chiếu khi bệnh nhân yêu cầu đo lại).
 */
export function VitalsCard({ appointmentId }: { appointmentId: string }) {
  const [history, setHistory] = useState<Vitals[]>([])
  const [loaded, setLoaded] = useState(false)
  const [expanded, setExpanded] = useState(false)

  useEffect(() => {
    if (!appointmentId) return
    let active = true
    void (async () => {
      try {
        const list = await getVitalsHistory(appointmentId)
        if (active) setHistory(list)
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

  if (!loaded || history.length === 0) return null

  const [latest, ...older] = history

  return (
    <Card>
      <CardContent className="flex flex-col gap-3">
        <div className="flex items-center gap-2 text-sm font-medium">
          <Activity className="size-4 text-primary" />
          Sinh hiệu
          {latest.measuredByName && (
            <span className="font-normal text-muted-foreground">
              · {latest.measuredByName} lúc {formatDateTime(latest.measuredAt)}
            </span>
          )}
          {older.length > 0 && (
            <Button
              type="button"
              size="sm"
              variant="ghost"
              className="ml-auto h-7 px-2 text-xs text-muted-foreground"
              onClick={() => setExpanded((v) => !v)}
            >
              {expanded ? <ChevronUp className="size-3.5" /> : <ChevronDown className="size-3.5" />}
              Lịch sử đo ({history.length} lần)
            </Button>
          )}
        </div>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-4 lg:grid-cols-5">
          <Metric label="Chiều cao" value={latest.heightCm} unit="cm" />
          <Metric label="Cân nặng" value={latest.weightKg} unit="kg" />
          <Metric label="BMI" value={latest.bmi} />
          <Metric label="Nhiệt độ" value={latest.temperatureC} unit="°C" tone={temperatureTone(latest.temperatureC)} />
          <Metric label="Mạch" value={latest.pulse} unit="l/p" tone={pulseTone(latest.pulse)} />
          <Metric
            label="Huyết áp"
            value={bloodPressure(latest)}
            unit="mmHg"
            tone={bloodPressureTone(latest.bloodPressureSystolic, latest.bloodPressureDiastolic)}
          />
          <Metric label="SpO2" value={latest.spO2} unit="%" tone={spO2Tone(latest.spO2)} />
          <Metric
            label="Nhịp thở"
            value={latest.respiratoryRate}
            unit="l/p"
            tone={respiratoryRateTone(latest.respiratoryRate)}
          />
        </div>
        {latest.notes && <p className="text-sm text-muted-foreground">Ghi chú: {latest.notes}</p>}

        {expanded && older.length > 0 && (
          <div className="flex flex-col gap-2 border-t pt-3">
            {older.map((v) => {
              const rowTone = worstTone([
                temperatureTone(v.temperatureC),
                pulseTone(v.pulse),
                spO2Tone(v.spO2),
                bloodPressureTone(v.bloodPressureSystolic, v.bloodPressureDiastolic),
                respiratoryRateTone(v.respiratoryRate),
              ])
              return (
                <div
                  key={v.id}
                  className={cn(
                    'flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-muted-foreground',
                    rowTone !== 'default' && metricValueToneClasses[rowTone],
                  )}
                >
                  <span className="font-medium text-foreground">{formatDateTime(v.measuredAt)}</span>
                  {v.temperatureC != null && <span>{v.temperatureC}°C</span>}
                  {bloodPressure(v) != null && <span>HA {bloodPressure(v)}</span>}
                  {v.pulse != null && <span>Mạch {v.pulse}</span>}
                  {v.spO2 != null && <span>SpO2 {v.spO2}%</span>}
                  {v.measuredByName && <span>· {v.measuredByName}</span>}
                  {rowTone !== 'default' && <TriangleAlert className="size-3" />}
                </div>
              )
            })}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
