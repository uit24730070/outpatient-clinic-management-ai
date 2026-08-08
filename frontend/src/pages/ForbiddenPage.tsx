import { Link } from 'react-router-dom'
import { useAuth } from '../store/auth'
import { landingPathFor } from '../config/access'

/** Trang 403 thân thiện khi truy cập route ngoài quyền (khớp 403 backend). */
export default function ForbiddenPage() {
  const { user } = useAuth()
  const home = landingPathFor(user?.role)

  return (
    <div className="page">
      <h1>Không có quyền truy cập</h1>
      <p>Bạn không được phép xem trang này với vai trò hiện tại.</p>
      <p>
        <Link to={home}>← Về trang chính của bạn</Link>
      </p>
    </div>
  )
}
