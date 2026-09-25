// Kiểu dữ liệu miền Cận lâm sàng (phiếu chỉ định + kết quả), khớp API backend (ADR 0015).

import type { ParaclinicalGroupValue } from './invoice'

// Const-map thay cho enum (tsconfig bật erasableSyntaxOnly). Giá trị số khớp
// LabOrderStatus backend (serialize enum thành số).
export const LabOrderStatus = {
  Ordered: 0,
  InProgress: 1,
  Completed: 2,
  Cancelled: 3,
} as const

export type LabOrderStatusValue = (typeof LabOrderStatus)[keyof typeof LabOrderStatus]

export const labOrderStatusLabels: Record<number, string> = {
  0: 'Đã chỉ định',
  1: 'Đang thực hiện',
  2: 'Hoàn tất',
  3: 'Đã huỷ',
}

export const LabOrderItemStatus = {
  Pending: 0,
  Completed: 1,
} as const

export type LabOrderItemStatusValue = (typeof LabOrderItemStatus)[keyof typeof LabOrderItemStatus]

/** Một thông số kết quả có cấu trúc (nhóm Xét nghiệm, ADR 0025). */
export interface LabResultParameter {
  name: string
  value: string
  unit: string | null
  referenceRange: string | null
  /** Tự tính khi Giá trị/Khoảng tham chiếu đều đọc được dạng số — không đọc được thì luôn false. */
  isAbnormal: boolean
}

export interface LabOrderItem {
  id: string
  servicePriceId: string
  serviceName: string
  unitPrice: number
  /** Nhóm CLS hiện tại của dịch vụ (ADR 0024) — tra theo ServicePriceId, không snapshot; dùng để chọn
   * giao diện nhập kết quả (bảng thông số cho Xét nghiệm, văn bản tự do cho nhóm còn lại). */
  group: ParaclinicalGroupValue | null
  resultText: string | null
  conclusion: string | null
  /** Kết quả có cấu trúc (ADR 0025) — rỗng nếu dùng resultText văn bản tự do. */
  parameters: LabResultParameter[]
  status: LabOrderItemStatusValue
  resultedAt: string | null
}

export interface LabOrder {
  id: string
  code: string
  /** null với phiếu walk-in (không qua phiếu khám) — ADR 0016. */
  encounterId: string | null
  /** Lịch khám gắn kèm (walk-in) hoặc null. */
  appointmentId: string | null
  /** Lượt tiếp nhận gắn kèm (walk-in, ADR 0017) hoặc null. */
  visitId: string | null
  patientId: string
  patientName: string | null
  /** null với phiếu walk-in (không có bác sĩ chỉ định). */
  doctorId: string | null
  doctorName: string | null
  status: LabOrderStatusValue
  note: string | null
  totalAmount: number
  invoicedAt: string | null
  /** Thời điểm đã thu phí CLS (Paid) — null nếu chưa thu. Chưa thu thì chặn nhập kết quả (ADR 0021, PAY-01). */
  paidAt: string | null
  items: LabOrderItem[]
  createdAt: string
  updatedAt: string | null
}

/** Một dòng chỉ định trong form (khớp CreateLabOrderItemRequest). */
export interface CreateLabOrderItemInput {
  servicePriceId: string
}

export interface CreateLabOrderInput {
  encounterId: string
  note: string | null
  items: CreateLabOrderItemInput[]
}

/** Đăng ký CLS walk-in (lễ tân) — khớp CreateWalkInLabOrderRequest (ADR 0016). */
export interface CreateWalkInLabOrderInput {
  patientId: string
  appointmentId: string | null
  note: string | null
  items: CreateLabOrderItemInput[]
  /** Lượt tiếp nhận để gom phiếu CLS & hoá đơn phí CLS theo lượt (ADR 0017); null nếu vãng lai. */
  visitId?: string | null
}

/** Một dòng thông số gửi lên khi nhập kết quả có cấu trúc (khớp ResultParameterInput). */
export interface ResultParameterInput {
  name: string
  value: string
  unit: string | null
  referenceRange: string | null
}

export interface SetLabResultInput {
  resultText: string | null
  conclusion: string | null
  parameters?: ResultParameterInput[] | null
}
