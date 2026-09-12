import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import {
  Calendar,
  Users,
  Stethoscope,
  Layers,
  UserCog,
  Pill,
  PackagePlus,
  TriangleAlert,
  Bot,
  Hospital,
  Receipt,
  Banknote,
  LogOut,
  Menu,
  ChevronDown,
} from 'lucide-react'
import { useAuth } from '../store/auth'
import { roleLabels } from '../types/auth'
import { navGroupsFor } from '../config/access'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Sheet, SheetContent, SheetTrigger, SheetTitle } from '@/components/ui/sheet'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Toaster } from '@/components/ui/sonner'

// Icon theo đường dẫn nav — tra cứu để không phải sửa config/access.ts (nguồn sự thật RBAC).
const navIcons: Record<string, typeof Calendar> = {
  '/my-clinic': Hospital,
  '/appointments': Calendar,
  '/patients': Users,
  '/doctors': Stethoscope,
  '/specialties': Layers,
  '/users': UserCog,
  '/medications': Pill,
  '/stock-receipts': PackagePlus,
  '/pharmacy/alerts': TriangleAlert,
  '/invoices': Receipt,
  '/service-prices': Banknote,
  '/assistant': Bot,
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/)
  const first = parts[0]?.[0] ?? ''
  const last = parts.length > 1 ? parts[parts.length - 1][0] : ''
  return (first + last).toUpperCase() || '?'
}

export default function MainLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const [mobileOpen, setMobileOpen] = useState(false)

  const onLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  // Menu hiển thị theo vai trò, gom nhóm (cấu hình khai báo ở config/access) — Admin thấy nhiều
  // mục nhất nên nhóm giúp sidebar đỡ rối; vai trò ít mục vẫn gọn vì nhóm rỗng bị lược bỏ.
  const groups = navGroupsFor(user?.role)
  // Chỉ 1 nhóm (menu ngắn) thì header nhóm chỉ thừa chữ, không cần hiện.
  const showGroupLabels = groups.length > 1

  const navList = (
    <nav className="flex flex-col gap-4 px-3">
      {groups.map(({ group, items }) => (
        <div key={group} className="flex flex-col gap-1">
          {showGroupLabels && (
            <p className="px-3 pb-1 text-xs font-semibold uppercase tracking-wide text-sidebar-foreground/40">
              {group}
            </p>
          )}
          {items.map((item) => {
            const Icon = navIcons[item.to] ?? Calendar
            return (
              <NavLink
                key={item.to}
                to={item.to}
                onClick={() => setMobileOpen(false)}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-sidebar-primary text-sidebar-primary-foreground shadow-sm'
                      : 'text-sidebar-foreground/70 hover:bg-sidebar-accent hover:text-sidebar-foreground',
                  )
                }
              >
                <Icon className="size-4 shrink-0" />
                <span className="truncate">{item.label}</span>
              </NavLink>
            )
          })}
        </div>
      ))}
    </nav>
  )

  const brand = (
    <div className="flex items-center gap-2 px-6 py-4">
      <div className="flex size-9 items-center justify-center rounded-lg bg-primary text-primary-foreground">
        <Hospital className="size-5" />
      </div>
      <div className="leading-tight">
        <div className="text-sm font-bold">Clinic AI</div>
        <div className="text-xs text-muted-foreground">Quản lý phòng khám</div>
      </div>
    </div>
  )

  return (
    <div className="min-h-screen bg-muted/30">
      {/* Sidebar cố định cho desktop */}
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-64 flex-col border-r border-sidebar-border bg-sidebar lg:flex">
        {brand}
        <div className="mt-2 flex-1 overflow-y-auto pb-4">{navList}</div>
      </aside>

      {/* Vùng nội dung */}
      <div className="lg:pl-64">
        {/* Topbar */}
        <header className="sticky top-0 z-20 flex h-16 items-center gap-3 border-b bg-background/95 px-4 backdrop-blur supports-[backdrop-filter]:bg-background/80 lg:px-8">
          {/* Nút mở menu trên mobile */}
          <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
            <SheetTrigger asChild>
              <Button variant="ghost" size="icon" className="lg:hidden">
                <Menu className="size-5" />
              </Button>
            </SheetTrigger>
            <SheetContent side="left" className="w-64 bg-sidebar p-0">
              <SheetTitle className="sr-only">Điều hướng</SheetTitle>
              {brand}
              <div className="mt-2">{navList}</div>
            </SheetContent>
          </Sheet>

          <div className="flex-1" />

          {user && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" className="h-10 gap-2 px-2">
                  <Avatar className="size-8">
                    <AvatarFallback className="bg-primary/10 text-xs font-semibold text-primary">
                      {initials(user.fullName)}
                    </AvatarFallback>
                  </Avatar>
                  <div className="hidden text-left leading-tight sm:block">
                    <div className="text-sm font-medium">{user.fullName}</div>
                    <div className="text-xs text-muted-foreground">
                      {roleLabels[user.role] ?? user.role}
                    </div>
                  </div>
                  <ChevronDown className="size-4 text-muted-foreground" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-52">
                <DropdownMenuLabel>
                  <div className="font-medium">{user.fullName}</div>
                  <div className="text-xs font-normal text-muted-foreground">
                    @{user.username}
                  </div>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={onLogout} variant="destructive">
                  <LogOut className="size-4" />
                  Đăng xuất
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          )}
        </header>

        <main className="mx-auto max-w-6xl p-4 lg:p-8">
          <Outlet />
        </main>
      </div>

      <Toaster richColors position="top-right" />
    </div>
  )
}
