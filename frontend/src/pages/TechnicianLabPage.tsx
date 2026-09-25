import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Activity, Clock, FlaskConical, PlayCircle, RefreshCw } from 'lucide-react'
import { listLabOrders } from '../services/labOrderService'
import { toastError } from '../lib/toast'
import { useAutoRefresh } from '../hooks/useAutoRefresh'
import { LabOrderStatus, type LabOrder } from '../types/labOrder'
import { LabOrderStatusBadge } from '../components/StatusBadge'
import { PageHeader } from '../components/PageHeader'
import { PatientContextHeader } from '../components/PatientContextHeader'
import { Pager } from '../components/Pager'
import { WorkspaceSummaryBar } from '../components/workspace/WorkspaceSummaryBar'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'

const PAGE_SIZE = 10

/**
 * Màn "Thực hiện cận lâm sàng" cho Kỹ thuật viên (ADR 0016): danh sách các phiếu chỉ định còn
 * ở trạng thái Đã chỉ định / Đang thực hiện (gồm cả walk-in lẫn phiếu do bác sĩ chỉ định).
 * Bấm "Thực hiện" chuyển sang màn riêng (`LabOrderExecutePage`) để nhập kết quả cho từng bệnh nhân.
 */
export default function TechnicianLabPage() {
  const [orders, setOrders] = useState<LabOrder[]>([])
  const [loading, setLoading] = useState(true)
  const [page, setPage] = useState(1)

  const load = useCallback(async (opts?: { silent?: boolean }) => {
    if (!opts?.silent) setLoading(true)
    try {
      // Hàng chờ = Ordered + InProgress (backend lọc theo một trạng thái nên gọi hai lần rồi gộp).
      const [ordered, inProgress] = await Promise.all([
        listLabOrders({ page: 1, pageSize: 100, status: LabOrderStatus.Ordered }),
        listLabOrders({ page: 1, pageSize: 100, status: LabOrderStatus.InProgress }),
      ])
      const merged = [...ordered.items, ...inProgress.items].sort((a, b) =>
        a.createdAt < b.createdAt ? -1 : 1,
      )
      setOrders(merged)
    } catch (err) {
      if (!opts?.silent) toastError(err)
    } finally {
      if (!opts?.silent) setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  // Bác sĩ chỉ định CLS ở màn khác — tự làm mới hàng chờ thực hiện.
  useAutoRefresh(() => void load({ silent: true }))

  const totalPages = Math.max(1, Math.ceil(orders.length / PAGE_SIZE))
  useEffect(() => {
    if (page > totalPages) setPage(totalPages)
  }, [page, totalPages])
  const pageItems = orders.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE)

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title="Thực hiện cận lâm sàng"
        description="Danh sách phiếu chỉ định chờ thực hiện — bấm Thực hiện để nhập kết quả cho từng bệnh nhân."
        actions={
          <Button variant="outline" onClick={() => void load()} disabled={loading}>
            <RefreshCw className={loading ? 'size-4 animate-spin' : 'size-4'} />
            Làm mới
          </Button>
        }
      />

      <WorkspaceSummaryBar
        items={[
          {
            icon: Clock,
            label: 'Chờ thực hiện',
            value: String(orders.filter((o) => o.status === LabOrderStatus.Ordered).length),
          },
          {
            icon: Activity,
            label: 'Đang thực hiện',
            value: String(orders.filter((o) => o.status === LabOrderStatus.InProgress).length),
          },
        ]}
      />

      {loading ? (
        <p className="text-muted-foreground">Đang tải…</p>
      ) : orders.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-10 text-muted-foreground">
            <FlaskConical className="size-8" />
            <p>Không có phiếu chỉ định nào đang chờ thực hiện.</p>
          </CardContent>
        </Card>
      ) : (
        <div className="flex flex-col gap-2">
          {pageItems.map((o) => (
            <div
              key={o.id}
              className="flex flex-wrap items-center justify-between gap-2 rounded-lg border bg-card p-3"
            >
              <div className="flex flex-wrap items-center gap-3">
                <PatientContextHeader patientId={o.patientId} fallbackName={o.patientName} variant="inline" />
                <span className="font-mono text-xs text-muted-foreground">{o.code}</span>
                <LabOrderStatusBadge status={o.status} />
                <div className="text-sm text-muted-foreground">
                  {o.doctorName && <>BS chỉ định: {o.doctorName}</>}
                  {!o.encounterId && (
                    <>
                      {o.doctorName && ' · '}
                      <span className="text-primary">Walk-in</span>
                    </>
                  )}
                </div>
              </div>
              <Button asChild size="sm">
                <Link to={`/lab/technician/${o.id}`}>
                  <PlayCircle className="size-4" />
                  Thực hiện
                </Link>
              </Button>
            </div>
          ))}
        </div>
      )}

      <Pager page={page} totalPages={totalPages} totalCount={orders.length} onPageChange={setPage} />
    </section>
  )
}
