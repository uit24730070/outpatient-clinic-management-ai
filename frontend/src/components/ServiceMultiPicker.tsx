import { useMemo, useState } from 'react'
import { Activity, ChevronDown, Microscope, MoreHorizontal, ScanLine, Search, TestTube } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { Table, TableBody, TableCell, TableRow } from '@/components/ui/table'
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
 * Chọn nhiều dịch vụ dạng bảng 2 cột (tên · đơn giá, theo gợi ý giảng viên — trước là dải chip khó
 * so sánh giá) kèm ô tìm kiếm lọc theo tên — dùng ở mọi màn chỉ định/đăng ký CLS (`VisitForm`,
 * `VisitDetailPage`, `LabOrderPanel`). Dịch vụ Cận lâm sàng (nhiều nhất — có thể 20-30 mục) được gom
 * theo `group` (xét nghiệm/chẩn đoán hình ảnh/thăm dò chức năng/nội soi, ADR 0024) thành từng khối
 * gấp mở được, mỗi khối một bảng riêng, thay vì một danh sách dài. Dịch vụ chưa phân nhóm/loại khác
 * vẫn hiển thị một bảng phẳng như trước (không đủ dữ liệu để gom).
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
  // khám…) vẫn hiển thị một bảng phẳng như cũ.
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

  // Bảng 2 cột: Tên dịch vụ (+ checkbox) · Đơn giá — click cả dòng để bật/tắt, không chỉ ô vuông.
  const serviceTable = (items: ServicePrice[]) => (
    <Table>
      <TableBody>
        {items.map((s) => {
          const on = picked.includes(s.id)
          return (
            <TableRow
              key={s.id}
              onClick={() => onToggle(s.id)}
              className={`cursor-pointer ${on ? 'bg-primary/5' : ''}`}
            >
              <TableCell className="w-8 py-2">
                <input
                  type="checkbox"
                  checked={on}
                  onChange={() => onToggle(s.id)}
                  onClick={(e) => e.stopPropagation()}
                  className="size-4 rounded border-input accent-primary"
                  aria-label={s.name}
                />
              </TableCell>
              <TableCell className={`py-2 ${on ? 'font-medium text-primary' : ''}`}>{s.name}</TableCell>
              <TableCell className="py-2 text-right tabular-nums text-muted-foreground">
                {formatVnd(s.unitPrice)}
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )

  if (!groups) {
    return (
      <div className="flex flex-col gap-2">
        {searchBox}
        {filtered.length === 0 ? (
          <p className="text-sm text-muted-foreground">Không tìm thấy dịch vụ phù hợp.</p>
        ) : (
          <div className="rounded-md border">{serviceTable(filtered)}</div>
        )}
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
              {!isCollapsed && <div className="border-t">{serviceTable(items)}</div>}
            </div>
          )
        })}
      </div>
    </div>
  )
}
