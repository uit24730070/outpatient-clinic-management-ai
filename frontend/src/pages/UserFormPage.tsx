import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  createUser,
  getUser,
  linkUserToDoctor,
  unlinkUserFromDoctor,
  updateUser,
} from '../services/userService'
import { listDoctors } from '../services/doctorService'
import { ApiException, toApiException } from '../services/apiClient'
import { UserRole, roleLabels, type UserRoleValue } from '../types/auth'
import type { Doctor } from '../types/doctor'

interface FormState {
  username: string
  password: string
  fullName: string
  role: UserRoleValue
  email: string | null
}

const emptyForm: FormState = {
  username: '',
  password: '',
  fullName: '',
  role: UserRole.Receptionist,
  email: null,
}

export default function UserFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [values, setValues] = useState<FormState>(emptyForm)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  // Trạng thái gắn hồ sơ bác sĩ (chỉ dùng khi edit + role Doctor).
  const [doctors, setDoctors] = useState<Doctor[]>([])
  const [linkedDoctorId, setLinkedDoctorId] = useState<string | null>(null)
  const [selectedDoctorId, setSelectedDoctorId] = useState<string>('')
  const [linkError, setLinkError] = useState<string | null>(null)
  const [linkBusy, setLinkBusy] = useState(false)

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        // Nạp bác sĩ để phục vụ gắn liên kết (lấy tối đa 100 — đủ quy mô phòng khám).
        const doctorPage = await listDoctors({ page: 1, pageSize: 100 })
        if (active) setDoctors(doctorPage.items)

        if (id) {
          const u = await getUser(id)
          if (active) {
            setValues({ username: u.username, password: '', fullName: u.fullName, role: u.role, email: u.email })
            setLinkedDoctorId(u.doctorId)
            setSelectedDoctorId(u.doctorId ?? '')
          }
        }
      } catch (err) {
        setFormError(toApiException(err).message)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => { active = false }
  }, [id])

  const setField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setValues((v) => ({ ...v, [key]: value }))
  }

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setFormError(null)
    setFieldErrors({})
    try {
      if (isEdit && id) {
        await updateUser(id, { fullName: values.fullName, role: values.role, email: values.email })
      } else {
        await createUser({
          username: values.username,
          password: values.password,
          fullName: values.fullName,
          role: values.role,
          email: values.email,
        })
      }
      navigate('/users')
    } catch (err) {
      const ex = toApiException(err)
      if (ex instanceof ApiException && ex.details) setFieldErrors(ex.details)
      setFormError(ex.message)
    } finally {
      setSaving(false)
    }
  }

  // Áp dụng gắn/gỡ liên kết ngay lập tức (tách khỏi lưu hồ sơ để tránh ràng buộc thứ tự).
  const onApplyLink = async () => {
    if (!id) return
    setLinkBusy(true)
    setLinkError(null)
    try {
      if (selectedDoctorId === (linkedDoctorId ?? '')) return
      // Gỡ liên kết cũ (nếu có) trước khi gắn mới.
      if (linkedDoctorId) await unlinkUserFromDoctor(linkedDoctorId)
      if (selectedDoctorId) await linkUserToDoctor(selectedDoctorId, id)
      setLinkedDoctorId(selectedDoctorId || null)
      // Làm mới danh sách bác sĩ để phản ánh trạng thái gắn.
      const doctorPage = await listDoctors({ page: 1, pageSize: 100 })
      setDoctors(doctorPage.items)
    } catch (err) {
      setLinkError(toApiException(err).message)
    } finally {
      setLinkBusy(false)
    }
  }

  if (loading) return <p>Đang tải…</p>

  const err = (field: string) => fieldErrors[field]?.[0]

  // Chọn được: bác sĩ chưa gắn tài khoản, hoặc bác sĩ đang gắn chính user này.
  const linkableDoctors = doctors.filter((d) => d.userId === null || d.id === linkedDoctorId)

  return (
    <section className="form-wrap">
      <h1>{isEdit ? 'Sửa người dùng' : 'Thêm người dùng'}</h1>
      {formError && <p className="alert alert--error">{formError}</p>}

      <form className="form" onSubmit={onSubmit} noValidate>
        {!isEdit && (
          <>
            <label className="field">
              <span>Tên đăng nhập *</span>
              <input value={values.username} onChange={(e) => setField('username', e.target.value)} />
              {err('Username') && <small className="field__error">{err('Username')}</small>}
            </label>

            <label className="field">
              <span>Mật khẩu *</span>
              <input
                type="password"
                value={values.password}
                onChange={(e) => setField('password', e.target.value)}
              />
              {err('Password') && <small className="field__error">{err('Password')}</small>}
            </label>
          </>
        )}

        <label className="field">
          <span>Họ tên *</span>
          <input value={values.fullName} onChange={(e) => setField('fullName', e.target.value)} />
          {err('FullName') && <small className="field__error">{err('FullName')}</small>}
        </label>

        <label className="field">
          <span>Vai trò *</span>
          <select value={values.role} onChange={(e) => setField('role', e.target.value as UserRoleValue)}>
            {Object.values(UserRole).map((r) => (
              <option key={r} value={r}>{roleLabels[r]}</option>
            ))}
          </select>
          {err('Role') && <small className="field__error">{err('Role')}</small>}
        </label>

        <label className="field">
          <span>Email</span>
          <input
            type="email"
            value={values.email ?? ''}
            onChange={(e) => setField('email', e.target.value || null)}
          />
          {err('Email') && <small className="field__error">{err('Email')}</small>}
        </label>

        <div className="form__actions">
          <button className="btn" type="button" onClick={() => navigate('/users')} disabled={saving}>
            Huỷ
          </button>
          <button className="btn btn--primary" type="submit" disabled={saving}>
            {saving ? 'Đang lưu…' : 'Lưu'}
          </button>
        </div>
      </form>

      {/* Gắn hồ sơ bác sĩ — chỉ khi đã có tài khoản (edit) và vai trò Bác sĩ. */}
      {isEdit && values.role === UserRole.Doctor && (
        <div className="form" style={{ marginTop: '1.5rem' }}>
          <h2>Hồ sơ bác sĩ</h2>
          <p className="muted">Gắn tài khoản này với một hồ sơ bác sĩ để bác sĩ thấy "Phòng khám của tôi".</p>
          {linkError && <p className="alert alert--error">{linkError}</p>}
          <label className="field">
            <span>Hồ sơ bác sĩ</span>
            <select value={selectedDoctorId} onChange={(e) => setSelectedDoctorId(e.target.value)}>
              <option value="">— Không gắn —</option>
              {linkableDoctors.map((d) => (
                <option key={d.id} value={d.id}>{d.code} · {d.fullName}</option>
              ))}
            </select>
          </label>
          <div className="form__actions">
            <button
              className="btn btn--primary"
              type="button"
              onClick={onApplyLink}
              disabled={linkBusy || selectedDoctorId === (linkedDoctorId ?? '')}
            >
              {linkBusy ? 'Đang áp dụng…' : 'Áp dụng liên kết'}
            </button>
          </div>
        </div>
      )}
    </section>
  )
}
