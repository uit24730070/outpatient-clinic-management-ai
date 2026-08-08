import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type { AuthResult, AuthUser, LoginRequest } from '../types/auth'

const TOKEN_KEY = 'clinic.token'
const USER_KEY = 'clinic.user'

/** Đăng nhập; lưu token + thông tin người dùng vào localStorage. */
export async function login(request: LoginRequest): Promise<AuthResult> {
  const res = await apiClient.post<ApiResponse<AuthResult>>('/api/auth/login', request)
  const result = unwrap(res.data)
  setToken(result.accessToken)
  setStoredUser(result.user)
  return result
}

/** Xoá phiên đăng nhập khỏi localStorage. */
export function logout(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

export function getStoredUser(): AuthUser | null {
  const raw = localStorage.getItem(USER_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as AuthUser
  } catch {
    return null
  }
}

function setStoredUser(user: AuthUser): void {
  localStorage.setItem(USER_KEY, JSON.stringify(user))
}
