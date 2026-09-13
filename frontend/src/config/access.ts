// Mô hình phân quyền & điều hướng tập trung theo vai trò (ADR 0009).
// Nguồn sự thật duy nhất cho: menu hiển thị, trang mặc định mỗi vai trò,
// và các cờ quyền — thay cho việc rải `role === ...` khắp nơi.
//
// LƯU Ý: đây chỉ là lớp UX. Chốt chặn bảo mật vẫn ở backend (RBAC + 403).
// Các cờ dưới phải KHỚP RBAC backend (Roles.ManageStaff, Roles.RecordEncounter).

import { UserRole, type UserRoleValue } from '../types/auth'

/** Có quyền quản lý danh mục/đặt lịch (ghi): Admin hoặc Lễ tân — khớp Roles.ManageStaff. */
export function canManageStaff(role: UserRoleValue | undefined): boolean {
  return role === UserRole.Admin || role === UserRole.Receptionist
}

/** Có quyền ghi bệnh án (phiếu khám/đơn thuốc): Admin hoặc Bác sĩ — khớp Roles.RecordEncounter. */
export function canRecordEncounter(role: UserRoleValue | undefined): boolean {
  return role === UserRole.Admin || role === UserRole.Doctor
}

/** Có quyền quản lý danh mục master Bác sĩ/Chuyên khoa (ghi): chỉ Admin — khớp Roles.ManageCatalog. */
export function canManageCatalog(role: UserRoleValue | undefined): boolean {
  return role === UserRole.Admin
}

/** Có quyền ghi nghiệp vụ kho thuốc (danh mục, nhập kho, sổ cái, cảnh báo): Admin hoặc Dược sĩ — khớp Roles.ManagePharmacy (ADR 0013). */
export function canManagePharmacy(role: UserRoleValue | undefined): boolean {
  return role === UserRole.Admin || role === UserRole.Pharmacist
}

/** Có quyền ghi/đọc nghiệp vụ viện phí (bảng giá, hoá đơn, thu tiền): Admin hoặc Lễ tân — khớp Roles.ManageBilling (ADR 0014). */
export function canManageBilling(role: UserRoleValue | undefined): boolean {
  return role === UserRole.Admin || role === UserRole.Receptionist
}

/** Có quyền nhập kết quả cận lâm sàng: Admin, Bác sĩ hoặc Kỹ thuật viên — khớp Roles.RecordLabResult (ADR 0016). */
export function canRecordLabResult(role: UserRoleValue | undefined): boolean {
  return role === UserRole.Admin || role === UserRole.Doctor || role === UserRole.Technician
}

/** Có quyền nhập/cập nhật sinh hiệu: Admin hoặc Điều dưỡng — khớp Roles.RecordVitals (ADR 0019). */
export function canRecordVitals(role: UserRoleValue | undefined): boolean {
  return role === UserRole.Admin || role === UserRole.Nurse
}

/** Có quyền điều phối hàng đợi khám: Admin, Lễ tân hoặc Điều dưỡng — khớp Roles.ManageQueue (ADR 0019). */
export function canManageQueue(role: UserRoleValue | undefined): boolean {
  return role === UserRole.Admin || role === UserRole.Receptionist || role === UserRole.Nurse
}

/** Một mục điều hướng, gắn danh sách vai trò được phép thấy + nhóm hiển thị trong sidebar. */
export interface NavItem {
  label: string
  to: string
  roles: UserRoleValue[]
  group: NavGroupValue
}

// Nhóm menu — thứ tự trong mảng này là thứ tự hiển thị trong sidebar (MainLayout gom theo nhóm).
// Mục tiêu: Admin (thấy gần hết menu) không phải lướt một danh sách phẳng ~20 mục.
export const NavGroup = {
  Overview: 'Tổng quan',
  Workspace: 'Workspace theo vai trò',
  Clinical: 'Khám bệnh',
  Paraclinical: 'Cận lâm sàng',
  Billing: 'Viện phí',
  Pharmacy: 'Kho thuốc',
  Catalog: 'Danh mục hệ thống',
  Other: 'Khác',
} as const
export type NavGroupValue = (typeof NavGroup)[keyof typeof NavGroup]

