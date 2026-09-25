import { useCallback, useState } from 'react'

export interface SortState {
  sortBy?: string
  sortDesc?: boolean
}

/**
 * Trạng thái sắp xếp bảng (server-side) dùng chung cho các trang danh sách: bấm cột lần đầu →
 * tăng dần, bấm lại cùng cột → đảo chiều, bấm cột khác → chuyển cột (tăng dần).
 */
export function useSort(onChange?: () => void) {
  const [sortBy, setSortBy] = useState<string | undefined>(undefined)
  const [sortDesc, setSortDesc] = useState(false)

  const toggleSort = useCallback(
    (field: string) => {
      if (sortBy === field) {
        setSortDesc((d) => !d)
      } else {
        setSortBy(field)
        setSortDesc(false)
      }
      onChange?.()
    },
    [sortBy, onChange],
  )

  return { sort: { sortBy, sortDesc } as SortState, toggleSort }
}
