import { createBrowserRouter, Navigate } from 'react-router-dom'
import MainLayout from '../layouts/MainLayout'
import RequireAuth from './RequireAuth'
import LoginPage from '../pages/LoginPage'
import PatientsListPage from '../pages/PatientsListPage'
import PatientFormPage from '../pages/PatientFormPage'
import SpecialtiesListPage from '../pages/SpecialtiesListPage'
import SpecialtyFormPage from '../pages/SpecialtyFormPage'
import DoctorsListPage from '../pages/DoctorsListPage'
import DoctorFormPage from '../pages/DoctorFormPage'
import AppointmentsListPage from '../pages/AppointmentsListPage'
import AppointmentFormPage from '../pages/AppointmentFormPage'

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <RequireAuth />,
    children: [
      {
        path: '/',
        element: <MainLayout />,
        children: [
          { index: true, element: <Navigate to="/appointments" replace /> },
          { path: 'appointments', element: <AppointmentsListPage /> },
          { path: 'appointments/new', element: <AppointmentFormPage /> },
          { path: 'appointments/:id/edit', element: <AppointmentFormPage /> },
          { path: 'patients', element: <PatientsListPage /> },
          { path: 'patients/new', element: <PatientFormPage /> },
          { path: 'patients/:id/edit', element: <PatientFormPage /> },
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
])
