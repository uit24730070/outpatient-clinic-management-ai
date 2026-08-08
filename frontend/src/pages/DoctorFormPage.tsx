import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { createDoctor, getDoctor, updateDoctor } from '../services/doctorService'
import { listSpecialties } from '../services/specialtyService'
import { ApiException, toApiException } from '../services/apiClient'
import type { DoctorFormValues } from '../types/doctor'
import type { Specialty } from '../types/specialty'

const emptyForm: DoctorFormValues = {
  fullName: '',
  specialtyId: '',
  phoneNumber: null,
  email: null,
}

export default function DoctorFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [values, setValues] = useState<DoctorFormValues>(emptyForm)
  const [specialties, setSpecialties] = useState<Specialty[]>([])
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        // Nạp danh sách chuyên khoa cho dropdown (lấy tối đa 100).
        const specialtyPage = await listSpecialties({ page: 1, pageSize: 100 })
        if (active) setSpecialties(specialtyPage.items)

        if (id) {
          const d = await getDoctor(id)
          if (active) {
            setValues({
              fullName: d.fullName,
              specialtyId: d.specialtyId,
              phoneNumber: d.phoneNumber,
              email: d.email,
            })
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

  const setField = <K extends keyof DoctorFormValues>(key: K, value: DoctorFormValues[K]) => {
    setValues((v) => ({ ...v, [key]: value }))
  }

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setFormError(null)
    setFieldErrors({})
    try {
      if (isEdit && id) {
        await updateDoctor(id, values)
      } else {
        await createDoctor(values)
      }
      navigate('/doctors')
    } catch (err) {
      const ex = toApiException(err)
      if (ex instanceof ApiException && ex.details) {
        setFieldErrors(ex.details)
      }
      setFormError(ex.message)
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p>Đang tải…</p>

  const err = (field: string) => fieldErrors[field]?.[0]

  return (
    <section className="form-wrap">
      <h1>{isEdit ? 'Sửa bác sĩ' : 'Thêm bác sĩ'}</h1>
      {formError && <p className="alert alert--error">{formError}</p>}

      <form className="form" onSubmit={onSubmit} noValidate>
        <label className="field">
          <span>Họ tên *</span>
          <input
            value={values.fullName}
            onChange={(e) => setField('fullName', e.target.value)}
          />
          {err('FullName') && <small className="field__error">{err('FullName')}</small>}
        </label>

        <label className="field">
          <span>Chuyên khoa *</span>
          <select
            value={values.specialtyId}
            onChange={(e) => setField('specialtyId', e.target.value)}
          >
            <option value="" disabled>— Chọn chuyên khoa —</option>
            {specialties.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </select>
          {err('SpecialtyId') && <small className="field__error">{err('SpecialtyId')}</small>}
        </label>

        <label className="field">
          <span>Số điện thoại</span>
          <input
            value={values.phoneNumber ?? ''}
            onChange={(e) => setField('phoneNumber', e.target.value || null)}
          />
          {err('PhoneNumber') && <small className="field__error">{err('PhoneNumber')}</small>}
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
          <button className="btn" type="button" onClick={() => navigate('/doctors')} disabled={saving}>
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
