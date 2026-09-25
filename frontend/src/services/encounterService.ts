import { apiClient, toApiException, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type {
  DispenseStatusValue,
  Encounter,
  EncounterFormValues,
  EncounterStatusValue,
} from '../types/encounter'

export interface ListEncountersParams {
  page: number
  pageSize: number
  patientId?: string
  doctorId?: string
  status?: EncounterStatusValue
  dispenseStatus?: DispenseStatusValue
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

// Cấp phát thực đơn thuốc đã thu tiền (Dược sĩ) — trừ tồn FEFO (ADR 0021, PAY-02).
export async function dispenseEncounter(id: string): Promise<Encounter> {
  const res = await apiClient.post<ApiResponse<Encounter>>(`/api/encounters/${id}/dispense`)
  return unwrap(res.data)
}

export interface ReturnStockItem {
  medicationId: string
  quantity: number
}

// Hoàn kho đơn đã cấp phát (Dược sĩ) — nhập lại tồn đúng lô + ghi sổ cái bù (ADR 0022, REF-02).
// Bắt buộc lý do; `items` chọn hoàn một phần theo từng thuốc/số lượng (rỗng = hoàn toàn bộ).
export async function returnStock(id: string, reason: string, items: ReturnStockItem[]): Promise<Encounter> {
  const res = await apiClient.post<ApiResponse<Encounter>>(`/api/encounters/${id}/return-stock`, {
    reason,
    items,
  })
  return unwrap(res.data)
}
