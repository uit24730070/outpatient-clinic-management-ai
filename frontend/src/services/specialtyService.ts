import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { Specialty, SpecialtyFormValues } from '../types/specialty'

export interface ListSpecialtiesParams {
  page: number
  pageSize: number
  search?: string
}

export async function listSpecialties(params: ListSpecialtiesParams): Promise<PagedResult<Specialty>> {
  const res = await apiClient.get<ApiResponse<PagedResult<Specialty>>>('/api/specialties', { params })
  return unwrap(res.data)
}

export async function getSpecialty(id: string): Promise<Specialty> {
  const res = await apiClient.get<ApiResponse<Specialty>>(`/api/specialties/${id}`)
  return unwrap(res.data)
}

export async function createSpecialty(values: SpecialtyFormValues): Promise<Specialty> {
  const res = await apiClient.post<ApiResponse<Specialty>>('/api/specialties', values)
  return unwrap(res.data)
}

export async function updateSpecialty(id: string, values: SpecialtyFormValues): Promise<Specialty> {
  const res = await apiClient.put<ApiResponse<Specialty>>(`/api/specialties/${id}`, values)
  return unwrap(res.data)
}

export async function deleteSpecialty(id: string): Promise<void> {
  await apiClient.delete(`/api/specialties/${id}`)
}
