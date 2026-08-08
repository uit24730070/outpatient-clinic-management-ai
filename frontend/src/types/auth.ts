// Kiểu dữ liệu miền Xác thực, khớp với API backend.

// Const-map thay cho enum (tsconfig bật erasableSyntaxOnly).
export const UserRole = {
  Admin: 'Admin',
  Receptionist: 'Receptionist',
  Doctor: 'Doctor',
} as const

export type UserRoleValue = (typeof UserRole)[keyof typeof UserRole]

export const roleLabels: Record<string, string> = {
  Admin: 'Quản trị',
  Receptionist: 'Lễ tân',
  Doctor: 'Bác sĩ',
}

export interface AuthUser {
  id: string
  username: string
  fullName: string
  role: UserRoleValue
  email: string | null
}

export interface AuthResult {
  accessToken: string
  expiresAt: string
  user: AuthUser
}

export interface LoginRequest {
  username: string
  password: string
}
