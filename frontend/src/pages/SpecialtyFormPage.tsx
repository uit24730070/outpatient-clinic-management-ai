import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { createSpecialty, getSpecialty, updateSpecialty } from '../services/specialtyService'
import { ApiException, toApiException } from '../services/apiClient'
import type { SpecialtyFormValues } from '../types/specialty'

const emptyForm: SpecialtyFormValues = {
  name: '',
  description: null,
}

export default function SpecialtyFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [values, setValues] = useState<SpecialtyFormValues>(emptyForm)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [loading, setLoading] = useState(isEdit)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    let active = true
    void (async () => {
      try {
        const s = await getSpecialty(id)
        if (!active) return
        setValues({ name: s.name, description: s.description })
      } catch (err) {
        setFormError(toApiException(err).message)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => { active = false }
  }, [id])

  const setField = <K extends keyof SpecialtyFormValues>(key: K, value: SpecialtyFormValues[K]) => {
    setValues((v) => ({ ...v, [key]: value }))
  }

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setFormError(null)
    setFieldErrors({})
    try {
      if (isEdit && id) {
        await updateSpecialty(id, values)
      } else {
        await createSpecialty(values)
      }
      navigate('/specialties')
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
      <h1>{isEdit ? 'Sửa chuyên khoa' : 'Thêm chuyên khoa'}</h1>
      {formError && <p className="alert alert--error">{formError}</p>}

      <form className="form" onSubmit={onSubmit} noValidate>
        <label className="field">
          <span>Tên chuyên khoa *</span>
          <input
            value={values.name}
            onChange={(e) => setField('name', e.target.value)}
          />
          {err('Name') && <small className="field__error">{err('Name')}</small>}
        </label>

        <label className="field">
          <span>Mô tả</span>
          <textarea
            rows={3}
            value={values.description ?? ''}
            onChange={(e) => setField('description', e.target.value || null)}
          />
          {err('Description') && <small className="field__error">{err('Description')}</small>}
        </label>

        <div className="form__actions">
          <button className="btn" type="button" onClick={() => navigate('/specialties')} disabled={saving}>
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
