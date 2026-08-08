import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../store/auth'
import type { UserRoleValue } from '../types/auth'

interface RequireRoleProps {
  /** Vai trò được phép vào nhóm route này. */
  roles: UserRoleValue[]
}

/**
 * Guard route theo vai trò (ADR 0009). Đặt bên trong <RequireAuth> (đã đảm bảo đăng nhập).
 * Ngoài quyền → chuyển tới trang 403 thân thiện. FE guard chỉ là UX; backend vẫn chốt bằng 403.
 */
export default function RequireRole({ roles }: RequireRoleProps) {
  const { user } = useAuth()

  if (user && !roles.includes(user.role)) {
    return <Navigate to="/forbidden" replace />
  }

  return <Outlet />
}
