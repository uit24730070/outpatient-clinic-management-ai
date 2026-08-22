// Tiện ích định dạng dùng chung phía FE.

/** Định dạng số tiền VND theo locale vi-VN (dấu chấm phân nhóm), ví dụ 150000 → "150.000 ₫". */
export function formatVnd(amount: number): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(
    amount,
  )
}
