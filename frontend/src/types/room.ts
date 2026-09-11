// Kiểu dữ liệu miền Phòng khám, khớp với API backend (WS-01).

export interface Room {
  id: string
  code: string
  name: string
  description: string | null
  createdAt: string
  updatedAt: string | null
}

export interface RoomFormValues {
  name: string
  description: string | null
}
