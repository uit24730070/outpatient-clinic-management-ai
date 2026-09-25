import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { Appointment, AppointmentStatusValue } from '../types/appointment'

export interface ListAppointmentsParams {
  page: number
  pageSize: number
  date?: string
  doctorId?: string
  patientId?: string
  status?: AppointmentStatusValue
  sortBy?: string
  sortDesc?: boolean
}

export async function listAppointments(params: ListAppointmentsParams): Promise<PagedResult<Appointment>> {
  const res = await apiClient.get<ApiResponse<PagedResult<Appointment>>>('/api/appointments', { params })
  return unwrap(res.data)
}

export async function getAppointment(id: string): Promise<Appointment> {
  const res = await apiClient.get<ApiResponse<Appointment>>(`/api/appointments/${id}`)
  return unwrap(res.data)
}

export async function deleteAppointment(id: string): Promise<void> {
  await apiClient.delete(`/api/appointments/${id}`)
}

// Các hành động chuyển trạng thái (khớp máy trạng thái ADR 0005).
export type AppointmentAction = 'check-in' | 'start' | 'complete' | 'cancel' | 'no-show'

export async function transitionAppointment(id: string, action: AppointmentAction): Promise<Appointment> {
  const res = await apiClient.post<ApiResponse<Appointment>>(`/api/appointments/${id}/${action}`)
  return unwrap(res.data)
}
