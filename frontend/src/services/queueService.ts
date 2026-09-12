import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type {
  CreateQueueTicketValues,
  QueueTicket,
  QueueTicketStatusValue,
} from '../types/queue'

export interface ListQueueParams {
  date?: string
  roomId?: string
  doctorId?: string
  status?: QueueTicketStatusValue
}

export async function listQueue(params: ListQueueParams = {}): Promise<QueueTicket[]> {
  const res = await apiClient.get<ApiResponse<QueueTicket[]>>('/api/queue', { params })
  return unwrap(res.data)
}

export async function createQueueTicket(
  values: CreateQueueTicketValues,
): Promise<QueueTicket> {
  const res = await apiClient.post<ApiResponse<QueueTicket>>('/api/queue', values)
  return unwrap(res.data)
}

export async function assignQueueTicket(
  id: string,
  values: { roomId: string | null; doctorId: string | null },
): Promise<QueueTicket> {
  const res = await apiClient.post<ApiResponse<QueueTicket>>(`/api/queue/${id}/assign`, values)
  return unwrap(res.data)
}

// Hành động chuyển trạng thái vé (khớp máy trạng thái ADR 0019).
export type QueueAction = 'call' | 'start' | 'done' | 'skip'

export async function transitionQueueTicket(
  id: string,
  action: QueueAction,
): Promise<QueueTicket> {
  const res = await apiClient.post<ApiResponse<QueueTicket>>(`/api/queue/${id}/${action}`)
  return unwrap(res.data)
}
