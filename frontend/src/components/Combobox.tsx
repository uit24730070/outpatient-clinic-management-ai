import { useEffect, useState } from 'react'
import { Check, ChevronsUpDown, Search } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'

export interface ComboboxOption {
  value: string
  label: string
  /** Dòng phụ nhỏ dưới label (vd chuyên khoa, tồn kho…), tuỳ chọn. */
  description?: string
}

interface ComboboxProps {
  value: string
  onValueChange: (value: string) => void
  /** Danh sách nguồn — đã tải sẵn (client lọc theo `label`) hoặc đã lọc sẵn từ server (dùng kèm `onSearchChange`). */
  options: ComboboxOption[]
  /** Khi có: gọi lại mỗi lần gõ tìm kiếm (vd để gọi API tìm theo server) thay vì tự lọc `options` phía client. */
  onSearchChange?: (query: string) => void
  placeholder?: string
  searchPlaceholder?: string
  emptyText?: string
  disabled?: boolean
  className?: string
}

/**
 * Select có ô tìm kiếm — thay `Select` thường khi danh sách dài (bác sĩ, dịch vụ, thuốc…) khiến cuộn
 * chọn thủ công bất tiện. Không phụ thuộc `cmdk` (chưa cài) — tự dựng trên `Popover` (radix-ui) + danh
 * sách lọc thường, đủ dùng cho quy mô danh mục hiện tại.
 */
export function Combobox({
  value,
  onValueChange,
  options,
  onSearchChange,
  placeholder = '— Chọn —',
  searchPlaceholder = 'Tìm kiếm…',
  emptyText = 'Không có kết quả.',
  disabled,
  className,
}: ComboboxProps) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const selected = options.find((o) => o.value === value)

  useEffect(() => {
    if (!open) {
      setQuery('')
      onSearchChange?.('')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open])

  const filtered = onSearchChange
    ? options
    : options.filter((o) => o.label.toLowerCase().includes(query.trim().toLowerCase()))

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          type="button"
          variant="outline"
          role="combobox"
          aria-expanded={open}
          disabled={disabled}
          className={cn(
            'w-full justify-between bg-transparent px-3 font-normal shadow-xs hover:bg-transparent',
            !selected && 'text-muted-foreground',
            className,
          )}
        >
          <span className="truncate">{selected ? selected.label : placeholder}</span>
          <ChevronsUpDown className="size-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent
        align="start"
        className="w-(--radix-popover-trigger-width) p-0"
      >
        <div className="flex items-center gap-2 border-b px-3">
          <Search className="size-4 shrink-0 text-muted-foreground" />
          <input
            autoFocus
            value={query}
            onChange={(e) => {
              setQuery(e.target.value)
              onSearchChange?.(e.target.value)
            }}
            placeholder={searchPlaceholder}
            className="flex h-9 w-full bg-transparent py-2 text-sm outline-none placeholder:text-muted-foreground"
          />
        </div>
        <div className="max-h-64 overflow-y-auto p-1">
          {filtered.length === 0 && (
            <p className="py-6 text-center text-sm text-muted-foreground">{emptyText}</p>
          )}
          {filtered.map((o) => (
            <button
              key={o.value}
              type="button"
              onClick={() => {
                onValueChange(o.value)
                setOpen(false)
              }}
              className={cn(
                'flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-left text-sm hover:bg-accent hover:text-accent-foreground',
                o.value === value && 'bg-accent/60',
              )}
            >
              <Check className={cn('size-4 shrink-0', o.value === value ? 'opacity-100' : 'opacity-0')} />
              <span className="flex min-w-0 flex-col">
                <span className="truncate">{o.label}</span>
                {o.description && (
                  <span className="truncate text-xs text-muted-foreground">{o.description}</span>
                )}
              </span>
            </button>
          ))}
        </div>
      </PopoverContent>
    </Popover>
  )
}
