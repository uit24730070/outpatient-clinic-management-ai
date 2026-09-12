import { apiClient, unwrap } from './apiClient'
import type { ApiResponse, PagedResult } from '../types/common'
import type {
  AddVisitServiceInput,
  CreateVisitInput,
  Visit,
  VisitListItem,
  VisitStatusValue,
} from '../types/visit'

export interface ListVisitsParams {
  page: number
  pageSize: number
  patientId?: string
  status?: VisitStatusValue
  date?: string
}

export async function listVisits(params: ListVisitsParams): Promise<PagedResult<VisitListItem>> {
  const res = await apiClient.get<ApiResponse<PagedResult<VisitListItem>>>('/api/visits', { params })
  return unwrap(res.data)
}

export async function getVisit(id: string): Promise<Visit> {
  const res = await apiClient.get<ApiResponse<Visit>>(`/api/visits/${id}`)
  return unwrap(res.data)
}

export async function createVisit(input: CreateVisitInput): Promise<Visit> {
  const res = await apiClient.post<ApiResponse<Visit>>('/api/visits', input)
  return unwrap(res.data)
}

export async function addVisitService(id: string, input: AddVisitServiceInput): Promise<Visit> {
  const res = await apiClient.post<ApiResponse<Visit>>(`/api/visits/${id}/services`, input)
  return unwrap(res.data)
}

export async function closeVisit(id: string): Promise<Visit> {
  const res = await apiClient.post<ApiResponse<Visit>>(`/api/visits/${id}/close`)
  return unwrap(res.data)
}

export async function cancelVisit(id: string): Promise<Visit> {
  const res = await apiClient.post<ApiResponse<Visit>>(`/api/visits/${id}/cancel`)
  return unwrap(res.data)
}

export async function reopenVisit(id: string): Promise<Visit> {
  const res = await apiClient.post<ApiResponse<Visit>>(`/api/visits/${id}/reopen`)
  return unwrap(res.data)
}
