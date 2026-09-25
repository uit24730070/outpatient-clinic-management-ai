import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { StockReceipt, StockReceiptFormValues } from '../types/medication'

export interface ListStockReceiptsParams {
  page: number
  pageSize: number
  sortBy?: string
  sortDesc?: boolean
}

export async function listStockReceipts(params: ListStockReceiptsParams): Promise<PagedResult<StockReceipt>> {
  const res = await apiClient.get<ApiResponse<PagedResult<StockReceipt>>>('/api/stock-receipts', { params })
  return unwrap(res.data)
}

export async function getStockReceipt(id: string): Promise<StockReceipt> {
  const res = await apiClient.get<ApiResponse<StockReceipt>>(`/api/stock-receipts/${id}`)
  return unwrap(res.data)
}

export async function createStockReceipt(values: StockReceiptFormValues): Promise<StockReceipt> {
  const res = await apiClient.post<ApiResponse<StockReceipt>>('/api/stock-receipts', values)
  return unwrap(res.data)
}
