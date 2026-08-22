import type { FieldValues, Path, UseFormReturn } from 'react-hook-form'
import { toast } from 'sonner'
import { toApiException } from '../services/apiClient'

/** PascalCase (property C#, vd `FullName`) → camelCase (field RHF, `fullName`). */
function toCamelCase(key: string): string {
  return key.charAt(0).toLowerCase() + key.slice(1)
}

/**
 * Ánh xạ lỗi field từ backend (`ApiException.details`, key PascalCase) sang RHF
 * (`form.setError`, field camelCase). Lỗi không có details (vd 409 trùng, 500) →
 * hiện toast. Trả về thông điệp lỗi gốc để caller dùng thêm nếu cần.
 *
 * Mẫu chuẩn ở mọi form (ADR 0012):
 *   catch (e) { applyServerErrors(form, e) }
 */
export function applyServerErrors<T extends FieldValues>(
  form: UseFormReturn<T>,
  err: unknown,
): string {
  const ex = toApiException(err)
  const details = ex.details

  if (details && Object.keys(details).length > 0) {
    for (const [key, messages] of Object.entries(details)) {
      form.setError(toCamelCase(key) as Path<T>, {
        type: 'server',
        message: messages.join(' '),
      })
    }
  } else {
    // Không map được field cụ thể (409/500/network) → thông báo chung.
    toast.error(ex.message)
  }

  return ex.message
}
