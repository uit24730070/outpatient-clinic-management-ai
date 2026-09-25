import { useEffect, useRef } from 'react'

const DEFAULT_INTERVAL_MS = 20_000

/**
 * Tự động gọi lại `callback` mỗi `intervalMs` khi tab đang hiển thị (Page Visibility API), và gọi
 * lại ngay khi tab được xem trở lại sau khi ẩn — tránh polling lãng phí khi người dùng chuyển tab.
 * Dùng cho các màn hàng đợi/workspace nhiều vai trò cùng thao tác (vd Lễ tân đăng ký → Bác sĩ cần
 * thấy ngay ở `/my-clinic`), không thay thế nút "Làm mới" thủ công đã có.
 */
export function useAutoRefresh(callback: () => void, intervalMs = DEFAULT_INTERVAL_MS): void {
  const callbackRef = useRef(callback)
  callbackRef.current = callback

  useEffect(() => {
    const tick = () => {
      if (document.visibilityState === 'visible') callbackRef.current()
    }
    const id = window.setInterval(tick, intervalMs)
    document.addEventListener('visibilitychange', tick)
    return () => {
      window.clearInterval(id)
      document.removeEventListener('visibilitychange', tick)
    }
  }, [intervalMs])
}
