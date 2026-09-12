// Kiểu dữ liệu miền Sinh hiệu, khớp với API backend (QN-02, ADR 0019).

export interface Vitals {
  id: string
  appointmentId: string
  patientId: string
  heightCm: number | null
  weightKg: number | null
  bmi: number | null
  temperatureC: number | null
  pulse: number | null
  bloodPressureSystolic: number | null
  bloodPressureDiastolic: number | null
  spO2: number | null
  respiratoryRate: number | null
  notes: string | null
  measuredAt: string
  measuredBy: string
  measuredByName: string | null
  createdAt: string
  updatedAt: string | null
}

// Dữ liệu form nhập sinh hiệu (mọi chỉ số tuỳ chọn; BMI backend tính).
export interface VitalsFormValues {
  heightCm: number | null
  weightKg: number | null
  temperatureC: number | null
  pulse: number | null
  bloodPressureSystolic: number | null
  bloodPressureDiastolic: number | null
  spO2: number | null
  respiratoryRate: number | null
  notes: string | null
}
