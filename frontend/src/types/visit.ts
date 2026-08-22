// Kiểu dữ liệu miền Lượt tiếp đón (Visit), khớp API backend (ADR 0017).

import type { Appointment } from './appointment'

// Const-map thay enum (erasableSyntaxOnly). Giá trị số khớp VisitStatus backend (serialize số).
export const VisitStatus = {
  Open: 0,
  Closed: 1,
  Cancelled: 2,
} as const

export type VisitStatusValue = (typeof VisitStatus)[keyof typeof VisitStatus]

export const visitStatusLabels: Record<number, string> = {
  0: 'Đang mở',
  1: 'Đã đóng',
  2: 'Đã huỷ',
}

/** Chi tiết một lượt tiếp đón: các dịch vụ khám + tổng viện phí gom cả lượt. */
export interface Visit {
  id: string
  code: string
  patientId: string
  patientName: string | null
  status: VisitStatusValue
  note: string | null
  appointments: Appointment[]
  totalBilled: number
  totalPaid: number
  totalOutstanding: number
  createdAt: string
  updatedAt: string | null
}

/** Dòng danh sách lượt tiếp đón (nhẹ). */
export interface VisitListItem {
  id: string
  code: string
  patientId: string
  patientName: string | null
  status: VisitStatusValue
  note: string | null
  serviceCount: number
  createdAt: string
}

/** Một dịch vụ khám trong lượt (khi tạo/thêm). */
export interface VisitServiceLineInput {
  doctorId: string
  startTime: string
  endTime: string
  reason: string | null
  servicePriceId: string | null
}

export interface CreateVisitInput {
  patientId: string
  note: string | null
  services: VisitServiceLineInput[]
}

export type AddVisitServiceInput = VisitServiceLineInput
