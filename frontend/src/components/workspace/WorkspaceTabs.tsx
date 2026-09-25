import { type ReactNode } from 'react'
import { X } from 'lucide-react'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import type { WorkspaceTab } from './useWorkspaceTabs'

interface WorkspaceTabsProps<T> {
  tabs: WorkspaceTab<T>[]
  active: string
  onActiveChange: (key: string) => void
  onClose: (key: string) => void
  renderContent: (tab: WorkspaceTab<T>) => ReactNode
  /**
   * 'center' (mặc định): dialog lớn giữa màn hình — dùng cho màn hình thao tác chính (vd khám bệnh)
   * cần nhiều chỗ và dễ nhìn thẳng, không lệch sang một bên. 'side': panel trượt từ phải, gọn hơn —
   * hợp với thao tác nhanh/phụ (vd thu tiền, tiếp nhận) không cần chiếm hết tầm nhìn.
   */
  variant?: 'center' | 'side'
}

function TabStrip<T>({
  tabs,
  active,
  onActiveChange,
  onClose,
}: Pick<WorkspaceTabsProps<T>, 'tabs' | 'active' | 'onActiveChange' | 'onClose'>) {
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
    </Tabs>
  )
}

/**
 * Phần hiển thị của pattern workspace (UX-02): panel chi tiết đè lên trang thay vì chia cột cố
 * định — danh sách/bảng phía sau giữ nguyên toàn bộ chiều rộng, không bị co nhỏ hay cuộn ngang (đổi
 * từ split-pane sau phản hồi UX). Nhiều tab vẫn mở song song, giữ nguyên nội dung khi chuyển qua lại
 * (mount cả — chỉ ẩn bằng `hidden`) để không mất dữ liệu đang nhập. Đóng panel (nút X, Esc, bấm ra
 * ngoài) coi như đóng tab đang xem — an toàn vì nháp lưu server-side (đóng/mở lại vẫn nạp đúng);
 * muốn mở thêm bệnh nhân/lượt khác trong khi đang xem, đóng panel rồi chọn dòng khác.
 */
export function WorkspaceTabs<T>({
  tabs,
  active,
  onActiveChange,
  onClose,
  renderContent,
  variant = 'center',
}: WorkspaceTabsProps<T>) {
  const activeTab = tabs.find((t) => t.key === active)
  const open = tabs.length > 0
  const onOpenChange = (next: boolean) => !next && active && onClose(active)

  const content = tabs.map((t) => (
    <div key={t.key} hidden={t.key !== active}>
      {renderContent(t)}
    </div>
  ))

  if (variant === 'side') {
    return (
      <Sheet open={open} onOpenChange={onOpenChange}>
        <SheetContent className="w-full gap-0 p-0 sm:max-w-2xl lg:max-w-3xl">
          <SheetHeader className="shrink-0 border-b pb-3">
            <SheetTitle className="sr-only">{activeTab?.label ?? 'Chi tiết'}</SheetTitle>
            <TabStrip tabs={tabs} active={active} onActiveChange={onActiveChange} onClose={onClose} />
          </SheetHeader>
          <div className="flex-1 overflow-y-auto px-4 pb-4">{content}</div>
        </SheetContent>
      </Sheet>
    )
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="flex h-[90vh] w-[95vw] max-w-6xl flex-col gap-0 overflow-hidden p-0 sm:max-w-6xl">
        <DialogHeader className="shrink-0 border-b px-6 py-3 text-left">
          <DialogTitle className="sr-only">{activeTab?.label ?? 'Chi tiết'}</DialogTitle>
          <TabStrip tabs={tabs} active={active} onActiveChange={onActiveChange} onClose={onClose} />
        </DialogHeader>
        <div className="flex-1 overflow-y-auto px-6 py-4">{content}</div>
      </DialogContent>
    </Dialog>
  )
}
