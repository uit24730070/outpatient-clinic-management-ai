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

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface ApiError {
  code: string
  message: string
  details?: Record<string, string[]> | null
}

export interface ApiResponse<T> {
  success: boolean
  data: T | null
  error: ApiError | null
  meta: unknown
}
