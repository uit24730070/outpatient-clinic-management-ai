import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import {
  completeEncounter,
  createEncounter,
  getEncounterByAppointment,
  updateEncounter,
} from '../services/encounterService'
import { getAppointment } from '../services/appointmentService'
import { ApiException, toApiException } from '../services/apiClient'
import { EncounterStatus, type Encounter, type PrescriptionItem } from '../types/encounter'

// Dòng đơn thuốc rỗng để thêm mới.
const emptyItem = (): PrescriptionItem => ({ drugName: '', dosage: '', quantity: 1, instruction: null })

export default function EncounterFormPage() {
  const { appointmentId } = useParams<{ appointmentId: string }>()
  const navigate = useNavigate()

  const [encounter, setEncounter] = useState<Encounter | null>(null)
  const [patientName, setPatientName] = useState('')
  const [doctorName, setDoctorName] = useState('')

  const [symptoms, setSymptoms] = useState('')
  const [diagnosis, setDiagnosis] = useState('')
  const [notes, setNotes] = useState('')
  const [items, setItems] = useState<PrescriptionItem[]>([])

  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  const isCompleted = encounter?.status === EncounterStatus.Completed

  useEffect(() => {
    if (!appointmentId) return
    let active = true
    void (async () => {
      try {
        const appt = await getAppointment(appointmentId)
        if (active) {
          setPatientName(appt.patientName ?? '—')
          setDoctorName(appt.doctorName ?? '—')
        }
        const existing = await getEncounterByAppointment(appointmentId)
        if (active && existing) {
          setEncounter(existing)
          setSymptoms(existing.symptoms ?? '')
          setDiagnosis(existing.diagnosis)
          setNotes(existing.notes ?? '')
          setItems(existing.prescriptionItems)
        }
      } catch (err) {
        if (active) setFormError(toApiException(err).message)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => { active = false }
  }, [appointmentId])

  const updateItem = (index: number, patch: Partial<PrescriptionItem>) => {
    setItems((prev) => prev.map((it, i) => (i === index ? { ...it, ...patch } : it)))
  }

  const buildValues = () => ({
    symptoms: symptoms.trim() || null,
    diagnosis: diagnosis.trim(),
    notes: notes.trim() || null,
    prescriptionItems: items.map((it) => ({
      drugName: it.drugName.trim(),
      dosage: it.dosage.trim(),
      quantity: Number(it.quantity),
      instruction: it.instruction?.trim() || null,
    })),
  })

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!appointmentId) return
    setSaving(true)
    setFormError(null)
    setFieldErrors({})
    try {
      if (encounter) {
        const saved = await updateEncounter(encounter.id, buildValues())
        setEncounter(saved)
        setItems(saved.prescriptionItems)
      } else {
        const created = await createEncounter({ appointmentId, ...buildValues() })
        setEncounter(created)
        setItems(created.prescriptionItems)
      }
    } catch (err) {
      const ex = toApiException(err)
      if (ex instanceof ApiException && ex.details) setFieldErrors(ex.details)
      setFormError(ex.message)
    } finally {
      setSaving(false)
    }
  }

  const onComplete = async () => {
    if (!encounter) return
    if (!window.confirm('Chốt phiếu khám? Sau khi chốt sẽ không sửa được và lịch khám chuyển sang Hoàn tất.')) return
    setSaving(true)
    setFormError(null)
    try {
      await completeEncounter(encounter.id)
      navigate('/appointments')
    } catch (err) {
      setFormError(toApiException(err).message)
      setSaving(false)
    }
  }

  if (loading) return <p>Đang tải…</p>

  const err = (field: string) => fieldErrors[field]?.[0]

  return (
    <section className="form-wrap">
      <h1>Phiếu khám</h1>
      <p className="muted">
        Bệnh nhân: <strong>{patientName}</strong> · Bác sĩ: <strong>{doctorName}</strong>
        {isCompleted && ' · Đã chốt'}
      </p>
      {formError && <p className="alert alert--error">{formError}</p>}

      <form className="form" onSubmit={onSubmit} noValidate>
        <label className="field">
          <span>Triệu chứng</span>
          <textarea rows={2} value={symptoms} disabled={isCompleted}
            onChange={(e) => setSymptoms(e.target.value)} />
          {err('Symptoms') && <small className="field__error">{err('Symptoms')}</small>}
        </label>

        <label className="field">
          <span>Chẩn đoán *</span>
          <textarea rows={2} value={diagnosis} disabled={isCompleted}
            onChange={(e) => setDiagnosis(e.target.value)} />
          {err('Diagnosis') && <small className="field__error">{err('Diagnosis')}</small>}
        </label>

        <label className="field">
          <span>Chỉ định / Ghi chú</span>
          <textarea rows={2} value={notes} disabled={isCompleted}
            onChange={(e) => setNotes(e.target.value)} />
          {err('Notes') && <small className="field__error">{err('Notes')}</small>}
        </label>

        <fieldset className="field">
          <span>Đơn thuốc</span>
          <table className="table">
            <thead>
              <tr>
                <th>Tên thuốc</th>
                <th>Liều</th>
                <th>Số lượng</th>
                <th>Cách dùng</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 && (
                <tr><td colSpan={5} className="table__empty">Chưa có thuốc nào.</td></tr>
              )}
              {items.map((it, i) => (
                <tr key={i}>
                  <td><input value={it.drugName} disabled={isCompleted}
                    onChange={(e) => updateItem(i, { drugName: e.target.value })} /></td>
                  <td><input value={it.dosage} disabled={isCompleted}
                    onChange={(e) => updateItem(i, { dosage: e.target.value })} /></td>
                  <td><input type="number" min={1} value={it.quantity} disabled={isCompleted}
                    onChange={(e) => updateItem(i, { quantity: Number(e.target.value) })} /></td>
                  <td><input value={it.instruction ?? ''} disabled={isCompleted}
                    onChange={(e) => updateItem(i, { instruction: e.target.value })} /></td>
                  <td>
                    {!isCompleted && (
                      <button type="button" className="link-btn link-btn--danger"
                        onClick={() => setItems((prev) => prev.filter((_, idx) => idx !== i))}>
                        Xoá
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!isCompleted && (
            <button type="button" className="btn" onClick={() => setItems((prev) => [...prev, emptyItem()])}>
              + Thêm thuốc
            </button>
          )}
        </fieldset>

        <div className="form__actions">
          <button className="btn" type="button" onClick={() => navigate('/appointments')} disabled={saving}>
            Quay lại
          </button>
          {!isCompleted && (
            <button className="btn btn--primary" type="submit" disabled={saving}>
              {saving ? 'Đang lưu…' : encounter ? 'Lưu' : 'Tạo phiếu'}
            </button>
          )}
          {encounter && !isCompleted && (
            <button className="btn btn--primary" type="button" onClick={onComplete} disabled={saving}>
              Chốt phiếu
            </button>
          )}
        </div>
      </form>
    </section>
  )
}
