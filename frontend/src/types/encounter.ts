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

// Trạng thái cấp phát thuốc (ADR 0021, PAY-02). Giá trị số khớp DispenseStatus backend.
export const DispenseStatus = {
  None: 0,
  Reserved: 1,
  Paid: 2,
  Dispensed: 3,
  Returned: 4,
} as const

export type DispenseStatusValue = (typeof DispenseStatus)[keyof typeof DispenseStatus]

export const dispenseStatusLabels: Record<number, string> = {
  0: 'Không có thuốc',
  1: 'Giữ tồn (chờ thu tiền)',
  2: 'Đã thu tiền (chờ cấp phát)',
  3: 'Đã cấp phát',
  4: 'Đã hoàn kho',
}

export interface PrescriptionItem {
  /** Thuốc trong danh mục (null = thuốc ngoài danh mục, không trừ tồn). */
  medicationId: string | null
  drugName: string
  dosage: string
  quantity: number
  instruction: string | null
}

/**
 * Liều theo buổi trong ngày (số viên mỗi buổi) + số ngày dùng.
 * Dùng ở form kê đơn để nhập trực quan; khi lưu quy về `dosage` (chuỗi) +
 * `quantity` (tổng số lượng) theo hợp đồng backend hiện có.
 */
export interface DoseSchedule {
  morning: number
  noon: number
  afternoon: number
  evening: number
  days: number
}

/** Tổng số lượng cần kê = (sáng + trưa + chiều + tối) × số ngày. */
export function totalQuantity(s: DoseSchedule): number {
  return (s.morning + s.noon + s.afternoon + s.evening) * s.days
}

/** Soạn chuỗi liều người-đọc-được, cũng là định dạng để parse ngược khi mở lại. */
export function buildDosageText(s: DoseSchedule): string {
  return `Sáng ${s.morning} - Trưa ${s.noon} - Chiều ${s.afternoon} - Tối ${s.evening} × ${s.days} ngày`
}

/** Parse chuỗi liều do `buildDosageText` sinh ra; trả null nếu không khớp (đơn cũ nhập tay). */
export function parseDosageText(text: string | null | undefined): DoseSchedule | null {
  if (!text) return null
  const m = text.match(
    /Sáng\s+(\d+)\s*-\s*Trưa\s+(\d+)\s*-\s*Chiều\s+(\d+)\s*-\s*Tối\s+(\d+)\s*×\s*(\d+)\s*ngày/i,
  )
  if (!m) return null
  return {
    morning: Number(m[1]),
    noon: Number(m[2]),
    afternoon: Number(m[3]),
    evening: Number(m[4]),
    days: Number(m[5]),
  }
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
  /** Trạng thái cấp phát thuốc (ADR 0021, PAY-02). */
  dispenseStatus: DispenseStatusValue
  /** Thời điểm chốt phiếu giữ tồn (Reserved) — null nếu chưa/không cần. */
  reservedAt: string | null
  /** Thời điểm đã thu tiền hoá đơn thuốc (Paid) — null nếu chưa thu. */
  medicationPaidAt: string | null
  /** Thời điểm đã cấp phát thuốc (trừ tồn FEFO) — null nếu chưa cấp phát. */
  dispensedAt: string | null
  /** Thời điểm đã lập hoá đơn thuốc từ phiếu này — null nếu chưa lập. */
  medicationInvoicedAt: string | null
  /** Lý do hoàn kho — null nếu chưa hoàn. */
  returnReason: string | null
  /** Thời điểm hoàn kho — null nếu chưa hoàn. */
  returnedAt: string | null
  returnedByUserId: string | null
  returnedByUserName: string | null
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
