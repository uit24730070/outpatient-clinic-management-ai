// Kiểu dữ liệu miền Viện phí (bảng giá dịch vụ, hoá đơn), khớp với API backend.

// ── Bảng giá dịch vụ (BILL-01) ────────────────────────────────────────

export interface ServicePrice {
  id: string
  code: string
  name: string
  unitPrice: number
  description: string | null
  createdAt: string
  updatedAt: string | null
}

export interface ServicePriceFormValues {
  name: string
  unitPrice: number
  description: string | null
}

// ── Hoá đơn (BILL-03/04) ──────────────────────────────────────────────

// Const-map thay cho enum (tsconfig bật erasableSyntaxOnly). Giá trị số khớp
// InvoiceStatus phía backend (serialize enum thành số, như AppointmentStatus).
export const InvoiceStatus = {
  Draft: 0,
  Paid: 1,
  Cancelled: 2,
} as const

export type InvoiceStatusValue = (typeof InvoiceStatus)[keyof typeof InvoiceStatus]

export const invoiceStatusLabels: Record<number, string> = {
  0: 'Nháp',
  1: 'Đã thu',
  2: 'Đã huỷ',
}

// Loại dòng hoá đơn (số, khớp InvoiceItemType backend).
export const InvoiceItemType = {
  ServiceFee: 0,
  Medication: 1,
  Other: 2,
} as const

export type InvoiceItemTypeValue = (typeof InvoiceItemType)[keyof typeof InvoiceItemType]

export const invoiceItemTypeLabels: Record<number, string> = {
  0: 'Công khám/Dịch vụ',
  1: 'Tiền thuốc',
  2: 'Khoản khác',
}

// Phương thức thanh toán (số, khớp PaymentMethod backend khi đọc).
export const PaymentMethod = {
  Cash: 0,
  Card: 1,
  Transfer: 2,
} as const

export type PaymentMethodValue = (typeof PaymentMethod)[keyof typeof PaymentMethod]

export const paymentMethodLabels: Record<number, string> = {
  0: 'Tiền mặt',
  1: 'Thẻ',
  2: 'Chuyển khoản',
}

// Chuỗi tên phương thức để GỬI lên API thu tiền (PayInvoiceRequest nhận chuỗi).
export const paymentMethodApiValue: Record<number, string> = {
  0: 'Cash',
  1: 'Card',
  2: 'Transfer',
}

export interface InvoiceItem {
  itemType: InvoiceItemTypeValue
  description: string
  unitPrice: number
  quantity: number
  lineTotal: number
  referenceId: string | null
}

export interface Invoice {
  id: string
  code: string
  patientId: string
  patientName: string | null
  encounterId: string | null
  status: InvoiceStatusValue
  totalAmount: number
  paidAt: string | null
  paymentMethod: PaymentMethodValue | null
  note: string | null
  items: InvoiceItem[]
  createdAt: string
  updatedAt: string | null
}

/** Một dòng dịch vụ trong form hoá đơn lẻ (khớp CreateInvoiceItemRequest). */
export interface InvoiceItemInput {
  servicePriceId: string
  quantity: number
}

export interface CreateInvoiceInput {
  patientId: string
  note: string | null
  items: InvoiceItemInput[]
}

export interface UpdateInvoiceInput {
  note: string | null
  items: InvoiceItemInput[]
}
