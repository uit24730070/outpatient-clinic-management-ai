import axios from 'axios'
import type { ApiError, ApiResponse } from '../types/patient'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5006'

export const apiClient = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
})

/** Lỗi có cấu trúc trích từ envelope ApiResponse của backend. */
export class ApiException extends Error {
  readonly code: string
  readonly details?: Record<string, string[]> | null

  constructor(error: ApiError) {
    super(error.message)
    this.name = 'ApiException'
    this.code = error.code
    this.details = error.details
  }
}

/** Bóc envelope thành công; ném ApiException nếu thất bại. */
export function unwrap<T>(body: ApiResponse<T>): T {
  if (!body.success || body.data === null) {
    throw new ApiException(body.error ?? { code: 'Unknown', message: 'Lỗi không xác định.' })
  }
  return body.data
}

/** Chuẩn hoá lỗi từ axios thành ApiException khi có thể. */
export function toApiException(err: unknown): ApiException {
  if (err instanceof ApiException) return err
  if (axios.isAxiosError(err)) {
    const body = err.response?.data as ApiResponse<unknown> | undefined
    if (body?.error) return new ApiException(body.error)
    return new ApiException({ code: 'Network', message: err.message })
  }
  return new ApiException({ code: 'Unknown', message: 'Lỗi không xác định.' })
}
