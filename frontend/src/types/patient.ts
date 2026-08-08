// Kiểu dữ liệu miền Bệnh nhân, khớp với API backend.

// Dùng const-map thay cho enum (tsconfig bật erasableSyntaxOnly).
export const Gender = {
  Unknown: 0,
  Male: 1,
  Female: 2,
  Other: 3,
} as const

export type GenderValue = (typeof Gender)[keyof typeof Gender]

export const genderLabels: Record<number, string> = {
  0: 'Không rõ',
  1: 'Nam',
  2: 'Nữ',
  3: 'Khác',
}

export interface Patient {
  id: string
  code: string
  fullName: string
  dateOfBirth: string | null
  gender: GenderValue
  phoneNumber: string | null
  address: string | null
  createdAt: string
  updatedAt: string | null
}

export interface PatientFormValues {
  fullName: string
  dateOfBirth: string | null
  gender: GenderValue
  phoneNumber: string | null
  address: string | null
}

// Re-export type dùng chung để giữ tương thích với các import cũ.
export type { PagedResult, ApiError, ApiResponse } from './common'
