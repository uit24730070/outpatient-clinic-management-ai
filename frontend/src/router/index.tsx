import { createBrowserRouter } from 'react-router-dom'
import MainLayout from '../layouts/MainLayout'
import RequireAuth from './RequireAuth'
import RequireRole from './RequireRole'
import RoleLanding from './RoleLanding'
import { UserRole } from '../types/auth'
import LoginPage from '../pages/LoginPage'
import ForbiddenPage from '../pages/ForbiddenPage'
import MyClinicPage from '../pages/MyClinicPage'
import PatientsListPage from '../pages/PatientsListPage'
import PatientFormPage from '../pages/PatientFormPage'
import SpecialtiesListPage from '../pages/SpecialtiesListPage'
import SpecialtyFormPage from '../pages/SpecialtyFormPage'
import DoctorsListPage from '../pages/DoctorsListPage'
import DoctorFormPage from '../pages/DoctorFormPage'
import AppointmentsListPage from '../pages/AppointmentsListPage'
import AppointmentFormPage from '../pages/AppointmentFormPage'
import EncounterFormPage from '../pages/EncounterFormPage'
import PatientEncountersPage from '../pages/PatientEncountersPage'
import UsersListPage from '../pages/UsersListPage'
import UserFormPage from '../pages/UserFormPage'
import AssistantPage from '../pages/AssistantPage'
import MedicationsListPage from '../pages/MedicationsListPage'
import MedicationFormPage from '../pages/MedicationFormPage'
import MedicationBatchesPage from '../pages/MedicationBatchesPage'
import StockReceiptsListPage from '../pages/StockReceiptsListPage'
import StockReceiptFormPage from '../pages/StockReceiptFormPage'
import PharmacyAlertsPage from '../pages/PharmacyAlertsPage'
import ServicePricesListPage from '../pages/ServicePricesListPage'
import ServicePriceFormPage from '../pages/ServicePriceFormPage'
import InvoicesListPage from '../pages/InvoicesListPage'
import InvoiceFormPage from '../pages/InvoiceFormPage'
import InvoiceDetailPage from '../pages/InvoiceDetailPage'
import LabOrderPrintPage from '../pages/LabOrderPrintPage'
import LabWalkInPage from '../pages/LabWalkInPage'
import TechnicianLabPage from '../pages/TechnicianLabPage'

