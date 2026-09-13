import type { LucideIcon } from 'lucide-react'
import { KpiCard, type KpiTone } from '@/components/KpiCard'

export interface WorkspaceSummaryItem {
  icon: LucideIcon
  label: string
  value: string
  tone?: KpiTone
}

/**
 * Dải thẻ tóm tắt đầu mỗi workspace theo vai trò (Epic 18, VIS-02) — cùng ngôn ngữ thị giác với
 * KPI ở Dashboard, để mỗi màn "một tác vụ" (Lễ tân/Bác sĩ/Điều dưỡng...) mở ra là thấy ngay tình
 * hình ca làm việc, không phải đếm dòng trong bảng bên dưới.
 */
export function WorkspaceSummaryBar({ items }: { items: WorkspaceSummaryItem[] }) {
  if (items.length === 0) return null
  return (
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {items.map((item) => (
        <KpiCard key={item.label} icon={item.icon} label={item.label} value={item.value} tone={item.tone} />
      ))}
    </div>
  )
}
