import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import {
  deleteAppointment,
  listAppointments,
  transitionAppointment,
  type AppointmentAction,
} from '../services/appointmentService'
import { listDoctors } from '../services/doctorService'
import { toApiException } from '../services/apiClient'
import { useAuth } from '../store/auth'
import {
  AppointmentStatus,
  appointmentStatusClass,
  appointmentStatusLabels,
  type Appointment,
} from '../types/appointment'
import type { Doctor } from '../types/doctor'
import type { PagedResult } from '../types/common'

const PAGE_SIZE = 10

// Các hành động chuyển trạng thái khả dụng theo trạng thái hiện tại.
const actionsByStatus: Record<number, { action: AppointmentAction; label: string }[]> = {
  [AppointmentStatus.Scheduled]: [
    { action: 'check-in', label: 'Check-in' },
    { action: 'cancel', label: 'Huỷ' },
    { action: 'no-show', label: 'Không đến' },
  ],
  [AppointmentStatus.CheckedIn]: [
    { action: 'start', label: 'Bắt đầu khám' },
    { action: 'cancel', label: 'Huỷ' },
    { action: 'no-show', label: 'Không đến' },
  ],
  [AppointmentStatus.InProgress]: [
    { action: 'complete', label: 'Hoàn tất' },
    { action: 'cancel', label: 'Huỷ' },
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

export default function AppointmentsListPage() {
  const { canManage } = useAuth()
  const [date, setDate] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const [doctors, setDoctors] = useState<Doctor[]>([])
  const [data, setData] = useState<PagedResult<Appointment> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void (async () => {
      try {
        const page = await listDoctors({ page: 1, pageSize: 100 })
        setDoctors(page.items)
      } catch {
        // Danh sách bác sĩ chỉ phục vụ bộ lọc; lỗi ở đây không chặn danh sách lịch.
      }
    })()
  }, [])

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
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
      setError(toApiException(err).message)
    } finally {
      setLoading(false)
    }
  }, [page, date, doctorId, status])

  useEffect(() => {
    void load()
  }, [load])

  const onAction = async (a: Appointment, action: AppointmentAction) => {
    const labels: Record<AppointmentAction, string> = {
      'check-in': 'check-in',
      start: 'bắt đầu khám',
      complete: 'hoàn tất',
      cancel: 'huỷ',
      'no-show': 'đánh dấu không đến',
    }
    if ((action === 'cancel' || action === 'no-show') &&
        !window.confirm(`Xác nhận ${labels[action]} lịch của "${a.patientName ?? ''}"?`)) return
    try {
      await transitionAppointment(a.id, action)
      void load()
    } catch (err) {
      setError(toApiException(err).message)
    }
  }

  const onDelete = async (a: Appointment) => {
    if (!window.confirm(`Xoá lịch khám của "${a.patientName ?? ''}"?`)) return
    try {
      await deleteAppointment(a.id)
      void load()
    } catch (err) {
      setError(toApiException(err).message)
    }
  }

  const canEdit = (a: Appointment) =>
    a.status === AppointmentStatus.Scheduled || a.status === AppointmentStatus.CheckedIn

  return (
    <section>
      <div className="page-head">
        <h1>Lịch khám</h1>
        {canManage && <Link className="btn btn--primary" to="/appointments/new">+ Đặt lịch</Link>}
      </div>

      <div className="toolbar">
        <input type="date" value={date} onChange={(e) => { setPage(1); setDate(e.target.value) }} />
        <select value={doctorId} onChange={(e) => { setPage(1); setDoctorId(e.target.value) }}>
          <option value="">— Tất cả bác sĩ —</option>
          {doctors.map((d) => (
            <option key={d.id} value={d.id}>{d.fullName}</option>
          ))}
        </select>
        <select value={status} onChange={(e) => { setPage(1); setStatus(e.target.value) }}>
          <option value="">— Tất cả trạng thái —</option>
          {Object.values(AppointmentStatus).map((v) => (
            <option key={v} value={v}>{appointmentStatusLabels[v]}</option>
          ))}
        </select>
        {(date || doctorId || status) && (
          <button className="btn" onClick={() => { setPage(1); setDate(''); setDoctorId(''); setStatus('') }}>
            Xoá lọc
          </button>
        )}
      </div>

      {error && <p className="alert alert--error">{error}</p>}
      {loading && <p>Đang tải…</p>}

      {data && (
        <>
          <table className="table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Bệnh nhân</th>
                <th>Bác sĩ</th>
                <th>Lý do</th>
                <th>Trạng thái</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 && (
                <tr><td colSpan={6} className="table__empty">Không có lịch khám nào.</td></tr>
              )}
              {data.items.map((a) => (
                <tr key={a.id}>
                  <td>{formatTime(a.startTime)} – {formatTime(a.endTime).split(', ')[1] ?? formatTime(a.endTime)}</td>
                  <td>{a.patientName ?? '—'}</td>
                  <td>{a.doctorName ?? '—'}</td>
                  <td>{a.reason ?? '—'}</td>
                  <td>
                    <span className={`badge ${appointmentStatusClass[a.status]}`}>
                      {appointmentStatusLabels[a.status]}
                    </span>
                  </td>
                  <td className="table__actions">
                    {canManage ? (
                      <>
                        {actionsByStatus[a.status].map((x) => (
                          <button
                            key={x.action}
                            className={`link-btn${x.action === 'cancel' || x.action === 'no-show' ? ' link-btn--danger' : ''}`}
                            onClick={() => onAction(a, x.action)}
                          >
                            {x.label}
                          </button>
                        ))}
                        {canEdit(a) && <Link to={`/appointments/${a.id}/edit`}>Sửa</Link>}
                        <button className="link-btn link-btn--danger" onClick={() => onDelete(a)}>Xoá</button>
                      </>
                    ) : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="pager">
            <button className="btn" disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>
              ← Trước
            </button>
            <span>Trang {data.page}/{Math.max(data.totalPages, 1)} · {data.totalCount} bản ghi</span>
            <button className="btn" disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>
              Sau →
            </button>
          </div>
        </>
      )}
    </section>
  )
}
