import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type { PharmacyAlerts } from '../types/medication'

/** Lấy cảnh báo kho: tồn thấp + lô sắp/đã hết hạn (RBAC ManagePharmacy). */
export async function getPharmacyAlerts(expiringInDays = 30): Promise<PharmacyAlerts> {
  const res = await apiClient.get<ApiResponse<PharmacyAlerts>>('/api/pharmacy/alerts', {
    params: { expiringInDays },
  })
  return unwrap(res.data)
}
