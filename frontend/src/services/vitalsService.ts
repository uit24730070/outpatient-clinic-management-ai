import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type { Vitals, VitalsFormValues } from '../types/vitals'

/** Lấy sinh hiệu của lượt khám; null nếu chưa đo (backend trả data=null). */
export async function getVitals(appointmentId: string): Promise<Vitals | null> {
  const res = await apiClient.get<ApiResponse<Vitals | null>>(
    `/api/appointments/${appointmentId}/vitals`,
  )
  return res.data.data ?? null
}

/** Nhập/cập nhật (upsert) sinh hiệu cho lượt khám. */
export async function upsertVitals(
  appointmentId: string,
  values: VitalsFormValues,
): Promise<Vitals> {
  const res = await apiClient.post<ApiResponse<Vitals>>(
    `/api/appointments/${appointmentId}/vitals`,
    values,
  )
  return unwrap(res.data)
}
