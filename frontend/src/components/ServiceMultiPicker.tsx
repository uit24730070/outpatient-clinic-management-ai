import { useMemo, useState } from 'react'
import { Activity, ChevronDown, Microscope, MoreHorizontal, ScanLine, Search, TestTube } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { formatVnd } from '../lib/format'
import { ParaclinicalGroup, paraclinicalGroupLabels, ServiceCategory, type ServicePrice } from '../types/invoice'

interface Props {
  services: ServicePrice[]
  picked: string[]
  onToggle: (id: string) => void
  emptyText?: string
  searchPlaceholder?: string
}

const groupIcons: Record<number, typeof TestTube> = {
  [ParaclinicalGroup.LabTest]: TestTube,
  [ParaclinicalGroup.Imaging]: ScanLine,
  [ParaclinicalGroup.Functional]: Activity,
  [ParaclinicalGroup.Endoscopy]: Microscope,
  [ParaclinicalGroup.Other]: MoreHorizontal,
}

const UNGROUPED_KEY = -1

/**
 * Chọn nhiều dịch vụ (chip bật/tắt) kèm ô tìm kiếm lọc theo tên — dùng ở mọi màn chỉ định/đăng ký CLS
 * (`VisitForm`, `VisitDetailPage`, `LabOrderPanel`). Dịch vụ Cận lâm sàng (nhiều nhất — có thể 20-30 mục)
 * được gom theo `group` (xét nghiệm/chẩn đoán hình ảnh/thăm dò chức năng/nội soi, ADR 0024) thành từng
 * khối gấp mở được thay vì một dải chip phẳng dài — phản hồi giảng viên: liệt kê phẳng gây rối mắt khi
 * chỉ định. Dịch vụ chưa phân nhóm/loại khác vẫn hiển thị phẳng như trước (không đủ dữ liệu để gom).
 */
export function ServiceMultiPicker({
  services,
  picked,
  onToggle,
  emptyText = 'Chưa có dịch vụ nào trong bảng giá.',
  searchPlaceholder = 'Tìm dịch vụ…',
}: Props) {
  const [search, setSearch] = useState('')
  const [collapsed, setCollapsed] = useState<Set<number>>(new Set())

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return services
    return services.filter((s) => s.name.toLowerCase().includes(q) || s.code.toLowerCase().includes(q))
  }, [services, search])

  // Chỉ gom nhóm khi mọi dịch vụ đều là Cận lâm sàng (nơi group có ý nghĩa) — các màn khác (công
  // khám…) vẫn hiển thị phẳng như cũ.
  const isParaclinicalList = services.length > 0 && services.every((s) => s.category === ServiceCategory.Paraclinical)

  const groups = useMemo(() => {
    if (!isParaclinicalList) return null
    const map = new Map<number, ServicePrice[]>()
    for (const s of filtered) {
      const key = s.group ?? UNGROUPED_KEY
      const list = map.get(key)
      if (list) list.push(s)
      else map.set(key, [s])
    }
    // Thứ tự cố định theo nhóm (không theo thứ tự gặp) để danh sách ổn định giữa các lần tìm kiếm.
    const order = [
      ParaclinicalGroup.LabTest,
      ParaclinicalGroup.Imaging,
      ParaclinicalGroup.Functional,
      ParaclinicalGroup.Endoscopy,
      ParaclinicalGroup.Other,
      UNGROUPED_KEY,
    ]
    return order
      .filter((k) => map.has(k))
      .map((k) => ({ key: k, label: k === UNGROUPED_KEY ? 'Chưa phân nhóm' : paraclinicalGroupLabels[k], items: map.get(k)! }))
  }, [filtered, isParaclinicalList])

  const toggleCollapsed = (key: number) => {
    setCollapsed((cur) => {
      const next = new Set(cur)
      if (next.has(key)) next.delete(key)
      else next.add(key)
      return next
    })
  }

  if (services.length === 0) {
    return <p className="text-sm text-muted-foreground">{emptyText}</p>
  }

  const searchBox = services.length > 6 && (
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
  )

  const chip = (s: ServicePrice) => {
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
  }

  if (!groups) {
    return (
      <div className="flex flex-col gap-2">
        {searchBox}
        <div className="flex flex-wrap gap-2">
          {filtered.length === 0 && (
            <p className="text-sm text-muted-foreground">Không tìm thấy dịch vụ phù hợp.</p>
          )}
          {filtered.map(chip)}
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-2">
      {searchBox}
      {groups.length === 0 && (
        <p className="text-sm text-muted-foreground">Không tìm thấy dịch vụ phù hợp.</p>
      )}
      <div className="flex flex-col gap-1.5">
        {groups.map(({ key, label, items }) => {
          // Khi đang tìm kiếm, luôn mở để thấy ngay kết quả trùng — chỉ tôn trọng gấp/mở tay khi rảnh tay.
          const isCollapsed = !search.trim() && collapsed.has(key)
          const pickedInGroup = items.filter((s) => picked.includes(s.id)).length
          const Icon = key === UNGROUPED_KEY ? MoreHorizontal : groupIcons[key]
          return (
            <div key={key} className="rounded-md border">
              <button
                type="button"
                onClick={() => toggleCollapsed(key)}
                className="flex w-full items-center justify-between px-3 py-2 text-left"
              >
                <span className="flex items-center gap-2 text-sm font-medium">
                  <Icon className="size-4 text-muted-foreground" />
                  {label}
                  <span className="text-xs font-normal text-muted-foreground">({items.length})</span>
                  {pickedInGroup > 0 && (
                    <span className="rounded-full bg-primary/10 px-2 py-0.5 text-xs font-medium text-primary">
                      Đã chọn {pickedInGroup}
                    </span>
                  )}
                </span>
                <ChevronDown
                  className={`size-4 text-muted-foreground transition-transform ${isCollapsed ? '-rotate-90' : ''}`}
                />
              </button>
              {!isCollapsed && (
                <div className="flex flex-wrap gap-2 border-t px-3 py-2">{items.map(chip)}</div>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
