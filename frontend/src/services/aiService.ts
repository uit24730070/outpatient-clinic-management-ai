import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type { PatientSummary, PatientAnswer } from '../types/ai'

// Yêu cầu backend tóm tắt lịch sử khám của bệnh nhân bằng AI (Bác sĩ/Admin).
export async function summarizePatient(patientId: string): Promise<PatientSummary> {
  const res = await apiClient.post<ApiResponse<PatientSummary>>(`/api/patients/${patientId}/ai-summary`)
  return unwrap(res.data)
}

// Hỏi đáp có ngữ cảnh (RAG) trên bệnh án: truy hồi phiếu liên quan rồi trả lời (Bác sĩ/Admin).
export async function askPatient(patientId: string, question: string): Promise<PatientAnswer> {
  const res = await apiClient.post<ApiResponse<PatientAnswer>>(
    `/api/patients/${patientId}/ai-ask`,
    { question },
  )
  return unwrap(res.data)
}
