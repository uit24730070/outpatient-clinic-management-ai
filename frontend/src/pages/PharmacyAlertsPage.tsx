import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { getPharmacyAlerts } from '../services/pharmacyService'
import { toApiException } from '../services/apiClient'
import type { PharmacyAlerts } from '../types/medication'

const EXPIRING_IN_DAYS = 30

export default function PharmacyAlertsPage() {
  const [data, setData] = useState<PharmacyAlerts | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const result = await getPharmacyAlerts(EXPIRING_IN_DAYS)
        if (active) setData(result)
      } catch (err) {
        if (active) setError(toApiException(err).message)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => { active = false }
  }, [])

  return (
    <section>
      <div className="page-head">
        <h1>Cảnh báo kho</h1>
        <Link className="btn" to="/medications">Danh mục thuốc</Link>
      </div>

      {error && <p className="alert alert--error">{error}</p>}
      {loading && <p>Đang tải…</p>}

      {data && (
        <>
          <h2>Tồn thấp <span className="muted">(≤ ngưỡng đặt lại)</span></h2>
          <table className="table">
            <thead>
              <tr>
                <th>Mã</th>
                <th>Tên thuốc</th>
                <th>Tồn</th>
                <th>Ngưỡng</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.lowStock.length === 0 && (
                <tr><td colSpan={5} className="table__empty">Không có thuốc tồn thấp.</td></tr>
              )}
              {data.lowStock.map((m) => (
                <tr key={m.medicationId}>
                  <td>{m.code}</td>
                  <td>{m.name}</td>
                  <td className="text-danger">{m.stockOnHand} {m.unit}</td>
                  <td>{m.reorderLevel}</td>
                  <td className="table__actions">
                    <Link to={`/stock-receipts/new`}>Nhập thêm</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <h2>Sắp / đã hết hạn <span className="muted">(trong {data.expiringInDays} ngày tới, còn tồn)</span></h2>
          <table className="table">
            <thead>
              <tr>
                <th>Thuốc</th>
                <th>Số lô</th>
                <th>Hạn dùng</th>
                <th>Tồn lô</th>
                <th>Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              {data.expiringBatches.length === 0 && (
                <tr><td colSpan={5} className="table__empty">Không có lô sắp/đã hết hạn.</td></tr>
              )}
              {data.expiringBatches.map((b) => (
                <tr key={b.batchId}>
                  <td>{b.medicationName} <span className="muted">({b.medicationCode})</span></td>
                  <td>{b.batchNumber}</td>
                  <td className={b.isExpired ? 'text-danger' : undefined}>{b.expiryDate}</td>
                  <td>{b.quantityOnHand}</td>
                  <td>
                    <span className={`badge ${b.isExpired ? 'badge--cancelled' : 'badge--scheduled'}`}>
                      {b.isExpired ? 'Đã hết hạn' : 'Sắp hết hạn'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      )}
    </section>
  )
}