// Menu khai báo — MainLayout render theo cấu hình này, không liệt kê cứng.
// Bác sĩ có menu gọn theo phận sự (không thấy quản lý Bác sĩ/Chuyên khoa).
export const navItems: NavItem[] = [
  // Tổng quan / Dashboard (Epic 7, ADR 0020) — báo cáo vận hành cho quản lý.
  { label: 'Tổng quan', to: '/dashboard', roles: [UserRole.Admin, UserRole.Receptionist], group: NavGroup.Overview },

  // Workspace theo vai trò (Epic 17) — 1 màn gộp việc lặp lại liên tiếp của từng vai trò.
  { label: 'Bác sĩ — Một màn', to: '/my-clinic', roles: [UserRole.Doctor], group: NavGroup.Workspace },
  { label: 'Lễ tân — Một màn', to: '/front-desk', roles: [UserRole.Admin, UserRole.Receptionist], group: NavGroup.Workspace },
  { label: 'Điều dưỡng — Một màn', to: '/nurse', roles: [UserRole.Admin, UserRole.Nurse], group: NavGroup.Workspace },
  { label: 'Dược sĩ — Một màn', to: '/pharmacy/workspace', roles: [UserRole.Admin, UserRole.Pharmacist], group: NavGroup.Workspace },

  // Khám bệnh — tiếp đón, lịch, hàng đợi, sinh hiệu, hồ sơ bệnh nhân.
  // Lượt tiếp đón (danh sách + tạo mới) đã gộp vào "Lễ tân — Một màn" (/front-desk) ở trên.
  { label: 'Lịch khám', to: '/appointments', roles: [UserRole.Admin, UserRole.Receptionist, UserRole.Doctor], group: NavGroup.Clinical },
  { label: 'Hàng đợi', to: '/queue', roles: [UserRole.Admin, UserRole.Receptionist, UserRole.Nurse], group: NavGroup.Clinical },
  { label: 'Sinh hiệu', to: '/vitals', roles: [UserRole.Admin, UserRole.Nurse], group: NavGroup.Clinical },
  { label: 'Bệnh nhân', to: '/patients', roles: [UserRole.Admin, UserRole.Receptionist, UserRole.Doctor], group: NavGroup.Clinical },

  // Cận lâm sàng — đăng ký gắn vào lượt tiếp đón (Tiếp đón/VisitDetailPage) + thực hiện/nhập kết
  // quả (kỹ thuật viên), ADR 0016.
  { label: 'Thực hiện CLS', to: '/lab/technician', roles: [UserRole.Admin, UserRole.Technician], group: NavGroup.Paraclinical },

  // Viện phí — thu ngân bởi Lễ tân (và Admin), ADR 0014.
  { label: 'Hoá đơn', to: '/invoices', roles: [UserRole.Admin, UserRole.Receptionist], group: NavGroup.Billing },
  { label: 'Bảng giá dịch vụ', to: '/service-prices', roles: [UserRole.Admin, UserRole.Receptionist], group: NavGroup.Billing },

  // Kho thuốc — quản lý trực tiếp bởi Dược sĩ (và Admin), ADR 0013.
  { label: 'Danh mục thuốc', to: '/medications', roles: [UserRole.Admin, UserRole.Pharmacist], group: NavGroup.Pharmacy },
  { label: 'Nhập kho', to: '/stock-receipts', roles: [UserRole.Admin, UserRole.Pharmacist], group: NavGroup.Pharmacy },
  // Cấp phát thuốc sau thu tiền — Dược sĩ/Admin (ADR 0021, PAY-02).
  { label: 'Cấp phát thuốc', to: '/pharmacy/dispense', roles: [UserRole.Admin, UserRole.Pharmacist], group: NavGroup.Pharmacy },
  { label: 'Cảnh báo kho', to: '/pharmacy/alerts', roles: [UserRole.Admin, UserRole.Pharmacist], group: NavGroup.Pharmacy },

  // Danh mục master Bác sĩ/Chuyên khoa/Phòng khám/Người dùng — chỉ Admin (trừ Phòng khám: Lễ tân cũng thấy).
  { label: 'Bác sĩ', to: '/doctors', roles: [UserRole.Admin], group: NavGroup.Catalog },
  { label: 'Chuyên khoa', to: '/specialties', roles: [UserRole.Admin], group: NavGroup.Catalog },
  { label: 'Phòng khám', to: '/rooms', roles: [UserRole.Admin, UserRole.Receptionist], group: NavGroup.Catalog },
  { label: 'Người dùng', to: '/users', roles: [UserRole.Admin], group: NavGroup.Catalog },

  { label: 'Trợ lý', to: '/assistant', roles: [UserRole.Admin, UserRole.Receptionist, UserRole.Doctor, UserRole.Pharmacist, UserRole.Technician, UserRole.Nurse], group: NavGroup.Other },
]

// Trang mặc định (landing) theo luồng công việc mỗi vai trò.
// Lễ tân/Admin → lịch khám; Bác sĩ → phòng khám của tôi.
export const roleLandingPath: Record<UserRoleValue, string> = {
  [UserRole.Admin]: '/dashboard',
  [UserRole.Receptionist]: '/dashboard',
  [UserRole.Doctor]: '/my-clinic',
  [UserRole.Pharmacist]: '/pharmacy/workspace',
  [UserRole.Technician]: '/lab/technician',
  [UserRole.Nurse]: '/nurse',
}

/** Trang mặc định cho vai trò hiện tại (fallback /appointments nếu thiếu). */
export function landingPathFor(role: UserRoleValue | undefined): string {
  return role ? roleLandingPath[role] ?? '/appointments' : '/appointments'
}

/** Các mục điều hướng mà vai trò được phép thấy (danh sách phẳng, không phân nhóm). */
export function navItemsFor(role: UserRoleValue | undefined): NavItem[] {
  return role ? navItems.filter((item) => item.roles.includes(role)) : []
}

/** Mục điều hướng mà vai trò được phép thấy, gom theo nhóm (thứ tự nhóm khớp `NavGroup`) — dùng cho sidebar. */
export function navGroupsFor(role: UserRoleValue | undefined): { group: NavGroupValue; items: NavItem[] }[] {
  const items = navItemsFor(role)
  return Object.values(NavGroup)
    .map((group) => ({ group, items: items.filter((item) => item.group === group) }))
    .filter((g) => g.items.length > 0)
}
