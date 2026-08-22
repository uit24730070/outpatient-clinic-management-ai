import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type { AssistantMessage, AssistantReply } from '../types/assistant'

// Gửi toàn bộ lịch sử hội thoại; backend điều phối tool-calling và trả câu trả lời cuối.
export async function chatWithAssistant(messages: AssistantMessage[]): Promise<AssistantReply> {
  const res = await apiClient.post<ApiResponse<AssistantReply>>('/api/assistant/chat', { messages })
  return unwrap(res.data)
}
