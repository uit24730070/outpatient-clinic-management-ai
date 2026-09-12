import { apiClient, unwrap } from './apiClient'
import type { ApiResponse } from '../types/common'
import type {
  AppointmentReport,
  DoctorProductivity,
  Overview,
  ReportRangeParams,
  RevenueReport,
} from '../types/report'

/** R-01 · Chỉ số tổng quan (KPI) hôm nay. */
export async function getOverview(): Promise<Overview> {
  const res = await apiClient.get<ApiResponse<Overview>>('/api/reports/overview')
  return unwrap(res.data)
}

/** R-02 · Báo cáo lịch khám theo khoảng ngày (mặc định 7 ngày gần nhất). */
export async function getAppointmentReport(
  params: ReportRangeParams & { doctorId?: string } = {},
): Promise<AppointmentReport> {
  const res = await apiClient.get<ApiResponse<AppointmentReport>>('/api/reports/appointments', { params })
  return unwrap(res.data)
}

/** R-03 · Năng suất theo bác sĩ (bác sĩ đăng nhập chỉ thấy của mình). */
export async function getDoctorProductivity(
  params: ReportRangeParams = {},
): Promise<DoctorProductivity[]> {
  const res = await apiClient.get<ApiResponse<DoctorProductivity[]>>('/api/reports/by-doctor', { params })
  return unwrap(res.data)
}

/** R-04 · Báo cáo doanh thu theo khoảng ngày. */
export async function getRevenueReport(
  params: ReportRangeParams = {},
): Promise<RevenueReport> {
  const res = await apiClient.get<ApiResponse<RevenueReport>>('/api/reports/revenue', { params })
  return unwrap(res.data)
}
