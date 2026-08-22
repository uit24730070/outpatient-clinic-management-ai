import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { getMedication, getMedicationBatches } from '../services/medicationService'
import { toApiException } from '../services/apiClient'
import type { Medication, MedicationBatch } from '../types/medication'

export default function MedicationBatchesPage() {
  const { id } = useParams<{ id: string }>()
  const [medication, setMedication] = useState<Medication | null>(null)
  const [batches, setBatches] = useState<MedicationBatch[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    let active = true
    void (async () => {
      try {
        const [m, bs] = await Promise.all([getMedication(id), getMedicationBatches(id)])
        if (active) {
          setMedication(m)
          setBatches(bs)
        }
      } catch (err) {
        if (active) setError(toApiException(err).message)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => { active = false }
  }, [id])

  if (loading) return <p>Đang tải…</p>

  const today = new Date().toISOString().slice(0, 10)

  return (
    <section>
      <div className="page-head">
        <h1>Lô thuốc {medication ? `· ${medication.name}` : ''}</h1>
        <Link className="btn" to="/medications">← Danh mục</Link>
      </div>

      {medication && (
        <p className="muted">
          Mã: <strong>{medication.code}</strong> · Hoạt chất: <strong>{medication.activeIngredient}</strong> ·
          Tồn tổng: <strong>{medication.stockOnHand}</strong> {medication.unit}
        </p>
      )}

      {error && <p className="alert alert--error">{error}</p>}

      <table className="table">
        <thead>
          <tr>
            <th>Số lô</th>
            <th>Hạn dùng</th>
            <th>Tồn</th>
          </tr>
        </thead>
        <tbody>
          {batches.length === 0 && (
            <tr><td colSpan={3} className="table__empty">Chưa có lô nào. Hãy nhập kho để tạo lô.</td></tr>
          )}
          {batches.map((b) => {
            const expired = b.expiryDate < today
            return (
              <tr key={b.id}>
                <td>{b.batchNumber}</td>
                <td className={expired ? 'text-danger' : undefined}>{b.expiryDate}{expired ? ' (đã hết hạn)' : ''}</td>
                <td>{b.quantityOnHand}</td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </section>
  )
}
