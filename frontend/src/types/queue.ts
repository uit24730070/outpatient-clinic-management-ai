// Kiểu dữ liệu miền Hàng đợi khám, khớp với API backend (QN-03, ADR 0019).

// Const-map thay cho enum (tsconfig bật erasableSyntaxOnly). Giá trị số khớp
// QueueTicketStatus phía backend (serialize enum thành số).
export const QueueTicketStatus = {
  Waiting: 0,
  Called: 1,
  InProgress: 2,
  Done: 3,
  Skipped: 4,
} as const

export type QueueTicketStatusValue =
  (typeof QueueTicketStatus)[keyof typeof QueueTicketStatus]

export const queueStatusLabels: Record<number, string> = {
  0: 'Đang chờ',
  1: 'Đã gọi',
  2: 'Đang khám',
  3: 'Hoàn tất',
  4: 'Bỏ qua',
}

export interface QueueTicket {
  id: string
  ticketDate: string
  number: number
  patientId: string
  patientName: string | null
  appointmentId: string | null
  roomId: string | null
  roomName: string | null
  doctorId: string | null
  doctorName: string | null
  status: QueueTicketStatusValue
  calledAt: string | null
  createdAt: string
  updatedAt: string | null
  hasVitals: boolean
}

export interface CreateQueueTicketValues {
  patientId: string
  appointmentId?: string | null
  roomId?: string | null
  doctorId?: string | null
}
