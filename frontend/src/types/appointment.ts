// Kiểu dữ liệu miền Lịch khám, khớp với API backend.

// Const-map thay cho enum (tsconfig bật erasableSyntaxOnly). Giá trị số khớp
// AppointmentStatus phía backend (serialize enum thành số, như Gender).
export const AppointmentStatus = {
  Scheduled: 0,
  CheckedIn: 1,
  InProgress: 2,
  Completed: 3,
  Cancelled: 4,
  NoShow: 5,
} as const

export type AppointmentStatusValue = (typeof AppointmentStatus)[keyof typeof AppointmentStatus]

export const appointmentStatusLabels: Record<number, string> = {
  0: 'Đã đặt',
  1: 'Đã đến',
  2: 'Đang khám',
  3: 'Hoàn tất',
  4: 'Đã huỷ',
  5: 'Không đến',
}

// Lớp CSS badge theo trạng thái (dùng ở danh sách).
export const appointmentStatusClass: Record<number, string> = {
  0: 'badge--scheduled',
  1: 'badge--checkedin',
  2: 'badge--inprogress',
  3: 'badge--completed',
  4: 'badge--cancelled',
  5: 'badge--noshow',
}

export interface Appointment {
  id: string
  patientId: string
  patientName: string | null
  doctorId: string
  doctorName: string | null
  startTime: string
  endTime: string
  reason: string | null
  status: AppointmentStatusValue
  checkedInAt: string | null
  /** Dịch vụ khám lễ tân đăng ký khi đặt lịch (snapshot) — ADR 0016. */
  servicePriceId: string | null
  serviceName: string | null
  servicePrice: number | null
  /** Phòng khám gán cho lịch (ADR 0018); null nếu chưa gán. */
  roomId: string | null
  roomName: string | null
  createdAt: string
  updatedAt: string | null
  /** Thời điểm đã lập hoá đơn cho dịch vụ khám này — null nếu chưa lập. */
  invoicedAt: string | null
  /** Lượt tiếp nhận gom lịch này (ADR 0017); null với lịch lẻ. */
  visitId: string | null
  /** Số thứ tự hàng đợi cấp cho lịch này (ADR 0019); null nếu chưa có vé. */
  queueNumber: number | null
}
