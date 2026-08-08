import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { Appointment, AppointmentFormValues, AppointmentStatusValue } from '../types/appointment'

export interface ListAppointmentsParams {
  page: number
  pageSize: number
  date?: string
  doctorId?: string
  patientId?: string
  status?: AppointmentStatusValue
}

export async function listAppointments(params: ListAppointmentsParams): Promise<PagedResult<Appointment>> {
  const res = await apiClient.get<ApiResponse<PagedResult<Appointment>>>('/api/appointments', { params })
  return unwrap(res.data)
}

export async function getAppointment(id: string): Promise<Appointment> {
  const res = await apiClient.get<ApiResponse<Appointment>>(`/api/appointments/${id}`)
  return unwrap(res.data)
}

export async function createAppointment(values: AppointmentFormValues): Promise<Appointment> {
  const res = await apiClient.post<ApiResponse<Appointment>>('/api/appointments', values)
  return unwrap(res.data)
}

export async function updateAppointment(id: string, values: Pick<AppointmentFormValues, 'startTime' | 'endTime' | 'reason'>): Promise<Appointment> {
  const res = await apiClient.put<ApiResponse<Appointment>>(`/api/appointments/${id}`, values)
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
