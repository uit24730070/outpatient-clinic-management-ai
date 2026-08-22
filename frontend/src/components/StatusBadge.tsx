import type { ReactNode } from 'react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import {
  AppointmentStatus,
  appointmentStatusLabels,
  type AppointmentStatusValue,
} from '../types/appointment'
import {
  EncounterStatus,
  encounterStatusLabels,
  type EncounterStatusValue,
} from '../types/encounter'
import { UserRole, roleLabels, type UserRoleValue } from '../types/auth'
import {
  InvoiceStatus,
  invoiceStatusLabels,
  type InvoiceStatusValue,
} from '../types/invoice'
import {
  LabOrderStatus,
  labOrderStatusLabels,
  type LabOrderStatusValue,
} from '../types/labOrder'

// Bảng tông màu — giữ đúng ngữ nghĩa màu cũ (ADR 0012), dùng lại khắp app.
const tone = {
  indigo: 'bg-indigo-50 text-indigo-700 ring-indigo-600/20',
  cyan: 'bg-cyan-50 text-cyan-700 ring-cyan-600/20',
  amber: 'bg-amber-100 text-amber-800 ring-amber-600/20',
  green: 'bg-green-100 text-green-700 ring-green-600/20',
  gray: 'bg-gray-100 text-gray-600 ring-gray-500/20',
  red: 'bg-red-100 text-red-700 ring-red-600/20',
  blue: 'bg-blue-50 text-blue-700 ring-blue-600/20',
} as const

type Tone = keyof typeof tone

/** Badge nền màu nhạt + viền mờ (thay các class .badge--* cũ). */
export function TonedBadge({
  tone: t,
  className,
  children,
}: {
  tone: Tone
  className?: string
  children: ReactNode
}) {
  return (
    <Badge
      variant="outline"
      className={cn('border-0 ring-1 ring-inset font-medium', tone[t], className)}
    >
      {children}
    </Badge>
  )
}

const appointmentTone: Record<number, Tone> = {
  [AppointmentStatus.Scheduled]: 'indigo',
  [AppointmentStatus.CheckedIn]: 'cyan',
  [AppointmentStatus.InProgress]: 'amber',
  [AppointmentStatus.Completed]: 'green',
  [AppointmentStatus.Cancelled]: 'gray',
  [AppointmentStatus.NoShow]: 'red',
}

export function AppointmentStatusBadge({ status }: { status: AppointmentStatusValue }) {
  return (
    <TonedBadge tone={appointmentTone[status] ?? 'gray'}>
      {appointmentStatusLabels[status] ?? status}
    </TonedBadge>
  )
}

const encounterTone: Record<number, Tone> = {
  [EncounterStatus.Draft]: 'amber',
  [EncounterStatus.Completed]: 'green',
}

export function EncounterStatusBadge({ status }: { status: EncounterStatusValue }) {
  return (
    <TonedBadge tone={encounterTone[status] ?? 'gray'}>
      {encounterStatusLabels[status] ?? status}
    </TonedBadge>
  )
}

const roleTone: Record<string, Tone> = {
  [UserRole.Admin]: 'blue',
  [UserRole.Receptionist]: 'cyan',
  [UserRole.Doctor]: 'green',
  [UserRole.Pharmacist]: 'amber',
  [UserRole.Technician]: 'indigo',
}

export function RoleBadge({ role }: { role: UserRoleValue }) {
  return <TonedBadge tone={roleTone[role] ?? 'gray'}>{roleLabels[role] ?? role}</TonedBadge>
}

const invoiceTone: Record<number, Tone> = {
  [InvoiceStatus.Draft]: 'amber',
  [InvoiceStatus.Paid]: 'green',
  [InvoiceStatus.Cancelled]: 'gray',
}

export function InvoiceStatusBadge({ status }: { status: InvoiceStatusValue }) {
  return (
    <TonedBadge tone={invoiceTone[status] ?? 'gray'}>
      {invoiceStatusLabels[status] ?? status}
    </TonedBadge>
  )
}

const labOrderTone: Record<number, Tone> = {
  [LabOrderStatus.Ordered]: 'indigo',
  [LabOrderStatus.InProgress]: 'amber',
  [LabOrderStatus.Completed]: 'green',
  [LabOrderStatus.Cancelled]: 'gray',
}

export function LabOrderStatusBadge({ status }: { status: LabOrderStatusValue }) {
  return (
    <TonedBadge tone={labOrderTone[status] ?? 'gray'}>
      {labOrderStatusLabels[status] ?? status}
    </TonedBadge>
  )
}

/** Badge trạng thái hoạt động của tài khoản. */
export function ActiveBadge({ active }: { active: boolean }) {
  return (
    <TonedBadge tone={active ? 'green' : 'gray'}>{active ? 'Hoạt động' : 'Đã khoá'}</TonedBadge>
  )
}
