import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  createAppointment,
  getAppointment,
  updateAppointment,
} from '../services/appointmentService'
import { listPatients } from '../services/patientService'
import { listDoctors } from '../services/doctorService'
import { ApiException, toApiException } from '../services/apiClient'
import type { AppointmentFormValues } from '../types/appointment'
import type { Patient } from '../types/patient'
import type { Doctor } from '../types/doctor'

// Chuyển ISO (UTC) → giá trị cho input datetime-local (giờ địa phương).
function toLocalInput(iso: string): string {
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

// Giá trị datetime-local (giờ địa phương) → ISO (UTC) để gửi backend.
function toIso(local: string): string {
  return new Date(local).toISOString()
}

export default function AppointmentFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [patientId, setPatientId] = useState('')
  const [doctorId, setDoctorId] = useState('')
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [reason, setReason] = useState('')
  const [readonlyNames, setReadonlyNames] = useState<{ patient: string; doctor: string } | null>(null)

  const [patientSearch, setPatientSearch] = useState('')
  const [patients, setPatients] = useState<Patient[]>([])
  const [doctors, setDoctors] = useState<Doctor[]>([])

  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const doctorPage = await listDoctors({ page: 1, pageSize: 100 })
        if (active) setDoctors(doctorPage.items)

        if (id) {
          const a = await getAppointment(id)
          if (active) {
            setStart(toLocalInput(a.startTime))
            setEnd(toLocalInput(a.endTime))
            setReason(a.reason ?? '')
            setPatientId(a.patientId)
            setDoctorId(a.doctorId)
            setReadonlyNames({ patient: a.patientName ?? '—', doctor: a.doctorName ?? '—' })
          }
        }
      } catch (err) {
        if (active) setFormError(toApiException(err).message)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => { active = false }
  }, [id])

  // Tìm kiếm bệnh nhân cho dropdown (chỉ khi tạo mới).
  useEffect(() => {
    if (isEdit) return
    let active = true
    void (async () => {
      try {
        const result = await listPatients({ page: 1, pageSize: 20, search: patientSearch.trim() || undefined })
        if (active) setPatients(result.items)
      } catch {
        // Bỏ qua lỗi tìm kiếm; người dùng thử lại.
      }
    })()
    return () => { active = false }
  }, [isEdit, patientSearch])

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setFormError(null)
    setFieldErrors({})
    try {
      if (isEdit && id) {
        await updateAppointment(id, { startTime: toIso(start), endTime: toIso(end), reason: reason.trim() || null })
      } else {
        const values: AppointmentFormValues = {
          patientId,
          doctorId,
          startTime: toIso(start),
          endTime: toIso(end),
          reason: reason.trim() || null,
        }
        await createAppointment(values)
      }
      navigate('/appointments')
    } catch (err) {
      const ex = toApiException(err)
      if (ex instanceof ApiException && ex.details) setFieldErrors(ex.details)
      setFormError(ex.message)
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p>Đang tải…</p>

  const err = (field: string) => fieldErrors[field]?.[0]

  return (
    <section className="form-wrap">
      <h1>{isEdit ? 'Sửa lịch khám' : 'Đặt lịch khám'}</h1>
      {formError && <p className="alert alert--error">{formError}</p>}

      <form className="form" onSubmit={onSubmit} noValidate>
        <label className="field">
          <span>Bệnh nhân *</span>
          {isEdit ? (
            <input value={readonlyNames?.patient ?? ''} disabled />
          ) : (
            <>
              <input
                type="search"
                placeholder="Tìm bệnh nhân theo tên, mã…"
                value={patientSearch}
                onChange={(e) => setPatientSearch(e.target.value)}
              />
              <select value={patientId} onChange={(e) => setPatientId(e.target.value)}>
                <option value="" disabled>— Chọn bệnh nhân —</option>
                {patients.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName} ({p.code})</option>
                ))}
              </select>
            </>
          )}
          {err('PatientId') && <small className="field__error">{err('PatientId')}</small>}
        </label>

        <label className="field">
          <span>Bác sĩ *</span>
          {isEdit ? (
            <input value={readonlyNames?.doctor ?? ''} disabled />
          ) : (
            <select value={doctorId} onChange={(e) => setDoctorId(e.target.value)}>
              <option value="" disabled>— Chọn bác sĩ —</option>
              {doctors.map((d) => (
                <option key={d.id} value={d.id}>{d.fullName}</option>
              ))}
            </select>
          )}
          {err('DoctorId') && <small className="field__error">{err('DoctorId')}</small>}
        </label>

        <label className="field">
          <span>Bắt đầu *</span>
          <input type="datetime-local" value={start} onChange={(e) => setStart(e.target.value)} />
          {err('StartTime') && <small className="field__error">{err('StartTime')}</small>}
        </label>

        <label className="field">
          <span>Kết thúc *</span>
          <input type="datetime-local" value={end} onChange={(e) => setEnd(e.target.value)} />
          {err('EndTime') && <small className="field__error">{err('EndTime')}</small>}
        </label>

        <label className="field">
          <span>Lý do khám</span>
          <input value={reason} onChange={(e) => setReason(e.target.value)} />
          {err('Reason') && <small className="field__error">{err('Reason')}</small>}
        </label>

        <div className="form__actions">
          <button className="btn" type="button" onClick={() => navigate('/appointments')} disabled={saving}>
            Huỷ
          </button>
          <button className="btn btn--primary" type="submit" disabled={saving}>
            {saving ? 'Đang lưu…' : 'Lưu'}
          </button>
        </div>
      </form>
    </section>
  )
}
