import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../store/auth'
import { roleLabels } from '../types/auth'

export default function MainLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  const onLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="layout">
      <header className="layout__header">
        <div className="layout__brand">Clinic Management AI</div>
        <nav className="layout__nav">
          <NavLink to="/appointments">Lịch khám</NavLink>
          <NavLink to="/patients">Bệnh nhân</NavLink>
          <NavLink to="/doctors">Bác sĩ</NavLink>
          <NavLink to="/specialties">Chuyên khoa</NavLink>
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
