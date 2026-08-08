import { Fragment, useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { listEncounters } from '../services/encounterService'
import { getPatient } from '../services/patientService'
import { toApiException } from '../services/apiClient'
import {
  encounterStatusClass,
  encounterStatusLabels,
  type Encounter,
} from '../types/encounter'
import type { PagedResult } from '../types/common'

const PAGE_SIZE = 10

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

export default function PatientEncountersPage() {
  const { id } = useParams<{ id: string }>()
  const [patientName, setPatientName] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Encounter> | null>(null)
  const [expanded, setExpanded] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!id) return
    void (async () => {
      try {
        const p = await getPatient(id)
        setPatientName(`${p.fullName} (${p.code})`)
      } catch {
        // Tên bệnh nhân chỉ để hiển thị tiêu đề; lỗi không chặn danh sách.
      }
    })()
  }, [id])

  const load = useCallback(async () => {
    if (!id) return
    setLoading(true)
    setError(null)
    try {
      const result = await listEncounters({ page, pageSize: PAGE_SIZE, patientId: id })
      setData(result)
    } catch (err) {
      setError(toApiException(err).message)
    } finally {
      setLoading(false)
    }
  }, [id, page])

  useEffect(() => { void load() }, [load])

  return (
    <section>
      <div className="page-head">
        <h1>Lịch sử khám {patientName && `— ${patientName}`}</h1>
        <Link className="btn" to="/patients">← Bệnh nhân</Link>
      </div>

      {error && <p className="alert alert--error">{error}</p>}
      {loading && <p>Đang tải…</p>}

      {data && (
        <>
          <table className="table">
            <thead>
              <tr>
                <th>Thời gian</th>
                <th>Bác sĩ</th>
                <th>Chẩn đoán</th>
                <th>Trạng thái</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 && (
                <tr><td colSpan={5} className="table__empty">Bệnh nhân chưa có phiếu khám nào.</td></tr>
              )}
              {data.items.map((e) => (
                <Fragment key={e.id}>
                  <tr>
                    <td>{formatDate(e.createdAt)}</td>
                    <td>{e.doctorName ?? '—'}</td>
                    <td>{e.diagnosis}</td>
                    <td>
                      <span className={`badge ${encounterStatusClass[e.status]}`}>
                        {encounterStatusLabels[e.status]}
                      </span>
                    </td>
                    <td className="table__actions">
                      <button className="link-btn"
                        onClick={() => setExpanded((cur) => (cur === e.id ? null : e.id))}>
                        {expanded === e.id ? 'Ẩn' : 'Chi tiết'}
                      </button>
                    </td>
                  </tr>
                  {expanded === e.id && (
                    <tr>
                      <td colSpan={5}>
                        <div className="detail">
                          {e.symptoms && <p><strong>Triệu chứng:</strong> {e.symptoms}</p>}
                          {e.notes && <p><strong>Chỉ định/Ghi chú:</strong> {e.notes}</p>}
                          <p><strong>Đơn thuốc:</strong></p>
                          {e.prescriptionItems.length === 0 ? (
                            <p className="muted">Không kê đơn.</p>
                          ) : (
                            <ul>
                              {e.prescriptionItems.map((it, i) => (
                                <li key={i}>
                                  {it.drugName} — {it.dosage} × {it.quantity}
                                  {it.instruction ? ` (${it.instruction})` : ''}
                                </li>
                              ))}
                            </ul>
                          )}
                        </div>
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
          </table>

          <div className="pager">
            <button className="btn" disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>
              ← Trước
            </button>
            <span>Trang {data.page}/{Math.max(data.totalPages, 1)} · {data.totalCount} bản ghi</span>
            <button className="btn" disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>
              Sau →
            </button>
          </div>
        </>
      )}
    </section>
  )
}
