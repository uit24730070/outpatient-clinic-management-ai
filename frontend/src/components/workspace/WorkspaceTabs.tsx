import { type ReactNode } from 'react'
import { X } from 'lucide-react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import type { WorkspaceTab } from './useWorkspaceTabs'

interface WorkspaceTabsProps<T> {
  tabs: WorkspaceTab<T>[]
  active: string
  onActiveChange: (key: string) => void
  onClose: (key: string) => void
  renderContent: (tab: WorkspaceTab<T>) => ReactNode
}

/**
 * Phần hiển thị của pattern workspace (UX-02): tab hàng ngang, mỗi tab giữ nguyên nội dung khi
 * chuyển qua lại (forceMount) để không mất dữ liệu đang nhập.
 */
export function WorkspaceTabs<T>({
  tabs,
  active,
  onActiveChange,
  onClose,
  renderContent,
}: WorkspaceTabsProps<T>) {
  if (tabs.length === 0) return null

  return (
    <Tabs value={active} onValueChange={onActiveChange}>
      <TabsList className="h-auto flex-wrap">
        {tabs.map((t) => (
          <TabsTrigger key={t.key} value={t.key} className="gap-2">
            {t.label}
            <span
              role="button"
              tabIndex={-1}
              aria-label="Đóng tab"
              className="rounded p-0.5 hover:bg-muted-foreground/20"
              onClick={(e) => {
                e.stopPropagation()
                onClose(t.key)
              }}
            >
              <X className="size-3.5" />
            </span>
          </TabsTrigger>
        ))}
      </TabsList>
      {tabs.map((t) => (
        <TabsContent key={t.key} value={t.key} forceMount className="data-[state=inactive]:hidden">
          {renderContent(t)}
        </TabsContent>
      ))}
    </Tabs>
  )
}
