import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { ChevronDown, ChevronRight, History } from 'lucide-react'
import { listEncounters } from '../services/encounterService'
import type { Encounter } from '../types/encounter'
import { EncounterStatusBadge } from './StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'

const RECENT_COUNT = 5

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

/**
 * Vài lần khám gần nhất của bệnh nhân (chẩn đoán/đơn thuốc), thu gọn ngay trong màn khám hiện tại —
 * bác sĩ không cần rời tab để mở "Lịch sử khám" mới nắm được tiền sử. Loại trừ chính phiếu đang mở
 * (`excludeEncounterId`) vì phiếu đó đã hiển thị đầy đủ ở form bên cạnh.
 */
export function RecentEncountersCard({
  patientId,
  excludeEncounterId,
  bare = false,
}: {
  patientId: string
  excludeEncounterId?: string
  /** Bỏ khung `<Card>` bọc ngoài — dùng khi ghép chung vào một Card khác (vd với PatientContextHeader). */
  bare?: boolean
}) {
  const [items, setItems] = useState<Encounter[]>([])
  const [loaded, setLoaded] = useState(false)
  const [expanded, setExpanded] = useState<string | null>(null)

  useEffect(() => {
    if (!patientId) return
    let active = true
    setLoaded(false)
    void (async () => {
      try {
        const result = await listEncounters({ page: 1, pageSize: RECENT_COUNT + 1, patientId })
        if (active) setItems(result.items.filter((e) => e.id !== excludeEncounterId).slice(0, RECENT_COUNT))
      } catch {
        // Chỉ là ngữ cảnh tham khảo — lỗi tải không chặn màn khám.
      } finally {
        if (active) setLoaded(true)
      }
    })()
    return () => {
      active = false
    }
  }, [patientId, excludeEncounterId])

  if (!loaded) return null

  const body = (
    <>
      <div className="flex items-center gap-2 text-sm font-medium">
        <History className="size-4 text-primary" />
        Lịch sử khám gần đây
      </div>
      {items.length === 0 ? (
          <p className="text-sm text-muted-foreground">Chưa có lần khám nào trước đó.</p>
        ) : (
          <ul className="flex flex-col divide-y">
            {items.map((e) => (
              <li key={e.id} className="py-2 first:pt-0 last:pb-0">
                <button
                  type="button"
                  className="flex w-full items-start gap-1.5 text-left"
                  onClick={() => setExpanded((cur) => (cur === e.id ? null : e.id))}
                >
                  {expanded === e.id ? (
                    <ChevronDown className="mt-0.5 size-3.5 shrink-0 text-muted-foreground" />
                  ) : (
                    <ChevronRight className="mt-0.5 size-3.5 shrink-0 text-muted-foreground" />
                  )}
                  <span className="flex-1">
                    <span className="block text-sm font-medium">{e.diagnosis}</span>
                    <span className="block text-xs text-muted-foreground">
                      {formatDate(e.createdAt)}
                      {e.doctorName ? ` · ${e.doctorName}` : ''}
                    </span>
                  </span>
                  <EncounterStatusBadge status={e.status} />
                </button>
                {expanded === e.id && (
                  <div className="mt-2 space-y-1 pl-5 text-xs text-muted-foreground">
                    {e.symptoms && (
                      <p>
                        <strong className="text-foreground">Triệu chứng:</strong> {e.symptoms}
                      </p>
                    )}
                    {e.notes && (
                      <p>
                        <strong className="text-foreground">Ghi chú:</strong> {e.notes}
                      </p>
                    )}
                    {e.prescriptionItems.length > 0 && (
                      <p>
                        <strong className="text-foreground">Đơn thuốc:</strong>{' '}
                        {e.prescriptionItems.map((it) => it.drugName).join(', ')}
                      </p>
                    )}
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      <Button asChild size="sm" variant="ghost" className="self-start px-0 text-muted-foreground">
        <Link to={`/patients/${patientId}/encounters`}>Xem đầy đủ lịch sử khám →</Link>
      </Button>
    </>
  )

  if (bare) return <div className="flex flex-col gap-2">{body}</div>

  return (
    <Card>
      <CardContent className="flex flex-col gap-2">{body}</CardContent>
    </Card>
  )
}
