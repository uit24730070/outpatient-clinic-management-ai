// Kiểu dữ liệu báo cáo/thống kê vận hành, khớp API backend (Epic 7 — Reporting, ADR 0020).

/** Phân rã số lịch theo trạng thái (khớp AppointmentStatusBreakdown backend). */
export interface AppointmentStatusBreakdown {
  scheduled: number
  checkedIn: number
  inProgress: number
  completed: number
  cancelled: number
  noShow: number
}

/** R-01 · Chỉ số tổng quan (KPI) cho hôm nay. */
export interface Overview {
  totalActivePatients: number
  totalDoctors: number
  encountersToday: number
  revenueToday: number
  queueWaiting: number
  appointmentsToday: number
  appointmentsByStatus: AppointmentStatusBreakdown
}

export interface AppointmentDayReport {
  date: string
  total: number
  byStatus: AppointmentStatusBreakdown
}

/** R-02 · Báo cáo lịch khám theo khoảng ngày. */
export interface AppointmentReport {
  from: string
  to: string
  total: number
  days: AppointmentDayReport[]
}

/** R-03 · Năng suất một bác sĩ trong khoảng ngày. */
export interface DoctorProductivity {
  doctorId: string
  doctorCode: string
  doctorName: string
  totalAppointments: number
  completedAppointments: number
  encounters: number
}

export interface RevenueDayReport {
  date: string
  total: number
  serviceFee: number
  medication: number
  paraclinical: number
  other: number
}

/** R-04 · Báo cáo doanh thu theo khoảng ngày. */
export interface RevenueReport {
  from: string
  to: string
  grandTotal: number
  serviceFeeTotal: number
  medicationTotal: number
  paraclinicalTotal: number
  otherTotal: number
  days: RevenueDayReport[]
}

/** Tham số khoảng ngày cho các báo cáo theo khoảng (định dạng yyyy-MM-dd). */
export interface ReportRangeParams {
  from?: string
  to?: string
}
