// Kiểu dữ liệu miền Chuyên khoa, khớp với API backend.

export interface Specialty {
  id: string
  name: string
  description: string | null
  createdAt: string
  updatedAt: string | null
}

export interface SpecialtyFormValues {
  name: string
  description: string | null
}
