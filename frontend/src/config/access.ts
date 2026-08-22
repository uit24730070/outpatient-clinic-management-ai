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

/** Có quyền ghi nghiệp vụ kho thuốc (danh mục, nhập kho): Admin hoặc Lễ tân — khớp Roles.ManagePharmacy. */
export function canManagePharmacy(role: UserRoleValue | undefined): boolean {
  return role === UserRole.Admin || role === UserRole.Receptionist
}

/** Một mục điều hướng, gắn danh sách vai trò được phép thấy. */
export interface NavItem {
  label: string
  to: string
  roles: UserRoleValue[]
}

// Menu khai báo — MainLayout render theo cấu hình này, không liệt kê cứng.
// Bác sĩ có menu gọn theo phận sự (không thấy quản lý Bác sĩ/Chuyên khoa).
export const navItems: NavItem[] = [
  { label: 'Phòng khám của tôi', to: '/my-clinic', roles: [UserRole.Doctor] },
  { label: 'Lịch khám', to: '/appointments', roles: [UserRole.Admin, UserRole.Receptionist, UserRole.Doctor] },
  { label: 'Bệnh nhân', to: '/patients', roles: [UserRole.Admin, UserRole.Receptionist, UserRole.Doctor] },
  // Danh mục master Bác sĩ/Chuyên khoa — chỉ Admin (đọc vẫn dùng được ở form đặt lịch).
  { label: 'Bác sĩ', to: '/doctors', roles: [UserRole.Admin] },
  { label: 'Chuyên khoa', to: '/specialties', roles: [UserRole.Admin] },
  { label: 'Người dùng', to: '/users', roles: [UserRole.Admin] },
  // Kho thuốc — quản lý bởi Admin/Lễ tân (chưa có vai trò Dược sĩ riêng, ADR 0011).
  { label: 'Danh mục thuốc', to: '/medications', roles: [UserRole.Admin, UserRole.Receptionist] },
  { label: 'Nhập kho', to: '/stock-receipts', roles: [UserRole.Admin, UserRole.Receptionist] },
  { label: 'Cảnh báo kho', to: '/pharmacy/alerts', roles: [UserRole.Admin, UserRole.Receptionist] },
  { label: 'Trợ lý', to: '/assistant', roles: [UserRole.Admin, UserRole.Receptionist, UserRole.Doctor] },
]

// Trang mặc định (landing) theo luồng công việc mỗi vai trò.
// Lễ tân/Admin → lịch khám; Bác sĩ → phòng khám của tôi.
export const roleLandingPath: Record<UserRoleValue, string> = {
  [UserRole.Admin]: '/appointments',
  [UserRole.Receptionist]: '/appointments',
  [UserRole.Doctor]: '/my-clinic',
}

/** Trang mặc định cho vai trò hiện tại (fallback /appointments nếu thiếu). */
export function landingPathFor(role: UserRoleValue | undefined): string {
  return role ? roleLandingPath[role] ?? '/appointments' : '/appointments'
}

/** Các mục điều hướng mà vai trò được phép thấy. */
export function navItemsFor(role: UserRoleValue | undefined): NavItem[] {
  return role ? navItems.filter((item) => item.roles.includes(role)) : []
}
