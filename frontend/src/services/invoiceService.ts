import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type {
  AppointmentInvoices,
  CreateInvoiceInput,
  Invoice,
  InvoiceStatusValue,
  UpdateInvoiceInput,
} from '../types/invoice'
import { paymentMethodApiValue, type PaymentMethodValue } from '../types/invoice'

export interface ListInvoicesParams {
  page: number
  pageSize: number
  patientId?: string
  appointmentId?: string
  status?: InvoiceStatusValue
  from?: string
  to?: string
}

export async function listInvoices(params: ListInvoicesParams): Promise<PagedResult<Invoice>> {
  const res = await apiClient.get<ApiResponse<PagedResult<Invoice>>>('/api/invoices', { params })
  return unwrap(res.data)
}

export async function getInvoice(id: string): Promise<Invoice> {
  const res = await apiClient.get<ApiResponse<Invoice>>(`/api/invoices/${id}`)
  return unwrap(res.data)
}

/** Gom các hoá đơn của một lượt tiếp đón + tổng đã lập/đã thu/còn nợ. */
export async function getInvoicesByAppointment(appointmentId: string): Promise<AppointmentInvoices> {
  const res = await apiClient.get<ApiResponse<AppointmentInvoices>>(
    `/api/invoices/by-appointment/${appointmentId}`,
  )
  return unwrap(res.data)
}

/** Lập hoá đơn từ một phiếu khám đã hoàn tất (tự điền công khám + thuốc đã cấp). */
export async function createInvoiceFromEncounter(encounterId: string): Promise<Invoice> {
  const res = await apiClient.post<ApiResponse<Invoice>>(
    `/api/invoices/from-encounter/${encounterId}`,
  )
  return unwrap(res.data)
}

/** Tạo hoá đơn dịch vụ lẻ (không gắn phiếu khám). */
export async function createInvoice(input: CreateInvoiceInput): Promise<Invoice> {
  const res = await apiClient.post<ApiResponse<Invoice>>('/api/invoices', input)
  return unwrap(res.data)
}

export async function updateInvoice(id: string, input: UpdateInvoiceInput): Promise<Invoice> {
  const res = await apiClient.put<ApiResponse<Invoice>>(`/api/invoices/${id}`, input)
  return unwrap(res.data)
}

/** Thu tiền hoá đơn. PaymentMethod gửi dạng CHUỖI ("Cash"/"Card"/"Transfer"). */
export async function payInvoice(id: string, method: PaymentMethodValue): Promise<Invoice> {
  const res = await apiClient.post<ApiResponse<Invoice>>(`/api/invoices/${id}/pay`, {
    paymentMethod: paymentMethodApiValue[method],
  })
  return unwrap(res.data)
}

export async function cancelInvoice(id: string): Promise<Invoice> {
  const res = await apiClient.post<ApiResponse<Invoice>>(`/api/invoices/${id}/cancel`)
  return unwrap(res.data)
}

export async function deleteInvoice(id: string): Promise<void> {
  await apiClient.delete(`/api/invoices/${id}`)
}
