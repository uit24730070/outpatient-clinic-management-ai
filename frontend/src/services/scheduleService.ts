import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type { DoctorScheduleFormValues, DoctorWorkSchedule } from '../types/schedule'

export async function listDoctorSchedules(doctorId: string): Promise<DoctorWorkSchedule[]> {
  const res = await apiClient.get<ApiResponse<DoctorWorkSchedule[]>>(`/api/doctors/${doctorId}/schedules`)
  return unwrap(res.data)
}

export async function createDoctorSchedule(
  doctorId: string,
  values: DoctorScheduleFormValues,
): Promise<DoctorWorkSchedule> {
  const res = await apiClient.post<ApiResponse<DoctorWorkSchedule>>(
    `/api/doctors/${doctorId}/schedules`,
    values,
  )
  return unwrap(res.data)
}

export async function updateDoctorSchedule(
  doctorId: string,
  scheduleId: string,
  values: DoctorScheduleFormValues,
): Promise<DoctorWorkSchedule> {
  const res = await apiClient.put<ApiResponse<DoctorWorkSchedule>>(
    `/api/doctors/${doctorId}/schedules/${scheduleId}`,
    values,
  )
  return unwrap(res.data)
}

export async function deleteDoctorSchedule(doctorId: string, scheduleId: string): Promise<void> {
  await apiClient.delete(`/api/doctors/${doctorId}/schedules/${scheduleId}`)
}
