import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../store/auth'
import { roleLabels } from '../types/auth'
import { navItemsFor } from '../config/access'

export default function MainLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const onLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  // Menu hiển thị theo vai trò (cấu hình khai báo ở config/access).
  const items = navItemsFor(user?.role)

  return (
    <div className="layout">
      <header className="layout__header">
        <div className="layout__brand">Clinic Management AI</div>
        <nav className="layout__nav">
          {items.map((item) => (
            <NavLink key={item.to} to={item.to}>{item.label}</NavLink>
          ))}
        </nav>
        {user && (
          <div className="layout__user">
            <span className="layout__user-name">
              {user.fullName} · {roleLabels[user.role] ?? user.role}
            </span>
            <button className="link-btn" onClick={onLogout}>Đăng xuất</button>
          </div>
        )}
      </header>
      <main className="layout__main">
        <Outlet />
      </main>
    </div>
  )
}
