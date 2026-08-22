import { toast } from 'sonner'
import { toApiException } from '../services/apiClient'

/** Thông báo thành công (lưu/xoá/khoá…). */
export function toastSuccess(message: string) {
  toast.success(message)
}

/** Thông báo thông tin/nhắc nhở (không phải lỗi hệ thống) — nhận chuỗi thẳng. */
export function toastInfo(message: string) {
  toast.info(message)
}

/** Thông báo lỗi từ exception API (bóc message envelope). */
export function toastError(err: unknown, fallback = 'Đã xảy ra lỗi.') {
  const message = err instanceof Error || typeof err === 'object' ? toApiException(err).message : fallback
  toast.error(message || fallback)
}

export { toast }
