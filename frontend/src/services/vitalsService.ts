import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type { Vitals, VitalsFormValues } from '../types/vitals'

/** Lần đo sinh hiệu gần nhất của lượt khám; null nếu chưa đo (backend trả data=null). */
export async function getVitals(appointmentId: string): Promise<Vitals | null> {
  const res = await apiClient.get<ApiResponse<Vitals | null>>(
    `/api/appointments/${appointmentId}/vitals`,
  )
  return res.data.data ?? null
}

/** Toàn bộ lịch sử đo sinh hiệu của lượt khám (mới nhất trước). */
export async function getVitalsHistory(appointmentId: string): Promise<Vitals[]> {
  const res = await apiClient.get<ApiResponse<Vitals[]>>(
    `/api/appointments/${appointmentId}/vitals/history`,
  )
  return unwrap(res.data)
}

/** Ghi một lần đo sinh hiệu mới cho lượt khám (luôn tạo bản ghi mới — không ghi đè, giữ lịch sử). */
export async function recordVitals(
  appointmentId: string,
  values: VitalsFormValues,
): Promise<Vitals> {
  const res = await apiClient.post<ApiResponse<Vitals>>(
    `/api/appointments/${appointmentId}/vitals`,
    values,
  )
  return unwrap(res.data)
}
