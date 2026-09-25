import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult, Patient, PatientFormValues } from '../types/patient'

export interface ListPatientsParams {
  page: number
  pageSize: number
  search?: string
  sortBy?: string
  sortDesc?: boolean
}

export async function listPatients(params: ListPatientsParams): Promise<PagedResult<Patient>> {
  const res = await apiClient.get<ApiResponse<PagedResult<Patient>>>('/api/patients', { params })
  return unwrap(res.data)
}

export async function getPatient(id: string): Promise<Patient> {
  const res = await apiClient.get<ApiResponse<Patient>>(`/api/patients/${id}`)
  return unwrap(res.data)
}

export async function createPatient(values: PatientFormValues): Promise<Patient> {
  const res = await apiClient.post<ApiResponse<Patient>>('/api/patients', values)
  return unwrap(res.data)
}

export async function updatePatient(id: string, values: PatientFormValues): Promise<Patient> {
  const res = await apiClient.put<ApiResponse<Patient>>(`/api/patients/${id}`, values)
  return unwrap(res.data)
}

export async function deletePatient(id: string): Promise<void> {
  await apiClient.delete(`/api/patients/${id}`)
}
