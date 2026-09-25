import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { ServiceCategoryValue, ServicePrice, ServicePriceFormValues } from '../types/invoice'

export interface ListServicePricesParams {
  page: number
  pageSize: number
  search?: string
  category?: ServiceCategoryValue
  sortBy?: string
  sortDesc?: boolean
}

export async function listServicePrices(
  params: ListServicePricesParams,
): Promise<PagedResult<ServicePrice>> {
  const res = await apiClient.get<ApiResponse<PagedResult<ServicePrice>>>('/api/service-prices', {
    params,
  })
  return unwrap(res.data)
}

export async function getServicePrice(id: string): Promise<ServicePrice> {
  const res = await apiClient.get<ApiResponse<ServicePrice>>(`/api/service-prices/${id}`)
  return unwrap(res.data)
}

export async function createServicePrice(values: ServicePriceFormValues): Promise<ServicePrice> {
  const res = await apiClient.post<ApiResponse<ServicePrice>>('/api/service-prices', values)
  return unwrap(res.data)
}

export async function updateServicePrice(
  id: string,
  values: ServicePriceFormValues,
): Promise<ServicePrice> {
  const res = await apiClient.put<ApiResponse<ServicePrice>>(`/api/service-prices/${id}`, values)
  return unwrap(res.data)
}

export async function deleteServicePrice(id: string): Promise<void> {
  await apiClient.delete(`/api/service-prices/${id}`)
}
