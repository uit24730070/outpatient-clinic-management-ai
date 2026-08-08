import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../store/auth'

/** Chặn truy cập khi chưa đăng nhập; chuyển về /login và ghi nhớ trang đích. */
export default function RequireAuth() {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}
