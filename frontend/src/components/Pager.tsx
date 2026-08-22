import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'

/** Phân trang gọn: trước/sau + chỉ số trang (thay .pager). */
export function Pager({
  page,
  totalPages,
  totalCount,
  onPageChange,
}: {
  page: number
  totalPages: number
  totalCount?: number
  onPageChange: (page: number) => void
}) {
  if (totalPages <= 1) return null
  return (
    <div className="mt-4 flex items-center justify-center gap-4 text-sm text-muted-foreground">
      <Button
        variant="outline"
        size="sm"
        disabled={page <= 1}
        onClick={() => onPageChange(page - 1)}
      >
        <ChevronLeft className="size-4" />
        Trước
      </Button>
      <span>
        Trang {page}/{totalPages}
        {totalCount != null && ` · ${totalCount} bản ghi`}
      </span>
      <Button
        variant="outline"
        size="sm"
        disabled={page >= totalPages}
        onClick={() => onPageChange(page + 1)}
      >
        Sau
        <ChevronRight className="size-4" />
      </Button>
    </div>
  )
}