// Nhóm vai trò khớp RBAC backend (Roles.ManageStaff / Roles.RecordEncounter).
const MANAGE_STAFF = [UserRole.Admin, UserRole.Receptionist]
const RECORD_ENCOUNTER = [UserRole.Admin, UserRole.Doctor]
const RECORD_LAB_RESULT = [UserRole.Admin, UserRole.Doctor, UserRole.Technician]
const ALL_ROLES = [
  UserRole.Admin,
  UserRole.Receptionist,
  UserRole.Doctor,
  UserRole.Pharmacist,
  UserRole.Technician,
]
const ADMIN_ONLY = [UserRole.Admin]
const MANAGE_PHARMACY = [UserRole.Admin, UserRole.Pharmacist]
const MANAGE_BILLING = [UserRole.Admin, UserRole.Receptionist]

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />,
    children: [
      // Bản in phiếu kết quả CLS — layout tối giản, ngoài MainLayout (không sidebar khi in).
      { path: '/lab-orders/:id/print', element: <LabOrderPrintPage /> },
      {
        path: '/',
        element: <MainLayout />,
        children: [
          // "/" điều hướng theo vai trò; trang 403 dùng chung.
          { index: true, element: <RoleLanding /> },
          { path: 'forbidden', element: <ForbiddenPage /> },

          // Phòng khám của tôi — chỉ Bác sĩ.
          {
            element: <RequireRole roles={[UserRole.Doctor]} />,
            children: [{ path: 'my-clinic', element: <MyClinicPage /> }],
          },

          // Lịch khám: đọc cho mọi vai trò; ghi (đặt/sửa) chỉ Admin/Lễ tân.
          {
            element: <RequireRole roles={ALL_ROLES} />,
            children: [{ path: 'appointments', element: <AppointmentsListPage /> }],
          },
          {
            element: <RequireRole roles={MANAGE_STAFF} />,
            children: [
              { path: 'appointments/new', element: <AppointmentFormPage /> },
              { path: 'appointments/:id/edit', element: <AppointmentFormPage /> },
            ],
          },
          // Lập phiếu khám: Admin/Bác sĩ.
          {
            element: <RequireRole roles={RECORD_ENCOUNTER} />,
            children: [
              { path: 'appointments/:appointmentId/encounter', element: <EncounterFormPage /> },
            ],
          },

          // Bệnh nhân: đọc + lịch sử khám cho mọi vai trò; thêm/sửa chỉ Admin/Lễ tân.
          {
            element: <RequireRole roles={ALL_ROLES} />,
            children: [
              { path: 'patients', element: <PatientsListPage /> },
              { path: 'patients/:id/encounters', element: <PatientEncountersPage /> },
            ],
          },
          {
            element: <RequireRole roles={MANAGE_STAFF} />,
            children: [
              { path: 'patients/new', element: <PatientFormPage /> },
              { path: 'patients/:id/edit', element: <PatientFormPage /> },
            ],
          },

          // Quản lý danh mục master Bác sĩ/Chuyên khoa — chỉ Admin.
          {
            element: <RequireRole roles={ADMIN_ONLY} />,
            children: [
              { path: 'doctors', element: <DoctorsListPage /> },
              { path: 'doctors/new', element: <DoctorFormPage /> },
              { path: 'doctors/:id/edit', element: <DoctorFormPage /> },
              { path: 'specialties', element: <SpecialtiesListPage /> },
              { path: 'specialties/new', element: <SpecialtyFormPage /> },
              { path: 'specialties/:id/edit', element: <SpecialtyFormPage /> },
            ],
          },

          // Trợ lý hội thoại — mọi vai trò đã đăng nhập (phạm vi dữ liệu do backend kiểm soát).
          {
            element: <RequireRole roles={ALL_ROLES} />,
            children: [{ path: 'assistant', element: <AssistantPage /> }],
          },

          // Quản lý người dùng — chỉ Admin.
          {
            element: <RequireRole roles={ADMIN_ONLY} />,
            children: [
              { path: 'users', element: <UsersListPage /> },
              { path: 'users/new', element: <UserFormPage /> },
              { path: 'users/:id/edit', element: <UserFormPage /> },
            ],
          },

          // Kho thuốc — danh mục + nhập kho. Ghi cho Admin/Dược sĩ (ManagePharmacy, ADR 0013).
          {
            element: <RequireRole roles={MANAGE_PHARMACY} />,
            children: [
              { path: 'medications', element: <MedicationsListPage /> },
              { path: 'medications/new', element: <MedicationFormPage /> },
              { path: 'medications/:id/edit', element: <MedicationFormPage /> },
              { path: 'medications/:id/batches', element: <MedicationBatchesPage /> },
              { path: 'stock-receipts', element: <StockReceiptsListPage /> },
              { path: 'stock-receipts/new', element: <StockReceiptFormPage /> },
              { path: 'pharmacy/alerts', element: <PharmacyAlertsPage /> },
            ],
          },

          // Đăng ký CLS walk-in — Lễ tân/Admin (ManageStaff, ADR 0016).
          {
            element: <RequireRole roles={MANAGE_STAFF} />,
            children: [{ path: 'lab/walk-in', element: <LabWalkInPage /> }],
          },
          // Thực hiện CLS + nhập kết quả — Kỹ thuật viên/Bác sĩ/Admin (RecordLabResult, ADR 0016).
          {
            element: <RequireRole roles={RECORD_LAB_RESULT} />,
            children: [{ path: 'lab/technician', element: <TechnicianLabPage /> }],
          },

          // Viện phí — thu ngân bởi Lễ tân/Admin (ManageBilling, ADR 0014).
          {
            element: <RequireRole roles={MANAGE_BILLING} />,
            children: [
              { path: 'service-prices', element: <ServicePricesListPage /> },
              { path: 'service-prices/new', element: <ServicePriceFormPage /> },
              { path: 'service-prices/:id/edit', element: <ServicePriceFormPage /> },
              { path: 'invoices', element: <InvoicesListPage /> },
              { path: 'invoices/new', element: <InvoiceFormPage /> },
              { path: 'invoices/:id', element: <InvoiceDetailPage /> },
              { path: 'invoices/:id/edit', element: <InvoiceFormPage /> },
            ],
          },
        ],
      },
    ],
  },
])
