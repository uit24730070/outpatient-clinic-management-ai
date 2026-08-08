// Kết quả tóm tắt lịch sử khám bằng AI, khớp PatientSummaryDto (camelCase).

export interface PatientSummary {
  patientId: string
  patientName: string
  summary: string
  encounterCount: number
  model: string
  generatedAt: string
}
