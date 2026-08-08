import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { getStoredUser, login as loginRequest, logout as logoutStore } from '../services/authService'
import { UserRole, type AuthUser, type LoginRequest } from '../types/auth'

interface AuthContextValue {
  user: AuthUser | null
  isAuthenticated: boolean
  /** Có quyền ghi (tạo/sửa/xoá) danh mục nghiệp vụ: Admin hoặc Lễ tân. */
  canManage: boolean
  /** Có quyền ghi bệnh án (phiếu khám/đơn thuốc): Bác sĩ hoặc Admin (ADR 0006). */
  canRecordEncounter: boolean
  login: (request: LoginRequest) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => getStoredUser())

  const login = useCallback(async (request: LoginRequest) => {
    const result = await loginRequest(request)
    setUser(result.user)
  }, [])

  const logout = useCallback(() => {
    logoutStore()
    setUser(null)
  }, [])

  const value = useMemo<AuthContextValue>(() => ({
    user,
    isAuthenticated: user !== null,
    canManage: user?.role === UserRole.Admin || user?.role === UserRole.Receptionist,
    canRecordEncounter: user?.role === UserRole.Admin || user?.role === UserRole.Doctor,
    login,
    logout,
  }), [user, login, logout])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth phải được dùng bên trong <AuthProvider>.')
  return ctx
}
