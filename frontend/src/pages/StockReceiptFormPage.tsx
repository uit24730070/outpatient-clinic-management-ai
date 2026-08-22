import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { createStockReceipt } from '../services/stockReceiptService'
import { listMedications } from '../services/medicationService'
import { ApiException, toApiException } from '../services/apiClient'
import type { Medication, StockReceiptItemInput } from '../types/medication'

const today = () => new Date().toISOString().slice(0, 10)

const emptyItem = (): StockReceiptItemInput => ({
  medicationId: '',
  batchNumber: '',
  expiryDate: '',
  quantity: 1,
  unitCost: null,
})

export default function StockReceiptFormPage() {
  const navigate = useNavigate()

  const [medications, setMedications] = useState<Medication[]>([])
  const [supplierName, setSupplierName] = useState('')
  const [receivedAt, setReceivedAt] = useState(today())
  const [note, setNote] = useState('')
  const [items, setItems] = useState<StockReceiptItemInput[]>([emptyItem()])

  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const page = await listMedications({ page: 1, pageSize: 100 })
        if (active) setMedications(page.items)
      } catch (err) {
        setFormError(toApiException(err).message)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => { active = false }
  }, [])

  const updateItem = (index: number, patch: Partial<StockReceiptItemInput>) => {
    setItems((prev) => prev.map((it, i) => (i === index ? { ...it, ...patch } : it)))
  }

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setFormError(null)
    setFieldErrors({})
    try {
      await createStockReceipt({
        supplierName: supplierName.trim(),
        // Quy đổi ngày nhận (chỉ ngày) sang thời điểm đầu ngày UTC.
        receivedAt: new Date(receivedAt).toISOString(),
        note: note.trim() || null,
        items: items.map((it) => ({
          medicationId: it.medicationId,
          batchNumber: it.batchNumber.trim(),
          expiryDate: it.expiryDate,
          quantity: Number(it.quantity),
          unitCost: it.unitCost === null || Number.isNaN(it.unitCost) ? null : Number(it.unitCost),
        })),
      })
      navigate('/stock-receipts')
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
      <h1>Phiếu nhập kho</h1>
      {formError && <p className="alert alert--error">{formError}</p>}

      <form className="form" onSubmit={onSubmit} noValidate>
        <label className="field">
          <span>Nhà cung cấp *</span>
          <input value={supplierName} onChange={(e) => setSupplierName(e.target.value)} />
          {err('SupplierName') && <small className="field__error">{err('SupplierName')}</small>}
        </label>

        <label className="field">
          <span>Ngày nhận *</span>
          <input type="date" value={receivedAt} onChange={(e) => setReceivedAt(e.target.value)} />
        </label>

        <label className="field">
          <span>Ghi chú</span>
          <textarea rows={2} value={note} onChange={(e) => setNote(e.target.value)} />
        </label>

        <fieldset className="field">
          <span>Dòng nhập *</span>
          {err('Items') && <small className="field__error">{err('Items')}</small>}
          <table className="table">
            <thead>
              <tr>
                <th>Thuốc</th>
                <th>Số lô</th>
                <th>Hạn dùng</th>
                <th>Số lượng</th>
                <th>Đơn giá</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map((it, i) => (
                <tr key={i}>
                  <td>
                    <select value={it.medicationId} onChange={(e) => updateItem(i, { medicationId: e.target.value })}>
                      <option value="" disabled>— Chọn thuốc —</option>
                      {medications.map((m) => (
                        <option key={m.id} value={m.id}>{m.code} · {m.name}</option>
                      ))}
                    </select>
                  </td>
                  <td><input value={it.batchNumber}
                    onChange={(e) => updateItem(i, { batchNumber: e.target.value })} /></td>
                  <td><input type="date" value={it.expiryDate}
                    onChange={(e) => updateItem(i, { expiryDate: e.target.value })} /></td>
                  <td><input type="number" min={1} value={it.quantity}
                    onChange={(e) => updateItem(i, { quantity: Number(e.target.value) })} /></td>
                  <td><input type="number" min={0} step="0.01" value={it.unitCost ?? ''}
                    onChange={(e) => updateItem(i, { unitCost: e.target.value === '' ? null : Number(e.target.value) })} /></td>
                  <td>
                    {items.length > 1 && (
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
          <button type="button" className="btn" onClick={() => setItems((prev) => [...prev, emptyItem()])}>
            + Thêm dòng
          </button>
        </fieldset>

        <div className="form__actions">
          <button className="btn" type="button" onClick={() => navigate('/stock-receipts')} disabled={saving}>
            Huỷ
          </button>
          <button className="btn btn--primary" type="submit" disabled={saving}>
            {saving ? 'Đang lưu…' : 'Tạo phiếu nhập'}
          </button>
        </div>
      </form>
    </section>
  )
}
