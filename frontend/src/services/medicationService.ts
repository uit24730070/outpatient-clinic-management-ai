import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { Medication, MedicationBatch, MedicationFormValues } from '../types/medication'

export interface ListMedicationsParams {
  page: number
  pageSize: number
  search?: string
}

export async function listMedications(params: ListMedicationsParams): Promise<PagedResult<Medication>> {
  const res = await apiClient.get<ApiResponse<PagedResult<Medication>>>('/api/medications', { params })
  return unwrap(res.data)
}

export async function getMedication(id: string): Promise<Medication> {
  const res = await apiClient.get<ApiResponse<Medication>>(`/api/medications/${id}`)
  return unwrap(res.data)
}

export async function getMedicationBatches(id: string): Promise<MedicationBatch[]> {
  const res = await apiClient.get<ApiResponse<MedicationBatch[]>>(`/api/medications/${id}/batches`)
  return unwrap(res.data)
}

export async function createMedication(values: MedicationFormValues): Promise<Medication> {
  const res = await apiClient.post<ApiResponse<Medication>>('/api/medications', values)
  return unwrap(res.data)
}

export async function updateMedication(id: string, values: MedicationFormValues): Promise<Medication> {
  const res = await apiClient.put<ApiResponse<Medication>>(`/api/medications/${id}`, values)
  return unwrap(res.data)
}

export async function deleteMedication(id: string): Promise<void> {
  await apiClient.delete(`/api/medications/${id}`)
}
