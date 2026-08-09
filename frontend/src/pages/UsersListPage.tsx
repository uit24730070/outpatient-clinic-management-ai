import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import {
  activateUser,
  deactivateUser,
  deleteUser,
  listUsers,
  resetUserPassword,
} from '../services/userService'
import { toApiException } from '../services/apiClient'
import { useAuth } from '../store/auth'
import { UserRole, roleLabels, type UserRoleValue } from '../types/auth'
import type { PagedResult } from '../types/common'
import type { UserListItem } from '../types/user'

const PAGE_SIZE = 10

export default function UsersListPage() {
  const { user } = useAuth()
  const [search, setSearch] = useState('')
  const [role, setRole] = useState<UserRoleValue | ''>('')
  const [status, setStatus] = useState<'' | 'active' | 'inactive'>('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<UserListItem> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
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
      setError(toApiException(err).message)
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
      void load()
    } catch (err) {
      setError(toApiException(err).message)
    }
  }

  const onResetPassword = async (u: UserListItem) => {
    const newPassword = window.prompt(`Đặt lại mật khẩu cho "${u.username}". Nhập mật khẩu mới (tối thiểu 8 ký tự, có chữ và số):`)
    if (!newPassword) return
    try {
      await resetUserPassword(u.id, newPassword)
      window.alert('Đã đặt lại mật khẩu.')
    } catch (err) {
      setError(toApiException(err).message)
    }
  }

  const onDelete = async (u: UserListItem) => {
    if (!window.confirm(`Xoá tài khoản "${u.username}"?`)) return
    try {
      await deleteUser(u.id)
      void load()
    } catch (err) {
      setError(toApiException(err).message)
    }
  }

  return (
    <section>
      <div className="page-head">
        <h1>Quản lý người dùng</h1>
        <Link className="btn btn--primary" to="/users/new">+ Thêm người dùng</Link>
      </div>

      <form className="toolbar" onSubmit={onSearchSubmit}>
        <input
          type="search"
          placeholder="Tìm theo tên đăng nhập, họ tên, email…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <select value={role} onChange={(e) => { setRole(e.target.value as UserRoleValue | ''); setPage(1) }}>
          <option value="">— Mọi vai trò —</option>
          {Object.values(UserRole).map((r) => (
            <option key={r} value={r}>{roleLabels[r]}</option>
          ))}
        </select>
        <select value={status} onChange={(e) => { setStatus(e.target.value as '' | 'active' | 'inactive'); setPage(1) }}>
          <option value="">— Mọi trạng thái —</option>
          <option value="active">Đang hoạt động</option>
          <option value="inactive">Đã khoá</option>
        </select>
        <button className="btn" type="submit">Tìm</button>
      </form>

      {error && <p className="alert alert--error">{error}</p>}
      {loading && <p>Đang tải…</p>}

      {data && (
        <>
          <table className="table">
            <thead>
              <tr>
                <th>Tên đăng nhập</th>
                <th>Họ tên</th>
                <th>Vai trò</th>
                <th>Email</th>
                <th>Trạng thái</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 && (
                <tr><td colSpan={6} className="table__empty">Không có người dùng nào.</td></tr>
              )}
              {data.items.map((u) => {
                const isSelf = u.id === user?.id
                return (
                  <tr key={u.id}>
                    <td>{u.username}{isSelf && <em> (bạn)</em>}</td>
                    <td>{u.fullName}</td>
                    <td>{roleLabels[u.role] ?? u.role}</td>
                    <td>{u.email ?? '—'}</td>
                    <td>
                      <span className={`badge ${u.isActive ? 'badge--completed' : 'badge--cancelled'}`}>
                        {u.isActive ? 'Hoạt động' : 'Đã khoá'}
                      </span>
                    </td>
                    <td className="table__actions">
                      <Link to={`/users/${u.id}/edit`}>Sửa</Link>
                      <button className="link-btn" onClick={() => onResetPassword(u)}>Đặt lại MK</button>
                      {!isSelf && (
                        <>
                          <button className="link-btn" onClick={() => onToggleActive(u)}>
                            {u.isActive ? 'Khoá' : 'Mở khoá'}
                          </button>
                          <button className="link-btn link-btn--danger" onClick={() => onDelete(u)}>Xoá</button>
                        </>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>

          <div className="pager">
            <button className="btn" disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>
              ← Trước
            </button>
            <span>Trang {data.page}/{Math.max(data.totalPages, 1)} · {data.totalCount} bản ghi</span>
            <button className="btn" disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>
              Sau →
            </button>
          </div>
        </>
      )}
    </section>
  )
}
