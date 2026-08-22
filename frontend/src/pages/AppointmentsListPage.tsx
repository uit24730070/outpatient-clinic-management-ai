import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Stethoscope, X } from 'lucide-react'
import {
  deleteAppointment,
  listAppointments,
  transitionAppointment,
  type AppointmentAction,
} from '../services/appointmentService'
import { listDoctors } from '../services/doctorService'
import { useAuth } from '../store/auth'
import { toastError, toastSuccess } from '../lib/toast'
import {
  AppointmentStatus,
  appointmentStatusLabels,
  type Appointment,
} from '../types/appointment'
import type { Doctor } from '../types/doctor'
import type { PagedResult } from '../types/common'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { AppointmentStatusBadge } from '../components/StatusBadge'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const PAGE_SIZE = 10
const ALL = 'all'

// Các hành động chuyển trạng thái khả dụng theo trạng thái hiện tại.
const actionsByStatus: Record<number, { action: AppointmentAction; label: string; danger?: boolean }[]> = {
  [AppointmentStatus.Scheduled]: [
    { action: 'check-in', label: 'Check-in' },
    { action: 'cancel', label: 'Huỷ', danger: true },
    { action: 'no-show', label: 'Không đến', danger: true },
  ],
  [AppointmentStatus.CheckedIn]: [
    { action: 'start', label: 'Bắt đầu khám' },
    { action: 'cancel', label: 'Huỷ', danger: true },
    { action: 'no-show', label: 'Không đến', danger: true },
  ],
  [AppointmentStatus.InProgress]: [
    { action: 'complete', label: 'Hoàn tất' },
    { action: 'cancel', label: 'Huỷ', danger: true },
  ],
  [AppointmentStatus.Completed]: [],
  [AppointmentStatus.Cancelled]: [],
  [AppointmentStatus.NoShow]: [],
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatEndTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

export default function AppointmentsListPage() {
  const { canManage, canRecordEncounter } = useAuth()
  const [date, setDate] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const [doctors, setDoctors] = useState<Doctor[]>([])
  const [data, setData] = useState<PagedResult<Appointment> | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    void (async () => {
      try {
        const result = await listDoctors({ page: 1, pageSize: 100 })
        setDoctors(result.items)
      } catch {
        // Danh sách bác sĩ chỉ phục vụ bộ lọc; lỗi ở đây không chặn danh sách lịch.
      }
    })()
  }, [])

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listAppointments({
        page,
        pageSize: PAGE_SIZE,
        date: date || undefined,
        doctorId: doctorId || undefined,
        status: status === '' ? undefined : (Number(status) as Appointment['status']),
      })
      setData(result)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [page, date, doctorId, status])

  useEffect(() => {
    void load()
  }, [load])

  const onAction = async (a: Appointment, action: AppointmentAction) => {
    try {
      await transitionAppointment(a.id, action)
      toastSuccess('Đã cập nhật trạng thái lịch khám.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const onDelete = async (a: Appointment) => {
    try {
      await deleteAppointment(a.id)
      toastSuccess('Đã xoá lịch khám.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  const canEdit = (a: Appointment) =>
    a.status === AppointmentStatus.Scheduled || a.status === AppointmentStatus.CheckedIn

  const hasFilter = Boolean(date || doctorId || status)

  return (
    <section>
      <PageHeader
        title="Lịch khám"
        description="Quản lý lịch hẹn & tiếp đón bệnh nhân"
        actions={
          canManage && (
            <Button asChild>
              <Link to="/appointments/new">
                <Plus className="size-4" />
                Đặt lịch
              </Link>
            </Button>
          )
        }
      />

      <Card className="mb-4">
        <CardContent className="flex flex-wrap items-center gap-3">
          <Input
            type="date"
            className="w-auto"
            value={date}
            onChange={(e) => {
              setPage(1)
              setDate(e.target.value)
            }}
          />
          <Select
            value={doctorId || ALL}
            onValueChange={(v) => {
              setPage(1)
              setDoctorId(v === ALL ? '' : v)
            }}
          >
            <SelectTrigger className="w-[200px]">
              <SelectValue placeholder="Bác sĩ" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>Tất cả bác sĩ</SelectItem>
              {doctors.map((d) => (
                <SelectItem key={d.id} value={d.id}>
                  {d.fullName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Select
            value={status === '' ? ALL : status}
            onValueChange={(v) => {
              setPage(1)
              setStatus(v === ALL ? '' : v)
            }}
          >
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Trạng thái" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>Tất cả trạng thái</SelectItem>
              {Object.values(AppointmentStatus).map((v) => (
                <SelectItem key={v} value={String(v)}>
                  {appointmentStatusLabels[v]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {hasFilter && (
            <Button
              variant="ghost"
              onClick={() => {
                setPage(1)
                setDate('')
                setDoctorId('')
                setStatus('')
              }}
            >
              <X className="size-4" />
              Xoá lọc
            </Button>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Thời gian</TableHead>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead>Bác sĩ</TableHead>
                <TableHead>Lý do</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Không có lịch khám nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((a) => (
                  <TableRow key={a.id}>
                    <TableCell className="whitespace-nowrap font-medium">
                      {formatTime(a.startTime)} – {formatEndTime(a.endTime)}
                    </TableCell>
                    <TableCell>{a.patientName ?? '—'}</TableCell>
                    <TableCell>{a.doctorName ?? '—'}</TableCell>
                    <TableCell className="max-w-[200px] truncate text-muted-foreground">
                      {a.reason ?? '—'}
                    </TableCell>
                    <TableCell>
                      <AppointmentStatusBadge status={a.status} />
                    </TableCell>
                    <TableCell>
                      <div className="flex flex-wrap items-center justify-end gap-1">
                        {canRecordEncounter && a.status === AppointmentStatus.InProgress && (
                          <Button asChild size="sm" variant="secondary">
                            <Link to={`/appointments/${a.id}/encounter`}>
                              <Stethoscope className="size-4" />
                              Khám
                            </Link>
                          </Button>
                        )}
                        {canManage &&
                          actionsByStatus[a.status].map((x) =>
                            x.danger ? (
                              <ConfirmDialog
                                key={x.action}
                                trigger={
                                  <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive">
                                    {x.label}
                                  </Button>
                                }
                                title={`${x.label} lịch khám?`}
                                description={`Xác nhận ${x.label.toLowerCase()} lịch của "${a.patientName ?? ''}"?`}
                                confirmText={x.label}
                                destructive
                                onConfirm={() => void onAction(a, x.action)}
                              />
                            ) : (
                              <Button
                                key={x.action}
                                size="sm"
                                variant="outline"
                                onClick={() => void onAction(a, x.action)}
                              >
                                {x.label}
                              </Button>
                            ),
                          )}
                        {canManage && canEdit(a) && (
                          <Button asChild size="sm" variant="ghost">
                            <Link to={`/appointments/${a.id}/edit`}>Sửa</Link>
                          </Button>
                        )}
                        {canManage && (
                          <ConfirmDialog
                            trigger={
                              <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive">
                                Xoá
                              </Button>
                            }
                            title="Xoá lịch khám?"
                            description={`Xoá lịch khám của "${a.patientName ?? ''}"? Hành động này không thể hoàn tác.`}
                            confirmText="Xoá"
                            destructive
                            onConfirm={() => void onDelete(a)}
                          />
                        )}
                        {!canManage && !canRecordEncounter && (
                          <span className="text-muted-foreground">—</span>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {data && (
        <Pager
          page={data.page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          onPageChange={setPage}
        />
      )}
    </section>
  )
}
