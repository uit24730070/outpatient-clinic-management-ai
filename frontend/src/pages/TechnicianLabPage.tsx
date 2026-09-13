import { useCallback, useEffect, useState } from 'react'
import { Activity, Clock, FlaskConical } from 'lucide-react'
import { listLabOrders } from '../services/labOrderService'
import { toastError } from '../lib/toast'
import { useAuth } from '../store/auth'
import { canRecordEncounter } from '../config/access'
import { LabOrderStatus, type LabOrder } from '../types/labOrder'
import { LabOrderCard } from '../components/LabOrderPanel'
import { PageHeader } from '../components/PageHeader'
import { PatientContextHeader } from '../components/PatientContextHeader'
import { WorkspaceSummaryBar } from '../components/workspace/WorkspaceSummaryBar'
import { Card, CardContent } from '@/components/ui/card'

/**
 * Màn "Thực hiện cận lâm sàng" cho Kỹ thuật viên (ADR 0016): hàng chờ các phiếu chỉ định còn
 * ở trạng thái Đã chỉ định / Đang thực hiện (gồm cả walk-in lẫn phiếu do bác sĩ chỉ định) để nhập kết quả.
 */
export default function TechnicianLabPage() {
  const { user } = useAuth()
  // Huỷ phiếu chỉ định khớp Roles.RecordEncounter (chỉ Admin/Bác sĩ) — Kỹ thuật viên chỉ nhập kết quả.
  const canCancel = canRecordEncounter(user?.role)
  const [orders, setOrders] = useState<LabOrder[]>([])
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
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
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title="Thực hiện cận lâm sàng"
        description="Hàng chờ các phiếu chỉ định cần thực hiện và nhập kết quả — một màn (UX-06)."
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
        <div className="flex flex-col gap-3">
          {orders.map((o) => (
            <div key={o.id} className="rounded-lg border bg-card p-3">
              <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
                <PatientContextHeader patientId={o.patientId} fallbackName={o.patientName} variant="inline" />
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
              <LabOrderCard order={o} canRecord canCancel={canCancel} onChanged={load} />
            </div>
          ))}
        </div>
      )}
    </section>
  )
}
