import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react'
import { Bot, Send, Wrench } from 'lucide-react'
import { chatWithAssistant } from '../services/assistantService'
import { toastError } from '../lib/toast'
import type { AssistantMessage, AssistantToolCall } from '../types/assistant'
import { PageHeader } from '../components/PageHeader'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

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
  const listRef = useRef<HTMLDivElement>(null)

  // Cuộn xuống cuối mỗi khi có lượt mới.
  useEffect(() => {
    listRef.current?.scrollTo({ top: listRef.current.scrollHeight, behavior: 'smooth' })
  }, [turns, loading])

  const send = useCallback(
    async (text: string) => {
      const question = text.trim()
      if (!question || loading) return

      setInput('')
      const nextTurns: Turn[] = [...turns, { role: 'user', content: question }]
      setTurns(nextTurns)
      setLoading(true)

      try {
        const history: AssistantMessage[] = nextTurns.map((t) => ({ role: t.role, content: t.content }))
        const reply = await chatWithAssistant(history)
        setTurns((cur) => [
          ...cur,
          { role: 'assistant', content: reply.answer, toolCalls: reply.toolCalls, model: reply.model },
        ])
      } catch (err) {
        toastError(err)
      } finally {
        setLoading(false)
      }
    },
    [turns, loading],
  )

  const onSubmit = (e: FormEvent) => {
    e.preventDefault()
    void send(input)
  }

  return (
    <section className="flex h-[calc(100vh-8rem)] flex-col">
      <PageHeader
        title="Trợ lý ảo"
        description="Hỏi đáp về bệnh nhân, lịch khám, bác sĩ và lịch sử khám. Trợ lý tự truy vấn dữ liệu theo quyền của bạn — thông tin cần kiểm chứng, không thay thế đánh giá chuyên môn."
      />

      <div
        ref={listRef}
        className="flex-1 space-y-4 overflow-y-auto rounded-xl border bg-muted/30 p-4"
      >
        {turns.length === 0 && !loading && (
          <div className="flex h-full flex-col items-center justify-center gap-4 text-center">
            <div className="flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary">
              <Bot className="size-6" />
            </div>
            <p className="text-muted-foreground">Bắt đầu bằng một câu hỏi, ví dụ:</p>
            <div className="flex flex-wrap justify-center gap-2">
              {suggestions.map((s) => (
                <Button key={s} type="button" variant="outline" size="sm" onClick={() => void send(s)}>
                  {s}
                </Button>
              ))}
            </div>
          </div>
        )}

        {turns.map((t, i) => (
          <div
            key={i}
            className={cn('flex flex-col gap-1', t.role === 'user' ? 'items-end' : 'items-start')}
          >
            <div
              className={cn(
                'max-w-[80%] whitespace-pre-wrap rounded-2xl px-4 py-2.5 text-sm leading-relaxed',
                t.role === 'user'
                  ? 'rounded-br-sm bg-primary text-primary-foreground'
                  : 'rounded-bl-sm border bg-card',
              )}
            >
              {t.content}
            </div>
            {t.toolCalls && t.toolCalls.length > 0 && (
              <div className="flex flex-wrap items-center gap-1 text-xs text-muted-foreground">
                <Wrench className="size-3" />
                Đã tra cứu:
                {t.toolCalls.map((c, j) => (
                  <Badge key={j} variant="secondary" className="font-normal">
                    {toolLabels[c.name] ?? c.name}
                  </Badge>
                ))}
              </div>
            )}
          </div>
        ))}

        {loading && (
          <div className="flex items-start">
            <div className="rounded-2xl rounded-bl-sm border bg-card px-4 py-2.5 text-sm italic text-muted-foreground">
              Đang xử lý…
            </div>
          </div>
        )}
      </div>

      <form className="mt-3 flex gap-2" onSubmit={onSubmit}>
        <Input
          placeholder="Nhập câu hỏi của bạn…"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          disabled={loading}
        />
        <Button type="submit" disabled={loading || !input.trim()}>
          <Send className="size-4" />
          Gửi
        </Button>
      </form>
    </section>
  )
}
