// Kiểu dữ liệu miền Quản lý người dùng (Admin), khớp với API backend.

import type { UserRoleValue } from './auth'

export interface UserListItem {
  id: string
  username: string
  fullName: string
  role: UserRoleValue
  email: string | null
  isActive: boolean
  /** Hồ sơ bác sĩ gắn với tài khoản (null nếu chưa gắn / không phải bác sĩ). */
  doctorId: string | null
  createdAt: string
  updatedAt: string | null
}

export interface CreateUserValues {
  username: string
  password: string
  fullName: string
  role: UserRoleValue
  email: string | null
}

export interface UpdateUserValues {
  fullName: string
  role: UserRoleValue
  email: string | null
}
