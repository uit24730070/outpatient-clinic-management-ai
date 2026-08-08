import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { createPatient, getPatient, updatePatient } from '../services/patientService'
import { ApiException, toApiException } from '../services/apiClient'
import { Gender, genderLabels, type GenderValue, type PatientFormValues } from '../types/patient'

const emptyForm: PatientFormValues = {
  fullName: '',
  dateOfBirth: null,
  gender: Gender.Unknown,
  phoneNumber: null,
  address: null,
}

export default function PatientFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [values, setValues] = useState<PatientFormValues>(emptyForm)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    let active = true
    void (async () => {
      try {
        const p = await getPatient(id)
        if (!active) return
        setValues({
          fullName: p.fullName,
          dateOfBirth: p.dateOfBirth,
          gender: p.gender,
          phoneNumber: p.phoneNumber,
          address: p.address,
        })
      } catch (err) {
        setFormError(toApiException(err).message)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => { active = false }
  }, [id])

  const setField = <K extends keyof PatientFormValues>(key: K, value: PatientFormValues[K]) => {
    setValues((v) => ({ ...v, [key]: value }))
  }

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setFormError(null)
    setFieldErrors({})
    try {
      if (isEdit && id) {
        await updatePatient(id, values)
      } else {
        await createPatient(values)
      }
      navigate('/patients')
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
      <h1>{isEdit ? 'Sửa bệnh nhân' : 'Thêm bệnh nhân'}</h1>
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
          <span>Giới tính</span>
          <select
            value={values.gender}
            onChange={(e) => setField('gender', Number(e.target.value) as GenderValue)}
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
            value={values.dateOfBirth ?? ''}
            onChange={(e) => setField('dateOfBirth', e.target.value || null)}
          />
          {err('DateOfBirth') && <small className="field__error">{err('DateOfBirth')}</small>}
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
          <span>Địa chỉ</span>
          <textarea
            rows={2}
            value={values.address ?? ''}
            onChange={(e) => setField('address', e.target.value || null)}
          />
          {err('Address') && <small className="field__error">{err('Address')}</small>}
        </label>

        <div className="form__actions">
          <button className="btn" type="button" onClick={() => navigate('/patients')} disabled={saving}>
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
