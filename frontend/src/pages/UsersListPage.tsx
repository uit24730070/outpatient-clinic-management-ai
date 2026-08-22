import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Search, Pencil, KeyRound, Lock, Unlock } from 'lucide-react'
import {
  activateUser,
  deactivateUser,
  deleteUser,
  listUsers,
  resetUserPassword,
} from '../services/userService'
import { useAuth } from '../store/auth'
import { toastError, toastSuccess } from '../lib/toast'
import { UserRole, roleLabels, type UserRoleValue } from '../types/auth'
import type { PagedResult } from '../types/common'
import type { UserListItem } from '../types/user'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { RoleBadge, ActiveBadge } from '../components/StatusBadge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const PAGE_SIZE = 10
const ALL = 'all'

export default function UsersListPage() {
  const { user } = useAuth()
  const [search, setSearch] = useState('')
  const [role, setRole] = useState<UserRoleValue | ''>('')
  const [status, setStatus] = useState<'' | 'active' | 'inactive'>('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<UserListItem> | null>(null)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listUsers({
        page,
        pageSize: PAGE_SIZE,
        search: search.trim() || undefined,
        role: role || undefined,
        isActive: status === '' ? undefined : status === 'active',
      })
      setData(result)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [page, search, role, status])

  useEffect(() => {
    void load()
  }, [load])

  const onSearchSubmit = (e: FormEvent) => {
    e.preventDefault()
    setPage(1)
    void load()
  }

  const onToggleActive = async (u: UserListItem) => {
    try {
      if (u.isActive) await deactivateUser(u.id)
      else await activateUser(u.id)
      toastSuccess(u.isActive ? 'Đã khoá tài khoản.' : 'Đã mở khoá tài khoản.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const onResetPassword = async (u: UserListItem) => {
    const newPassword = window.prompt(
      `Đặt lại mật khẩu cho "${u.username}". Nhập mật khẩu mới (tối thiểu 8 ký tự, có chữ và số):`,
    )
    if (!newPassword) return
    try {
      await resetUserPassword(u.id, newPassword)
      toastSuccess('Đã đặt lại mật khẩu.')
    } catch (err) {
      toastError(err)
    }
  }

  const onDelete = async (u: UserListItem) => {
    try {
      await deleteUser(u.id)
      toastSuccess('Đã xoá tài khoản.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  return (
    <section>
      <PageHeader
        title="Quản lý người dùng"
        description="Tài khoản đăng nhập và phân quyền"
        actions={
          <Button asChild>
            <Link to="/users/new">
              <Plus className="size-4" />
              Thêm người dùng
            </Link>
          </Button>
        }
      />

      <Card className="mb-4">
        <CardContent>
          <form className="flex flex-wrap items-center gap-2" onSubmit={onSearchSubmit}>
            <Input
              type="search"
              className="max-w-xs"
              placeholder="Tìm theo tên đăng nhập, họ tên, email…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <Select
              value={role || ALL}
              onValueChange={(v) => {
                setRole(v === ALL ? '' : (v as UserRoleValue))
                setPage(1)
              }}
            >
              <SelectTrigger className="w-[160px]">
                <SelectValue placeholder="Vai trò" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL}>Mọi vai trò</SelectItem>
                {Object.values(UserRole).map((r) => (
                  <SelectItem key={r} value={r}>
                    {roleLabels[r]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select
              value={status || ALL}
              onValueChange={(v) => {
                setStatus(v === ALL ? '' : (v as 'active' | 'inactive'))
                setPage(1)
              }}
            >
              <SelectTrigger className="w-[160px]">
                <SelectValue placeholder="Trạng thái" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL}>Mọi trạng thái</SelectItem>
                <SelectItem value="active">Đang hoạt động</SelectItem>
                <SelectItem value="inactive">Đã khoá</SelectItem>
              </SelectContent>
            </Select>
            <Button type="submit" variant="secondary">
              <Search className="size-4" />
              Tìm
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tên đăng nhập</TableHead>
                <TableHead>Họ tên</TableHead>
                <TableHead>Vai trò</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Không có người dùng nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((u) => {
                  const isSelf = u.id === user?.id
                  return (
                    <TableRow key={u.id}>
                      <TableCell className="font-medium">
                        {u.username}
                        {isSelf && <span className="ml-1 text-xs text-muted-foreground">(bạn)</span>}
                      </TableCell>
                      <TableCell>{u.fullName}</TableCell>
                      <TableCell>
                        <RoleBadge role={u.role} />
                      </TableCell>
                      <TableCell>{u.email ?? '—'}</TableCell>
                      <TableCell>
                        <ActiveBadge active={u.isActive} />
                      </TableCell>
                      <TableCell>
                        <div className="flex flex-wrap items-center justify-end gap-1">
                          <Button asChild size="sm" variant="ghost">
                            <Link to={`/users/${u.id}/edit`}>
                              <Pencil className="size-4" />
                              Sửa
                            </Link>
                          </Button>
                          <Button size="sm" variant="ghost" onClick={() => void onResetPassword(u)}>
                            <KeyRound className="size-4" />
                            Đặt lại MK
                          </Button>
                          {!isSelf && (
                            <>
                              <Button size="sm" variant="ghost" onClick={() => void onToggleActive(u)}>
                                {u.isActive ? (
                                  <>
                                    <Lock className="size-4" />
                                    Khoá
                                  </>
                                ) : (
                                  <>
                                    <Unlock className="size-4" />
                                    Mở khoá
                                  </>
                                )}
                              </Button>
                              <ConfirmDialog
                                trigger={
                                  <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive">
                                    Xoá
                                  </Button>
                                }
                                title="Xoá tài khoản?"
                                description={`Xoá tài khoản "${u.username}"? Hành động này không thể hoàn tác.`}
                                confirmText="Xoá"
                                destructive
                                onConfirm={() => void onDelete(u)}
                              />
                            </>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                })}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {data && (
        <Pager
          page={data.page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          onPageChange={setPage}
        />
      )}
    </section>
  )
}
