import { apiClient, toApiException, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { Encounter, EncounterFormValues, EncounterStatusValue } from '../types/encounter'

export interface ListEncountersParams {
  page: number
  pageSize: number
  patientId?: string
  doctorId?: string
  status?: EncounterStatusValue
}

export async function listEncounters(params: ListEncountersParams): Promise<PagedResult<Encounter>> {
  const res = await apiClient.get<ApiResponse<PagedResult<Encounter>>>('/api/encounters', { params })
  return unwrap(res.data)
}

export async function getEncounter(id: string): Promise<Encounter> {
  const res = await apiClient.get<ApiResponse<Encounter>>(`/api/encounters/${id}`)
  return unwrap(res.data)
}

// Lấy phiếu khám theo lịch (1–1). Trả null nếu lịch chưa có phiếu (404).
export async function getEncounterByAppointment(appointmentId: string): Promise<Encounter | null> {
  try {
    const res = await apiClient.get<ApiResponse<Encounter>>(`/api/encounters/by-appointment/${appointmentId}`)
    return unwrap(res.data)
  } catch (err) {
    // Lịch chưa có phiếu (NotFound) → null; các lỗi khác ném lại.
    if (toApiException(err).code === 'Encounter.NotFound') return null
    throw err
  }
}

export async function createEncounter(values: EncounterFormValues): Promise<Encounter> {
  const res = await apiClient.post<ApiResponse<Encounter>>('/api/encounters', values)
  return unwrap(res.data)
}

export async function updateEncounter(
  id: string,
  values: Pick<EncounterFormValues, 'symptoms' | 'diagnosis' | 'notes' | 'prescriptionItems'>,
): Promise<Encounter> {
  const res = await apiClient.put<ApiResponse<Encounter>>(`/api/encounters/${id}`, values)
  return unwrap(res.data)
}

export async function completeEncounter(id: string): Promise<Encounter> {
  const res = await apiClient.post<ApiResponse<Encounter>>(`/api/encounters/${id}/complete`)
  return unwrap(res.data)
}
