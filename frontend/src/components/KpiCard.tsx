import type { LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Card, CardContent } from '@/components/ui/card'

// Sắc thái cảnh báo dùng chung cho mọi thẻ chỉ số (Dashboard + WorkspaceSummaryBar) — khớp
// ngôn ngữ màu cảnh báo lâm sàng (Epic 18, VIS-04): default = trung tính, warning/danger = cần chú ý.
export type KpiTone = 'default' | 'warning' | 'danger'

const toneClasses: Record<KpiTone, string> = {
  default: 'bg-primary/10 text-primary',
  warning: 'bg-amber-500/10 text-amber-600 dark:text-amber-400',
  danger: 'bg-destructive/10 text-destructive',
}

export function KpiCard({
  icon: Icon,
  label,
  value,
  tone = 'default',
}: {
  icon: LucideIcon
  label: string
  value: string
  tone?: KpiTone
}) {
  return (
    <Card>
      <CardContent className="flex items-center gap-4">
        <div className={cn('flex size-11 items-center justify-center rounded-lg', toneClasses[tone])}>
          <Icon className="size-5" />
        </div>
        <div>
          <p className="text-sm text-muted-foreground">{label}</p>
          <p className="text-2xl font-bold tracking-tight">{value}</p>
        </div>
      </CardContent>
    </Card>
  )
}
