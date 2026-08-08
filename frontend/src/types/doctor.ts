// Kiểu dữ liệu miền Bác sĩ, khớp với API backend.

export interface Doctor {
  id: string
  code: string
  fullName: string
  specialtyId: string
  specialtyName: string | null
  phoneNumber: string | null
  email: string | null
  createdAt: string
  updatedAt: string | null
}

export interface DoctorFormValues {
  fullName: string
  specialtyId: string
  phoneNumber: string | null
  email: string | null
}
