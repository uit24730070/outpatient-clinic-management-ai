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

// Nhóm vai trò khớp RBAC backend (Roles.ManageStaff / Roles.RecordEncounter).
const MANAGE_STAFF = [UserRole.Admin, UserRole.Receptionist]
const RECORD_ENCOUNTER = [UserRole.Admin, UserRole.Doctor]
const ALL_ROLES = [UserRole.Admin, UserRole.Receptionist, UserRole.Doctor]

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />,
    children: [
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

          // Quản lý danh mục Bác sĩ/Chuyên khoa — chỉ Admin/Lễ tân.
          {
            element: <RequireRole roles={MANAGE_STAFF} />,
            children: [
              { path: 'doctors', element: <DoctorsListPage /> },
              { path: 'doctors/new', element: <DoctorFormPage /> },
              { path: 'doctors/:id/edit', element: <DoctorFormPage /> },
              { path: 'specialties', element: <SpecialtiesListPage /> },
              { path: 'specialties/new', element: <SpecialtyFormPage /> },
              { path: 'specialties/:id/edit', element: <SpecialtyFormPage /> },
            ],
          },
        ],
      },
    ],
  },
])
