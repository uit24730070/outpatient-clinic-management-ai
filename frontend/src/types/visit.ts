// Kiểu dữ liệu miền Lượt tiếp nhận (Visit), khớp API backend (ADR 0017).

import type { Appointment } from './appointment'
import type { LabOrderStatusValue } from './labOrder'

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

/** Phiếu CLS gắn lượt (tóm tắt). */
export interface VisitLabOrder {
  id: string
  code: string
  status: LabOrderStatusValue
  totalAmount: number
  invoicedAt: string | null
  itemCount: number
}

/** Chi tiết một lượt tiếp nhận: các dịch vụ khám + CLS + tổng viện phí gom cả lượt. */
export interface Visit {
  id: string
  code: string
  patientId: string
  patientName: string | null
  status: VisitStatusValue
  note: string | null
  appointments: Appointment[]
  labOrders: VisitLabOrder[]
  totalBilled: number
  totalPaid: number
  totalOutstanding: number
  createdAt: string
  updatedAt: string | null
}

/** Dòng danh sách lượt tiếp nhận (nhẹ). */
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

/**
 * Một dịch vụ khám trong lượt (khi tạo/thêm). Không có khung giờ — walk-in không đặt trước giờ khám,
 * server tự lấy thời điểm tiếp nhận làm mốc; thứ tự khám do số thứ tự hàng đợi quyết định.
 */
export interface VisitServiceLineInput {
  doctorId: string
  reason: string | null
  servicePriceId: string | null
}

export interface CreateVisitInput {
  patientId: string
  note: string | null
  services: VisitServiceLineInput[]
  /** Dịch vụ CLS (loại Paraclinical) đăng ký ngay lúc tiếp nhận — tạo phiếu CLS walk-in gắn lượt (ADR 0017). */
  paraclinicalServiceIds?: string[]
}

export type AddVisitServiceInput = VisitServiceLineInput
