import axios from 'axios'
import type { ApiError, ApiResponse } from '../types/patient'
import { getToken, logout } from './authService'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5006'

export const apiClient = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
})

// Gắn Bearer token vào mọi request khi đã đăng nhập.
apiClient.interceptors.request.use((config) => {
  const token = getToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Token hết hạn / không hợp lệ (401): xoá phiên và chuyển về trang đăng nhập.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = axios.isAxiosError(error) ? error.response?.status : undefined
    const isLoginCall = error?.config?.url?.includes('/api/auth/login')
    if (status === 401 && !isLoginCall && window.location.pathname !== '/login') {
      logout()
      window.location.assign('/login')
    }
    return Promise.reject(error)
  },
)

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
    // Không có response từ server (mất mạng/timeout/CORS) — message của axios là tiếng Anh,
    // dịch lại để nhất quán với giao diện tiếng Việt thay vì lộ text gốc "Network Error".
    const message =
      err.code === 'ECONNABORTED'
        ? 'Yêu cầu quá thời gian chờ. Kiểm tra kết nối mạng và thử lại.'
        : 'Không thể kết nối máy chủ. Kiểm tra kết nối mạng và thử lại.'
    return new ApiException({ code: 'Network', message })
  }
  return new ApiException({ code: 'Unknown', message: 'Lỗi không xác định.' })
}
