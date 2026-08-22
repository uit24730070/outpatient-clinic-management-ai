import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { chatWithAssistant } from '../services/assistantService'
import { toApiException } from '../services/apiClient'
import type { AssistantMessage, AssistantToolCall } from '../types/assistant'

// Nhãn thân thiện cho các công cụ đã gọi (hiển thị truy vết).
const toolLabels: Record<string, string> = {
  search_patients: 'Tìm bệnh nhân',
  list_doctors: 'Tra cứu bác sĩ',
  list_appointments: 'Xem lịch khám',
  get_patient_encounters: 'Lịch sử khám bệnh nhân',
}

const suggestions = [
  'Hôm nay có bao nhiêu lịch khám đang chờ?',
  'Tìm bệnh nhân tên Nguyễn',
  'Bác sĩ nào thuộc chuyên khoa Nội?',
]

interface Turn extends AssistantMessage {
  toolCalls?: AssistantToolCall[]
  model?: string
}

export default function AssistantPage() {
  const [turns, setTurns] = useState<Turn[]>([])
  const [input, setInput] = useState('')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const listRef = useRef<HTMLDivElement>(null)

  // Cuộn xuống cuối mỗi khi có lượt mới.
  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: 'smooth' })
  }, [turns, loading])

  const send = useCallback(async (text: string) => {
    const question = text.trim()
    if (!question || loading) return

    setError(null)
    setInput('')
    const nextTurns: Turn[] = [...turns, { role: 'user', content: question }]
    setTurns(nextTurns)
    setLoading(true)

    try {
      // Gửi toàn bộ lịch sử (chỉ role + content) để trợ lý có ngữ cảnh multi-turn.
      const history: AssistantMessage[] = nextTurns.map((t) => ({ role: t.role, content: t.content }))
      const reply = await chatWithAssistant(history)
      setTurns((cur) => [
        ...cur,
        { role: 'assistant', content: reply.answer, toolCalls: reply.toolCalls, model: reply.model },
      ])
    } catch (err) {
      setError(toApiException(err).message)
    } finally {
      setLoading(false)
    }
  }, [turns, loading])

  const onSubmit = (e: FormEvent) => {
    e.preventDefault()
    void send(input)
  }

  return (
    <section className="assistant">
      <div className="page-head">
        <h1>Trợ lý ảo</h1>
      </div>
      <p className="muted">
        Hỏi đáp về bệnh nhân, lịch khám, bác sĩ và lịch sử khám. Trợ lý tự truy vấn dữ liệu trong hệ
        thống theo quyền của bạn. Đây là thông tin hỗ trợ, cần kiểm chứng — không thay thế đánh giá chuyên môn.
      </p>

      <div className="assistant__chat" ref={listRef}>
        {turns.length === 0 && !loading && (
          <div className="assistant__empty">
            <p className="muted">Bắt đầu bằng một câu hỏi, ví dụ:</p>
            <div className="assistant__suggestions">
              {suggestions.map((s) => (
                <button key={s} type="button" className="chip" onClick={() => void send(s)}>
                  {s}
                </button>
              ))}
            </div>
          </div>
        )}

        {turns.map((t, i) => (
          <div key={i} className={`msg msg--${t.role}`}>
            <div className="msg__bubble">{t.content}</div>
            {t.toolCalls && t.toolCalls.length > 0 && (
              <div className="msg__tools muted">
                Đã tra cứu:{' '}
                {t.toolCalls.map((c, j) => (
                  <span key={j} className="chip chip--sm">{toolLabels[c.name] ?? c.name}</span>
                ))}
              </div>
            )}
          </div>
        ))}

        {loading && (
          <div className="msg msg--assistant">
            <div className="msg__bubble msg__bubble--loading">Đang xử lý…</div>
          </div>
        )}
      </div>

      {error && <p className="alert alert--error">{error}</p>}

      <form className="assistant__form" onSubmit={onSubmit}>
        <input
          className="input assistant__input"
          placeholder="Nhập câu hỏi của bạn…"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          disabled={loading}
        />
        <button className="btn btn--primary" type="submit" disabled={loading || !input.trim()}>
          Gửi
        </button>
      </form>
    </section>
  )
}
