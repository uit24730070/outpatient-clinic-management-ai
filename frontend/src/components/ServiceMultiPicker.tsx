import { useMemo, useState } from 'react'
import { Search } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { formatVnd } from '../lib/format'
import type { ServicePrice } from '../types/invoice'

interface Props {
  services: ServicePrice[]
  picked: string[]
  onToggle: (id: string) => void
  emptyText?: string
  searchPlaceholder?: string
}

/**
 * Chọn nhiều dịch vụ (chip bật/tắt) kèm ô tìm kiếm lọc theo tên — dùng ở mọi màn chỉ định/đăng ký CLS
 * (`VisitForm`, `VisitDetailPage`, `LabOrderPanel`). Trước đây mỗi màn tự vẽ lưới chip riêng; tách ra đây
 * để khỏi lặp code, đồng thời thêm tìm kiếm khi danh mục dài (nhiều dịch vụ) khó rà bằng mắt.
 */
export function ServiceMultiPicker({
  services,
  picked,
  onToggle,
  emptyText = 'Chưa có dịch vụ nào trong bảng giá.',
  searchPlaceholder = 'Tìm dịch vụ…',
}: Props) {
  const [search, setSearch] = useState('')

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return services
    return services.filter((s) => s.name.toLowerCase().includes(q) || s.code.toLowerCase().includes(q))
  }, [services, search])

  if (services.length === 0) {
    return <p className="text-sm text-muted-foreground">{emptyText}</p>
  }

  return (
    <div className="flex flex-col gap-2">
      {services.length > 6 && (
        <div className="relative">
          <Search className="pointer-events-none absolute left-2.5 top-2.5 size-4 text-muted-foreground" />
          <Input
            type="search"
            className="pl-8"
            placeholder={searchPlaceholder}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
      )}
      <div className="flex flex-wrap gap-2">
        {filtered.length === 0 && (
          <p className="text-sm text-muted-foreground">Không tìm thấy dịch vụ phù hợp.</p>
        )}
        {filtered.map((s) => {
          const on = picked.includes(s.id)
          return (
            <button
              key={s.id}
              type="button"
              onClick={() => onToggle(s.id)}
              className={
                on
                  ? 'rounded-full border border-primary bg-primary/10 px-3 py-1 text-sm text-primary'
                  : 'rounded-full border px-3 py-1 text-sm text-muted-foreground hover:bg-muted'
              }
            >
              {s.name} · {formatVnd(s.unitPrice)}
            </button>
          )
        })}
      </div>
    </div>
  )
}
