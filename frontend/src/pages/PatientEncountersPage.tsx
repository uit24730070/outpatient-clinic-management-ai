import { Fragment, useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, Sparkles, ChevronDown, ChevronRight, Loader2, Receipt } from 'lucide-react'
import { listEncounters } from '../services/encounterService'
import { getPatient } from '../services/patientService'
import { summarizePatient, askPatient } from '../services/aiService'
import { createInvoiceFromEncounter } from '../services/invoiceService'
import { useAuth } from '../store/auth'
import { canManageBilling } from '../config/access'
import { toastError, toastSuccess } from '../lib/toast'
import { EncounterStatus, type Encounter } from '../types/encounter'
import type { PatientSummary, PatientAnswer } from '../types/ai'
import type { PagedResult } from '../types/common'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { EncounterStatusBadge } from '../components/StatusBadge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const PAGE_SIZE = 10

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function PatientEncountersPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user, canRecordEncounter } = useAuth()
  const canBilling = canManageBilling(user?.role)
  const [creatingInvoiceFor, setCreatingInvoiceFor] = useState<string | null>(null)
  const [patientName, setPatientName] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Encounter> | null>(null)
  const [expanded, setExpanded] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)

  const [summary, setSummary] = useState<PatientSummary | null>(null)
  const [summarizing, setSummarizing] = useState(false)

  const summarize = useCallback(async () => {
    if (!id) return
    setSummarizing(true)
    setSummary(null)
    try {
      setSummary(await summarizePatient(id))
    } catch (err) {
      toastError(err)
    } finally {
      setSummarizing(false)
    }
  }, [id])

  // Hỏi đáp có ngữ cảnh (RAG).
  const [question, setQuestion] = useState('')
  const [answer, setAnswer] = useState<PatientAnswer | null>(null)
  const [asking, setAsking] = useState(false)

  const ask = useCallback(
    async (e: FormEvent) => {
      e.preventDefault()
      if (!id || !question.trim()) return
      setAsking(true)
      setAnswer(null)
      try {
        setAnswer(await askPatient(id, question.trim()))
      } catch (err) {
        toastError(err)
      } finally {
        setAsking(false)
      }
    },
    [id, question],
  )

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
    try {
      const result = await listEncounters({ page, pageSize: PAGE_SIZE, patientId: id })
      setData(result)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [id, page])

  useEffect(() => {
    void load()
  }, [load])

  const createInvoice = async (encounterId: string) => {
    setCreatingInvoiceFor(encounterId)
    try {
      const inv = await createInvoiceFromEncounter(encounterId)
      toastSuccess('Đã lập hoá đơn thuốc từ phiếu khám.')
      navigate(`/invoices/${inv.id}`)
    } catch (err) {
      // Đã lập HĐ thuốc cho phiếu này (409) hoặc lỗi khác — hiển thị thông báo server.
      toastError(err)
    } finally {
      setCreatingInvoiceFor(null)
    }
  }

  return (
    <section>
      <PageHeader
        title={patientName ? `Lịch sử khám — ${patientName}` : 'Lịch sử khám'}
        actions={
          <div className="flex items-center gap-2">
            {canRecordEncounter && (
              <Button disabled={summarizing} onClick={() => void summarize()}>
                {summarizing ? <Loader2 className="size-4 animate-spin" /> : <Sparkles className="size-4" />}
                {summarizing ? 'Đang tóm tắt…' : 'Tóm tắt bằng AI'}
              </Button>
            )}
            <Button asChild variant="outline">
              <Link to="/patients">
                <ArrowLeft className="size-4" />
                Bệnh nhân
              </Link>
            </Button>
          </div>
        }
      />

      {canRecordEncounter && (summarizing || summary) && (
        <Card className="mb-4 border-l-4 border-l-primary">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Sparkles className="size-4 text-primary" />
              Tóm tắt bằng AI
            </CardTitle>
          </CardHeader>
          <CardContent>
            {summarizing && <p className="text-muted-foreground">Đang phân tích lịch sử khám…</p>}
            {summary && (
              <>
                <p className="whitespace-pre-wrap">{summary.summary}</p>
                <p className="mt-2 text-sm text-muted-foreground">
                  Dựa trên {summary.encounterCount} phiếu khám gần nhất · model: {summary.model}. Thông
                  tin tham khảo, không thay thế đánh giá của bác sĩ.
                </p>
              </>
            )}
          </CardContent>
        </Card>
      )}

      {canRecordEncounter && (
        <Card className="mb-4">
          <CardHeader>
            <CardTitle className="text-base">Hỏi đáp trên bệnh án (RAG)</CardTitle>
          </CardHeader>
          <CardContent>
            <form className="flex gap-2" onSubmit={(e) => void ask(e)}>
              <Input
                placeholder="Đặt câu hỏi tự do, ví dụ: Bệnh nhân từng dùng kháng sinh gì?"
                value={question}
                onChange={(e) => setQuestion(e.target.value)}
              />
              <Button type="submit" disabled={asking || !question.trim()}>
                {asking ? <Loader2 className="size-4 animate-spin" /> : null}
                {asking ? 'Đang hỏi…' : 'Hỏi AI'}
              </Button>
            </form>
            {asking && (
              <p className="mt-3 text-muted-foreground">
                Đang truy hồi phiếu liên quan và tổng hợp câu trả lời…
              </p>
            )}
            {answer && (
              <div className="mt-3">
                <p className="whitespace-pre-wrap">{answer.answer}</p>
                {answer.sources.length > 0 && (
                  <div className="mt-3 rounded-md bg-muted/50 p-3 text-sm">
                    <p className="font-medium">Nguồn được trích ({answer.sources.length} phiếu):</p>
                    <ul className="mt-1 list-disc space-y-1 pl-5">
                      {answer.sources.map((s, i) => (
                        <li key={s.encounterId}>
                          Nguồn {i + 1}: {formatDate(s.createdAt)} — {s.diagnosis}{' '}
                          <span className="text-muted-foreground">
                            (tương đồng {(s.similarity * 100).toFixed(0)}%)
                          </span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
                <p className="mt-2 text-sm text-muted-foreground">
                  model: {answer.model}. Thông tin tham khảo, không thay thế đánh giá của bác sĩ.
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Thời gian</TableHead>
                <TableHead>Bác sĩ</TableHead>
                <TableHead>Chẩn đoán</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    Bệnh nhân chưa có phiếu khám nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((e) => (
                  <Fragment key={e.id}>
                    <TableRow>
                      <TableCell className="whitespace-nowrap">{formatDate(e.createdAt)}</TableCell>
                      <TableCell>{e.doctorName ?? '—'}</TableCell>
                      <TableCell className="font-medium">{e.diagnosis}</TableCell>
                      <TableCell>
                        <EncounterStatusBadge status={e.status} />
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex items-center justify-end gap-1">
                          {canBilling && e.status === EncounterStatus.Completed && (
                            <Button
                              size="sm"
                              variant="ghost"
                              disabled={creatingInvoiceFor === e.id}
                              onClick={() => void createInvoice(e.id)}
                            >
                              {creatingInvoiceFor === e.id ? (
                                <Loader2 className="size-4 animate-spin" />
                              ) : (
                                <Receipt className="size-4" />
                              )}
                              Lập HĐ thuốc
                            </Button>
                          )}
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => setExpanded((cur) => (cur === e.id ? null : e.id))}
                          >
                            {expanded === e.id ? (
                              <ChevronDown className="size-4" />
                            ) : (
                              <ChevronRight className="size-4" />
                            )}
                            {expanded === e.id ? 'Ẩn' : 'Chi tiết'}
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                    {expanded === e.id && (
                      <TableRow>
                        <TableCell colSpan={5} className="bg-muted/30">
                          <div className="space-y-2 py-1">
                            {e.symptoms && (
                              <p>
                                <strong>Triệu chứng:</strong> {e.symptoms}
                              </p>
                            )}
                            {e.notes && (
                              <p>
                                <strong>Chỉ định/Ghi chú:</strong> {e.notes}
                              </p>
                            )}
                            <p className="font-semibold">Đơn thuốc:</p>
                            {e.prescriptionItems.length === 0 ? (
                              <p className="text-muted-foreground">Không kê đơn.</p>
                            ) : (
                              <ul className="list-disc space-y-1 pl-5">
                                {e.prescriptionItems.map((it, i) => (
                                  <li key={i}>
                                    {it.drugName} — {it.dosage} × {it.quantity}
                                    {it.instruction ? ` (${it.instruction})` : ''}
                                  </li>
                                ))}
                              </ul>
                            )}
                          </div>
                        </TableCell>
                      </TableRow>
                    )}
                  </Fragment>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {data && (
        <Pager
          page={data.page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          onPageChange={setPage}
        />
      )}
    </section>
  )
}
