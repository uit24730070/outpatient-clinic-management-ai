// Kết quả tóm tắt lịch sử khám bằng AI, khớp PatientSummaryDto (camelCase).

export interface PatientSummary {
  patientId: string
  patientName: string
  summary: string
  encounterCount: number
  model: string
  generatedAt: string
}

// Phiếu khám được trích làm nguồn cho câu trả lời (traceability), khớp AnswerSourceDto.
export interface AnswerSource {
  encounterId: string
  createdAt: string
  diagnosis: string
  similarity: number
}

// Kết quả hỏi đáp có ngữ cảnh (RAG), khớp PatientAnswerDto.
export interface PatientAnswer {
  patientId: string
  patientName: string
  question: string
  answer: string
  model: string
  sources: AnswerSource[]
  generatedAt: string
}
