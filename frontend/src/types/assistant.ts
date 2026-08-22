// Kiểu cho trợ lý hội thoại nghiệp vụ (AI-03), khớp DTO backend (camelCase).

// Một lượt hội thoại hiển thị/gửi đi. role: 'user' | 'assistant'.
export interface AssistantMessage {
  role: 'user' | 'assistant'
  content: string
}

// Một công cụ đã được trợ lý gọi (để truy vết), khớp AssistantToolCallDto.
export interface AssistantToolCall {
  name: string
  arguments: string
  result: string
}

// Kết quả trả về từ POST /api/assistant/chat, khớp AssistantReplyDto.
export interface AssistantReply {
  answer: string
  model: string
  toolCalls: AssistantToolCall[]
  generatedAt: string
}
