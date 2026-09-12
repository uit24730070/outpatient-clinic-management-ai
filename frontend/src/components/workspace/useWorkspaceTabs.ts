import { useState } from 'react'

/**
 * Pattern "workspace theo vai trò" (Epic 17, UX-02): danh sách trên + nhiều tab chi tiết/hành
 * động nhanh mở song song bên dưới, đóng/mở độc lập. Rút từ `/my-clinic` (Sprint 15) để dùng lại
 * cho các workspace khác (vd `/front-desk`).
 */
export interface WorkspaceTab<T> {
  key: string
  label: string
  data: T
}

export function useWorkspaceTabs<T>() {
  const [tabs, setTabs] = useState<WorkspaceTab<T>[]>([])
  const [active, setActive] = useState('')

  const openTab = (key: string, label: string, data: T) => {
    setTabs((cur) => (cur.some((t) => t.key === key) ? cur : [...cur, { key, label, data }]))
    setActive(key)
  }

  const closeTab = (key: string) => {
    setTabs((cur) => {
      const next = cur.filter((t) => t.key !== key)
      setActive((curActive) => (curActive === key ? (next[next.length - 1]?.key ?? '') : curActive))
      return next
    })
  }

  return { tabs, active, setActive, openTab, closeTab }
}
