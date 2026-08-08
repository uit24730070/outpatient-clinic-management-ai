import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { Doctor, DoctorFormValues } from '../types/doctor'

export interface ListDoctorsParams {
  page: number
  pageSize: number
  search?: string
}

export async function listDoctors(params: ListDoctorsParams): Promise<PagedResult<Doctor>> {
  const res = await apiClient.get<ApiResponse<PagedResult<Doctor>>>('/api/doctors', { params })
  return unwrap(res.data)
}

export async function getDoctor(id: string): Promise<Doctor> {
  const res = await apiClient.get<ApiResponse<Doctor>>(`/api/doctors/${id}`)
  return unwrap(res.data)
}

export async function createDoctor(values: DoctorFormValues): Promise<Doctor> {
  const res = await apiClient.post<ApiResponse<Doctor>>('/api/doctors', values)
  return unwrap(res.data)
}

export async function updateDoctor(id: string, values: DoctorFormValues): Promise<Doctor> {
  const res = await apiClient.put<ApiResponse<Doctor>>(`/api/doctors/${id}`, values)
  return unwrap(res.data)
}

export async function deleteDoctor(id: string): Promise<void> {
  await apiClient.delete(`/api/doctors/${id}`)
}
