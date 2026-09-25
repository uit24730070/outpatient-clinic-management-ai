// Kiểu dữ liệu miền Viện phí (bảng giá dịch vụ, hoá đơn), khớp với API backend.

// ── Bảng giá dịch vụ (BILL-01) ────────────────────────────────────────

// Phân loại dịch vụ (số, khớp ServiceCategory backend). CLS-01.
export const ServiceCategory = {
  Consultation: 0,
  Paraclinical: 1,
  Other: 2,
} as const

export type ServiceCategoryValue = (typeof ServiceCategory)[keyof typeof ServiceCategory]

export const serviceCategoryLabels: Record<number, string> = {
  0: 'Công khám',
  1: 'Cận lâm sàng',
  2: 'Khác',
}

export interface ServicePrice {
  id: string
  code: string
  name: string
  unitPrice: number
  description: string | null
  category: ServiceCategoryValue
  createdAt: string
  updatedAt: string | null
}

export interface ServicePriceFormValues {
  name: string
  unitPrice: number
  description: string | null
  category: ServiceCategoryValue
}

// ── Hoá đơn (BILL-03/04) ──────────────────────────────────────────────

// Const-map thay cho enum (tsconfig bật erasableSyntaxOnly). Giá trị số khớp
// InvoiceStatus phía backend (serialize enum thành số, như AppointmentStatus).
export const InvoiceStatus = {
  Draft: 0,
  Paid: 1,
  Cancelled: 2,
  Refunded: 3,
} as const

export type InvoiceStatusValue = (typeof InvoiceStatus)[keyof typeof InvoiceStatus]

export const invoiceStatusLabels: Record<number, string> = {
  0: 'Nháp',
  1: 'Đã thu',
  2: 'Đã huỷ',
  3: 'Đã hoàn tiền',
}

// Loại dòng hoá đơn (số, khớp InvoiceItemType backend — Paraclinical chèn trước Other, ADR 0015).
export const InvoiceItemType = {
  ServiceFee: 0,
  Medication: 1,
  Paraclinical: 2,
  Other: 3,
} as const

export type InvoiceItemTypeValue = (typeof InvoiceItemType)[keyof typeof InvoiceItemType]

export const invoiceItemTypeLabels: Record<number, string> = {
  0: 'Công khám/Dịch vụ',
  1: 'Tiền thuốc',
  2: 'Cận lâm sàng',
  3: 'Khoản khác',
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
  appointmentId: string | null
  visitId: string | null
  status: InvoiceStatusValue
  totalAmount: number
  paidAt: string | null
  paymentMethod: PaymentMethodValue | null
  note: string | null
  items: InvoiceItem[]
  createdAt: string
  updatedAt: string | null
  /** Thời điểm hoàn tiền (null nếu chưa hoàn). */
  refundedAt: string | null
  /** Lý do hoàn tiền (null nếu chưa hoàn). */
  refundReason: string | null
}

/** Gom hoá đơn theo một lịch khám + tổng (khớp AppointmentInvoicesDto backend). */
export interface AppointmentInvoices {
  appointmentId: string
  invoices: Invoice[]
  totalBilled: number
  totalPaid: number
  totalOutstanding: number
}

/** Gom hoá đơn theo lượt tiếp nhận + tổng (khớp VisitInvoicesDto backend, ADR 0017). */
export interface VisitInvoices {
  visitId: string
  invoices: Invoice[]
  totalBilled: number
  totalPaid: number
  totalOutstanding: number
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
  /**
   * Các dịch vụ khám được thu trong hoá đơn này (tuỳ chọn, có thể nhiều — gộp cấp Lượt tiếp nhận);
   * để trống/undefined với vãng lai/chỉ-CLS. Mỗi dịch vụ khám chỉ lập được một lần.
   */
  appointmentIds?: string[]
  /** Gộp thêm phí CLS từ một phiếu chỉ định chưa lập hoá đơn (tuỳ chọn, ADR 0021 PAY-01). */
  labOrderId?: string | null
}

export interface UpdateInvoiceInput {
  note: string | null
  items: InvoiceItemInput[]
}
