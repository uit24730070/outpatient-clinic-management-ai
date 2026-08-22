// Kiểu dữ liệu miền Phiếu khám (bệnh án), khớp với API backend.

// Const-map thay cho enum (tsconfig bật erasableSyntaxOnly). Giá trị số khớp
// EncounterStatus phía backend (serialize enum thành số, như AppointmentStatus).
export const EncounterStatus = {
  Draft: 0,
  Completed: 1,
} as const

export type EncounterStatusValue = (typeof EncounterStatus)[keyof typeof EncounterStatus]

export const encounterStatusLabels: Record<number, string> = {
  0: 'Nháp',
  1: 'Đã chốt',
}

export const encounterStatusClass: Record<number, string> = {
  0: 'badge--scheduled',
  1: 'badge--completed',
}

export interface PrescriptionItem {
  /** Thuốc trong danh mục (null = thuốc ngoài danh mục, không trừ tồn). */
  medicationId: string | null
  drugName: string
  dosage: string
  quantity: number
  instruction: string | null
}

export interface Encounter {
  id: string
  appointmentId: string
  patientId: string
  patientName: string | null
  doctorId: string
  doctorName: string | null
  symptoms: string | null
  diagnosis: string
  notes: string | null
  status: EncounterStatusValue
  prescriptionItems: PrescriptionItem[]
  /** Thời điểm đã cấp phát thuốc (trừ tồn FEFO) — null nếu chưa cấp phát. */
  dispensedAt: string | null
  createdAt: string
  updatedAt: string | null
}

export interface EncounterFormValues {
  appointmentId: string
  symptoms: string | null
  diagnosis: string
  notes: string | null
  prescriptionItems: PrescriptionItem[]
}
