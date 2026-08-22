import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { createMedication, getMedication, updateMedication } from '../services/medicationService'
import { ApiException, toApiException } from '../services/apiClient'
import type { MedicationFormValues } from '../types/medication'

const emptyForm: MedicationFormValues = {
  name: '',
  activeIngredient: '',
  unit: '',
  reorderLevel: 0,
  description: null,
}

export default function MedicationFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [values, setValues] = useState<MedicationFormValues>(emptyForm)
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        if (id) {
          const m = await getMedication(id)
          if (active) {
            setValues({
              name: m.name,
              activeIngredient: m.activeIngredient,
              unit: m.unit,
              reorderLevel: m.reorderLevel,
              description: m.description,
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

  const setField = <K extends keyof MedicationFormValues>(key: K, value: MedicationFormValues[K]) => {
    setValues((v) => ({ ...v, [key]: value }))
  }

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setFormError(null)
    setFieldErrors({})
    try {
      if (isEdit && id) {
        await updateMedication(id, values)
      } else {
        await createMedication(values)
      }
      navigate('/medications')
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
      <h1>{isEdit ? 'Sửa thuốc' : 'Thêm thuốc'}</h1>
      {formError && <p className="alert alert--error">{formError}</p>}

      <form className="form" onSubmit={onSubmit} noValidate>
        <label className="field">
          <span>Tên thuốc *</span>
          <input value={values.name} onChange={(e) => setField('name', e.target.value)} />
          {err('Name') && <small className="field__error">{err('Name')}</small>}
        </label>

        <label className="field">
          <span>Hoạt chất *</span>
          <input value={values.activeIngredient} onChange={(e) => setField('activeIngredient', e.target.value)} />
          {err('ActiveIngredient') && <small className="field__error">{err('ActiveIngredient')}</small>}
        </label>

        <label className="field">
          <span>Đơn vị tính *</span>
          <input placeholder="viên / vỉ / chai / ống…" value={values.unit}
            onChange={(e) => setField('unit', e.target.value)} />
          {err('Unit') && <small className="field__error">{err('Unit')}</small>}
        </label>

        <label className="field">
          <span>Ngưỡng tồn tối thiểu</span>
          <input type="number" min={0} value={values.reorderLevel}
            onChange={(e) => setField('reorderLevel', Number(e.target.value))} />
          {err('ReorderLevel') && <small className="field__error">{err('ReorderLevel')}</small>}
        </label>

        <label className="field">
          <span>Mô tả</span>
          <textarea rows={2} value={values.description ?? ''}
            onChange={(e) => setField('description', e.target.value || null)} />
          {err('Description') && <small className="field__error">{err('Description')}</small>}
        </label>

        <div className="form__actions">
          <button className="btn" type="button" onClick={() => navigate('/medications')} disabled={saving}>
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
