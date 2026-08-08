import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type { PatientSummary } from '../types/ai'

// Yêu cầu backend tóm tắt lịch sử khám của bệnh nhân bằng AI (Bác sĩ/Admin).
export async function summarizePatient(patientId: string): Promise<PatientSummary> {
  const res = await apiClient.post<ApiResponse<PatientSummary>>(`/api/patients/${patientId}/ai-summary`)
  return unwrap(res.data)
}
