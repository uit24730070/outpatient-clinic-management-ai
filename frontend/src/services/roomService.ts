import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { Room, RoomFormValues } from '../types/room'

export interface ListRoomsParams {
  page: number
  pageSize: number
  search?: string
  sortBy?: string
  sortDesc?: boolean
}

export async function listRooms(params: ListRoomsParams): Promise<PagedResult<Room>> {
  const res = await apiClient.get<ApiResponse<PagedResult<Room>>>('/api/rooms', { params })
  return unwrap(res.data)
}

export async function getRoom(id: string): Promise<Room> {
  const res = await apiClient.get<ApiResponse<Room>>(`/api/rooms/${id}`)
  return unwrap(res.data)
}

export async function createRoom(values: RoomFormValues): Promise<Room> {
  const res = await apiClient.post<ApiResponse<Room>>('/api/rooms', values)
  return unwrap(res.data)
}

export async function updateRoom(id: string, values: RoomFormValues): Promise<Room> {
  const res = await apiClient.put<ApiResponse<Room>>(`/api/rooms/${id}`, values)
  return unwrap(res.data)
}

export async function deleteRoom(id: string): Promise<void> {
  await apiClient.delete(`/api/rooms/${id}`)
}
