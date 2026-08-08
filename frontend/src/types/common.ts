// Kiểu dùng chung cho mọi feature, khớp envelope API backend.

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface ApiError {
  code: string
  message: string
  details?: Record<string, string[]> | null
}

export interface ApiResponse<T> {
  success: boolean
  data: T | null
  error: ApiError | null
  meta: unknown
}
