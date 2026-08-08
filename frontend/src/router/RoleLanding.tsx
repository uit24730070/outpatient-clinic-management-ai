import { Navigate } from 'react-router-dom'
import { useAuth } from '../store/auth'
import { landingPathFor } from '../config/access'

/** Điều hướng "/" tới trang mặc định theo vai trò (ADR 0009). */
export default function RoleLanding() {
  const { user } = useAuth()
  return <Navigate to={landingPathFor(user?.role)} replace />
}
