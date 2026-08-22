import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type {
  CreateLabOrderInput,
  LabOrder,
  LabOrderStatusValue,
  SetLabResultInput,
} from '../types/labOrder'

export interface ListLabOrdersParams {
  page: number
  pageSize: number
  encounterId?: string
  patientId?: string
  status?: LabOrderStatusValue
}

export async function listLabOrders(params: ListLabOrdersParams): Promise<PagedResult<LabOrder>> {
  const res = await apiClient.get<ApiResponse<PagedResult<LabOrder>>>('/api/lab-orders', { params })
  return unwrap(res.data)
}

export async function getLabOrder(id: string): Promise<LabOrder> {
  const res = await apiClient.get<ApiResponse<LabOrder>>(`/api/lab-orders/${id}`)
  return unwrap(res.data)
}

/** Chỉ định cận lâm sàng từ một phiếu khám. */
export async function createLabOrder(input: CreateLabOrderInput): Promise<LabOrder> {
  const res = await apiClient.post<ApiResponse<LabOrder>>('/api/lab-orders', input)
  return unwrap(res.data)
}

/** Nhập kết quả cho một mục chỉ định. */
export async function setLabResult(
  id: string,
  itemId: string,
  input: SetLabResultInput,
): Promise<LabOrder> {
  const res = await apiClient.post<ApiResponse<LabOrder>>(
    `/api/lab-orders/${id}/items/${itemId}/result`,
    input,
  )
  return unwrap(res.data)
}

/** Huỷ phiếu chỉ định. */
export async function cancelLabOrder(id: string): Promise<LabOrder> {
  const res = await apiClient.post<ApiResponse<LabOrder>>(`/api/lab-orders/${id}/cancel`)
  return unwrap(res.data)
}
