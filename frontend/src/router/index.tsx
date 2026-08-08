import { createBrowserRouter, Navigate } from 'react-router-dom'
import MainLayout from '../layouts/MainLayout'
import PatientsListPage from '../pages/PatientsListPage'
import PatientFormPage from '../pages/PatientFormPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <MainLayout />,
    children: [
      { index: true, element: <Navigate to="/patients" replace /> },
      { path: 'patients', element: <PatientsListPage /> },
      { path: 'patients/new', element: <PatientFormPage /> },
      { path: 'patients/:id/edit', element: <PatientFormPage /> },
    ],
  },
])
