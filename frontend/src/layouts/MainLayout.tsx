import { NavLink, Outlet } from 'react-router-dom'

export default function MainLayout() {
  return (
    <div className="layout">
      <header className="layout__header">
        <div className="layout__brand">Clinic Management AI</div>
        <nav className="layout__nav">
          <NavLink to="/patients">Bệnh nhân</NavLink>
        </nav>
      </header>
      <main className="layout__main">
        <Outlet />
      </main>
    </div>
  )
}
