import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  createAppointment,
  getAppointment,
  updateAppointment,
} from '../services/appointmentService'
import { listPatients, createPatient } from '../services/patientService'
import { listDoctors } from '../services/doctorService'
import { ApiException, toApiException } from '../services/apiClient'
import type { AppointmentFormValues } from '../types/appointment'
import { Gender, genderLabels, type Patient, type GenderValue, type PatientFormValues } from '../types/patient'
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

  // Tạo nhanh bệnh nhân ngay trong form đặt lịch (không phải rời trang).
  const emptyNewPatient: PatientFormValues = {
    fullName: '', dateOfBirth: null, gender: Gender.Unknown, phoneNumber: null, address: null,
  }
  const [showCreatePatient, setShowCreatePatient] = useState(false)
  const [newPatient, setNewPatient] = useState<PatientFormValues>(emptyNewPatient)
  const [npErrors, setNpErrors] = useState<Record<string, string[]>>({})
  const [npError, setNpError] = useState<string | null>(null)
  const [npSaving, setNpSaving] = useState(false)

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

  // Cập nhật một trường của bệnh nhân mới.
  const setNp = <K extends keyof PatientFormValues>(key: K, value: PatientFormValues[K]) =>
    setNewPatient((prev) => ({ ...prev, [key]: value }))

  // Tạo bệnh nhân rồi tự chọn vào lịch (không rời trang). Không submit form đặt lịch.
  const onCreatePatient = async () => {
    setNpSaving(true)
    setNpError(null)
    setNpErrors({})
    try {
      const created = await createPatient({
        ...newPatient,
        fullName: newPatient.fullName.trim(),
        phoneNumber: newPatient.phoneNumber?.trim() || null,
        address: newPatient.address?.trim() || null,
        dateOfBirth: newPatient.dateOfBirth || null,
      })
      // Đưa lên đầu danh sách chọn + chọn luôn bệnh nhân vừa tạo.
      setPatients((prev) => [created, ...prev.filter((p) => p.id !== created.id)])
      setPatientId(created.id)
      setShowCreatePatient(false)
      setNewPatient(emptyNewPatient)
      setPatientSearch('')
      // Xoá lỗi field PatientId (nếu có) do đã chọn được bệnh nhân.
      setFieldErrors((prev) => { const { PatientId: _drop, ...rest } = prev; return rest })
    } catch (err) {
      const ex = toApiException(err)
      if (ex instanceof ApiException && ex.details) setNpErrors(ex.details)
      setNpError(ex.message)
    } finally {
      setNpSaving(false)
    }
  }

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
        <div className="field">
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
              <button
                type="button"
                className="link-btn patient-create__toggle"
                onClick={() => setShowCreatePatient((v) => !v)}
              >
                {showCreatePatient ? '× Đóng tạo mới' : '+ Không tìm thấy? Tạo bệnh nhân mới'}
              </button>

              {showCreatePatient && (
                <div className="patient-create">
                  <p className="patient-create__title">Tạo nhanh bệnh nhân</p>
                  {npError && <p className="alert alert--error">{npError}</p>}

                  <label className="field">
                    <span>Họ tên *</span>
                    <input
                      value={newPatient.fullName}
                      onChange={(e) => setNp('fullName', e.target.value)}
                      placeholder="Nguyễn Văn A"
                    />
                    {npErrors.FullName?.[0] && <small className="field__error">{npErrors.FullName[0]}</small>}
                  </label>

                  <div className="patient-create__row">
                    <label className="field">
                      <span>Giới tính</span>
                      <select
                        value={newPatient.gender}
                        onChange={(e) => setNp('gender', Number(e.target.value) as GenderValue)}
                      >
                        {Object.entries(genderLabels).map(([value, label]) => (
                          <option key={value} value={value}>{label}</option>
                        ))}
                      </select>
                    </label>
                    <label className="field">
                      <span>Ngày sinh</span>
                      <input
                        type="date"
                        value={newPatient.dateOfBirth ?? ''}
                        onChange={(e) => setNp('dateOfBirth', e.target.value || null)}
                      />
                      {npErrors.DateOfBirth?.[0] && <small className="field__error">{npErrors.DateOfBirth[0]}</small>}
                    </label>
                  </div>

                  <label className="field">
                    <span>Số điện thoại</span>
                    <input
                      value={newPatient.phoneNumber ?? ''}
                      onChange={(e) => setNp('phoneNumber', e.target.value || null)}
                      placeholder="09xxxxxxxx"
                    />
                    {npErrors.PhoneNumber?.[0] && <small className="field__error">{npErrors.PhoneNumber[0]}</small>}
                  </label>

                  <label className="field">
                    <span>Địa chỉ</span>
                    <input
                      value={newPatient.address ?? ''}
                      onChange={(e) => setNp('address', e.target.value || null)}
                    />
                  </label>

                  <div className="patient-create__actions">
                    <button
                      type="button"
                      className="btn btn--primary"
                      disabled={npSaving || !newPatient.fullName.trim()}
                      onClick={() => void onCreatePatient()}
                    >
                      {npSaving ? 'Đang tạo…' : 'Tạo & chọn bệnh nhân'}
                    </button>
                  </div>
                </div>
              )}
            </>
          )}
          {err('PatientId') && <small className="field__error">{err('PatientId')}</small>}
        </div>

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
