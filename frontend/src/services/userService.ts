import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type { UserRoleValue } from '../types/auth'
import type { CreateUserValues, UpdateUserValues, UserListItem } from '../types/user'

export interface ListUsersParams {
  page: number
  pageSize: number
  search?: string
  role?: UserRoleValue
  isActive?: boolean
  sortBy?: string
  sortDesc?: boolean
}

export async function listUsers(params: ListUsersParams): Promise<PagedResult<UserListItem>> {
  const res = await apiClient.get<ApiResponse<PagedResult<UserListItem>>>('/api/users', { params })
  return unwrap(res.data)
}

export async function getUser(id: string): Promise<UserListItem> {
  const res = await apiClient.get<ApiResponse<UserListItem>>(`/api/users/${id}`)
  return unwrap(res.data)
}

export async function createUser(values: CreateUserValues): Promise<UserListItem> {
  const res = await apiClient.post<ApiResponse<UserListItem>>('/api/users', values)
  return unwrap(res.data)
}

export async function updateUser(id: string, values: UpdateUserValues): Promise<UserListItem> {
  const res = await apiClient.put<ApiResponse<UserListItem>>(`/api/users/${id}`, values)
  return unwrap(res.data)
}

export async function resetUserPassword(id: string, newPassword: string): Promise<void> {
  await apiClient.post(`/api/users/${id}/reset-password`, { newPassword })
}

export async function activateUser(id: string): Promise<void> {
  await apiClient.post(`/api/users/${id}/activate`)
}

export async function deactivateUser(id: string): Promise<void> {
  await apiClient.post(`/api/users/${id}/deactivate`)
}

export async function deleteUser(id: string): Promise<void> {
  await apiClient.delete(`/api/users/${id}`)
}

// Gắn/gỡ liên kết User↔Doctor (endpoint đặt trên hồ sơ bác sĩ).
export async function linkUserToDoctor(doctorId: string, userId: string): Promise<void> {
  await apiClient.post(`/api/doctors/${doctorId}/link-user`, { userId })
}

export async function unlinkUserFromDoctor(doctorId: string): Promise<void> {
  await apiClient.post(`/api/doctors/${doctorId}/unlink-user`)
}
