import { Fragment, useCallback, useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { listEncounters } from '../services/encounterService'
import { getPatient } from '../services/patientService'
import { summarizePatient, askPatient } from '../services/aiService'
import { toApiException } from '../services/apiClient'
import { useAuth } from '../store/auth'
import { type FormEvent } from 'react'
import {
  encounterStatusClass,
  encounterStatusLabels,
  type Encounter,
} from '../types/encounter'
import type { PatientSummary, PatientAnswer } from '../types/ai'
import type { PagedResult } from '../types/common'

const PAGE_SIZE = 10

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

export default function PatientEncountersPage() {
  const { id } = useParams<{ id: string }>()
  const { canRecordEncounter } = useAuth()
  const [patientName, setPatientName] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Encounter> | null>(null)
  const [expanded, setExpanded] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [summary, setSummary] = useState<PatientSummary | null>(null)
  const [summarizing, setSummarizing] = useState(false)
  const [summaryError, setSummaryError] = useState<string | null>(null)

  const summarize = useCallback(async () => {
    if (!id) return
    setSummarizing(true)
    setSummaryError(null)
    setSummary(null)
    try {
      setSummary(await summarizePatient(id))
    } catch (err) {
      setSummaryError(toApiException(err).message)
    } finally {
      setSummarizing(false)
    }
  }, [id])

  // Hỏi đáp có ngữ cảnh (RAG).
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState<PatientAnswer | null>(null)
  const [asking, setAsking] = useState(false)
  const [askError, setAskError] = useState<string | null>(null)

  const ask = useCallback(async (e: FormEvent) => {
    e.preventDefault()
    if (!id || !question.trim()) return
    setAsking(true)
    setAskError(null)
    setAnswer(null)
    try {
      setAnswer(await askPatient(id, question.trim()))
    } catch (err) {
      setAskError(toApiException(err).message)
    } finally {
      setAsking(false)
    }
  }, [id, question])

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
        <div className="page-head__actions">
          {canRecordEncounter && (
            <button className="btn btn--primary" disabled={summarizing} onClick={() => void summarize()}>
              {summarizing ? 'Đang tóm tắt…' : '✨ Tóm tắt bằng AI'}
            </button>
          )}
          <Link className="btn" to="/patients">← Bệnh nhân</Link>
        </div>
      </div>

      {canRecordEncounter && (summarizing || summaryError || summary) && (
        <div className="ai-summary">
          <h2 className="ai-summary__title">Tóm tắt bằng AI</h2>
          {summarizing && <p>Đang phân tích lịch sử khám…</p>}
          {summaryError && <p className="alert alert--error">{summaryError}</p>}
          {summary && (
            <>
              <p className="ai-summary__text">{summary.summary}</p>
              <p className="muted ai-summary__meta">
                Dựa trên {summary.encounterCount} phiếu khám gần nhất · model: {summary.model}.
                Thông tin tham khảo, không thay thế đánh giá của bác sĩ.
              </p>
            </>
          )}
        </div>
      )}

      {canRecordEncounter && (
        <div className="ai-summary">
          <h2 className="ai-summary__title">Hỏi đáp trên bệnh án (RAG)</h2>
          <form className="ai-ask" onSubmit={(e) => void ask(e)}>
            <input
              className="input ai-ask__input"
              placeholder="Đặt câu hỏi tự do, ví dụ: Bệnh nhân từng dùng kháng sinh gì?"
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
            />
            <button className="btn btn--primary" type="submit" disabled={asking || !question.trim()}>
              {asking ? 'Đang hỏi…' : 'Hỏi AI'}
            </button>
          </form>
          {asking && <p>Đang truy hồi phiếu liên quan và tổng hợp câu trả lời…</p>}
          {askError && <p className="alert alert--error">{askError}</p>}
          {answer && (
            <>
              <p className="ai-summary__text">{answer.answer}</p>
              {answer.sources.length > 0 && (
                <div className="ai-ask__sources">
                  <p className="muted">Nguồn được trích ({answer.sources.length} phiếu):</p>
                  <ul>
                    {answer.sources.map((s, i) => (
                      <li key={s.encounterId}>
                        Nguồn {i + 1}: {formatDate(s.createdAt)} — {s.diagnosis}{' '}
                        <span className="muted">(tương đồng {(s.similarity * 100).toFixed(0)}%)</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}
              <p className="muted ai-summary__meta">
                model: {answer.model}. Thông tin tham khảo, không thay thế đánh giá của bác sĩ.
              </p>
            </>
          )}
        </div>
      )}

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
