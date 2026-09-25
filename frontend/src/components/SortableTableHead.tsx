import { type ReactNode } from 'react'
import { ArrowDown, ArrowUp, ArrowUpDown } from 'lucide-react'
import { type SortState } from '../hooks/useSort'
import { cn } from '@/lib/utils'
import { TableHead } from '@/components/ui/table'

/**
 * Ô tiêu đề bảng bấm được để sắp xếp server-side — dùng cùng `useSort` ở mỗi trang danh sách.
 */
export function SortableTableHead({
  field,
  sort,
  onSort,
  className,
  align,
  children,
}: {
  field: string
  sort: SortState
  onSort: (field: string) => void
  className?: string
  /** Căn phải nút bấm (cột số liệu) — truyền cùng lúc `className="text-right"` trên TableHead. */
  align?: 'left' | 'right'
  children: ReactNode
}) {
  const active = sort.sortBy === field
  const Icon = active ? (sort.sortDesc ? ArrowDown : ArrowUp) : ArrowUpDown

  return (
    <TableHead className={className}>
      <button
        type="button"
        className={cn(
          'inline-flex items-center gap-1 hover:text-foreground',
          align === 'right' && 'w-full justify-end',
          active ? 'text-foreground' : 'text-muted-foreground',
        )}
        onClick={() => onSort(field)}
      >
        {children}
        <Icon className="size-3.5" />
      </button>
    </TableHead>
  )
}
