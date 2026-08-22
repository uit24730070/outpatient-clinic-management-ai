// Kiểu dữ liệu miền Xác thực, khớp với API backend.

// Const-map thay cho enum (tsconfig bật erasableSyntaxOnly).
export const UserRole = {
  Admin: 'Admin',
  Receptionist: 'Receptionist',
  Doctor: 'Doctor',
  Pharmacist: 'Pharmacist',
} as const

export type UserRoleValue = (typeof UserRole)[keyof typeof UserRole]

export const roleLabels: Record<string, string> = {
  Admin: 'Quản trị',
  Receptionist: 'Lễ tân',
  Doctor: 'Bác sĩ',
  Pharmacist: 'Dược sĩ',
}

export interface AuthUser {
  id: string
  username: string
  fullName: string
  role: UserRoleValue
  email: string | null
  /** Hồ sơ bác sĩ gắn với tài khoản (nếu là user Bác sĩ đã liên kết); null nếu chưa gắn (ADR 0009). */
  doctorId: string | null
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
