import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listAppointments } from '../services/appointmentService'
import { toApiException } from '../services/apiClient'
import { useAuth } from '../store/auth'
import {
  AppointmentStatus,
  appointmentStatusClass,
  appointmentStatusLabels,
  type Appointment,
} from '../types/appointment'

// Các trạng thái thuộc "phòng khám của tôi": đã tiếp đón hoặc đang khám.
const CLINIC_STATUSES: number[] = [AppointmentStatus.CheckedIn, AppointmentStatus.InProgress]

function formatTime(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/**
 * Phòng khám của tôi (Bác sĩ): bệnh nhân đang chờ/đang khám của chính bác sĩ đăng nhập,
 * lọc theo doctorId = của tôi (ADR 0009). Bác sĩ chưa gắn hồ sơ → hướng dẫn nhờ Admin.
 */
export default function MyClinicPage() {
  const { doctorId } = useAuth()
  const [items, setItems] = useState<Appointment[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!doctorId) return
    setLoading(true)
    setError(null)
    try {
      // Lấy lịch của bác sĩ rồi lọc trạng thái "đang trong phòng khám" phía client.
      const result = await listAppointments({ page: 1, pageSize: 100, doctorId })
      const clinic = result.items
        .filter((a) => CLINIC_STATUSES.includes(a.status))
        .sort((x, y) => x.startTime.localeCompare(y.startTime))
      setItems(clinic)
    } catch (err) {
      setError(toApiException(err).message)
    } finally {
      setLoading(false)
    }
  }, [doctorId])

  useEffect(() => {
    void load()
  }, [load])

  // Bác sĩ chưa được gắn hồ sơ Doctor → không lọc được "của tôi".
  if (!doctorId) {
    return (
      <section>
        <div className="page-head"><h1>Phòng khám của tôi</h1></div>
        <p className="alert alert--warning">
          Tài khoản của bạn chưa được gắn với hồ sơ bác sĩ. Vui lòng nhờ quản trị viên liên kết
          tài khoản với hồ sơ bác sĩ để xem danh sách bệnh nhân của bạn.
        </p>
      </section>
    )
  }

  return (
    <section>
      <div className="page-head">
        <h1>Phòng khám của tôi</h1>
        <button className="btn" onClick={() => void load()}>Làm mới</button>
      </div>

      {error && <p className="alert alert--error">{error}</p>}
      {loading && <p>Đang tải…</p>}

      {!loading && (
        <table className="table">
          <thead>
            <tr>
              <th>Thời gian</th>
              <th>Bệnh nhân</th>
              <th>Lý do</th>
              <th>Trạng thái</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {items.length === 0 && (
              <tr><td colSpan={5} className="table__empty">Hiện không có bệnh nhân nào đang chờ/đang khám.</td></tr>
            )}
            {items.map((a) => (
              <tr key={a.id}>
                <td>{formatTime(a.startTime)}</td>
                <td>{a.patientName ?? '—'}</td>
                <td>{a.reason ?? '—'}</td>
                <td>
                  <span className={`badge ${appointmentStatusClass[a.status]}`}>
                    {appointmentStatusLabels[a.status]}
                  </span>
                </td>
                <td className="table__actions">
                  {a.status === AppointmentStatus.InProgress ? (
                    <Link to={`/appointments/${a.id}/encounter`}>Khám</Link>
                  ) : (
                    <span className="text-muted">Chờ tiếp đón bắt đầu khám</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  )
}
