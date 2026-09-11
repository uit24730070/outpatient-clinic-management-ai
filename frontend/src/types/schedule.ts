// Kiểu dữ liệu Lịch làm việc bác sĩ (mẫu tuần), khớp API backend (WS-02).
// DayOfWeek khớp System.DayOfWeek của .NET: Chủ nhật = 0 … Thứ 7 = 6.

export const DayOfWeek = {
  Sunday: 0,
  Monday: 1,
  Tuesday: 2,
  Wednesday: 3,
  Thursday: 4,
  Friday: 5,
  Saturday: 6,
} as const

export type DayOfWeekValue = (typeof DayOfWeek)[keyof typeof DayOfWeek]

export const dayOfWeekLabels: Record<number, string> = {
  1: 'Thứ 2',
  2: 'Thứ 3',
  3: 'Thứ 4',
  4: 'Thứ 5',
  5: 'Thứ 6',
  6: 'Thứ 7',
  0: 'Chủ nhật',
}

// Thứ tự hiển thị: Thứ 2 → Chủ nhật.
export const dayOfWeekOrder: DayOfWeekValue[] = [1, 2, 3, 4, 5, 6, 0]

export interface DoctorWorkSchedule {
  id: string
  doctorId: string
  dayOfWeek: DayOfWeekValue
  startTime: string // "HH:mm:ss"
  endTime: string
  roomId: string | null
  roomName: string | null
  createdAt: string
  updatedAt: string | null
}

export interface DoctorScheduleFormValues {
  dayOfWeek: DayOfWeekValue
  startTime: string // "HH:mm:ss"
  endTime: string
  roomId: string | null
}

/** "HH:mm:ss" | "HH:mm" → "HH:mm" để hiển thị/điền vào input time. */
export function toTimeInput(value: string): string {
  return value.slice(0, 5)
}

/** "HH:mm" (input time) → "HH:mm:ss" để gửi backend (TimeOnly). */
export function toTimeSpanString(value: string): string {
  return value.length === 5 ? `${value}:00` : value
}
