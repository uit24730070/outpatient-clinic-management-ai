import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { PackageCheck, PackagePlus, RefreshCw, TriangleAlert } from 'lucide-react'
import { dispenseEncounter, listEncounters, returnStock, type ReturnStockItem } from '../services/encounterService'
import { getPharmacyAlerts } from '../services/pharmacyService'
import { toastError, toastSuccess } from '../lib/toast'
import { useAutoRefresh } from '../hooks/useAutoRefresh'
import { DispenseStatus, type Encounter } from '../types/encounter'
import type { PharmacyAlerts } from '../types/medication'
import { PageHeader } from '../components/PageHeader'
import { EncounterDispenseCard } from '../components/EncounterDispenseCard'
import { TonedBadge } from '../components/StatusBadge'
import { WorkspaceSummaryBar } from '../components/workspace/WorkspaceSummaryBar'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'

const ALERT_PREVIEW = 5

/**
 * Workspace Dược sĩ (Epic 17, UX-05): gộp đơn thuốc đã thanh toán chờ cấp phát + thao tác cấp phát
 * tại chỗ + cảnh báo tồn thấp/sắp hết hạn trên 1 màn — thay việc phải mở `/pharmacy/dispense` +
 * `/pharmacy/alerts` rời nhau. Hai trang cũ vẫn giữ nguyên, đây là bổ sung (cùng pattern UX-03/04).
 */
export default function PharmacyWorkspacePage() {
  const [pendingOrders, setPendingOrders] = useState<Encounter[]>([])
  const [dispensedOrders, setDispensedOrders] = useState<Encounter[]>([])
  const [alerts, setAlerts] = useState<PharmacyAlerts | null>(null)
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async (opts?: { silent?: boolean }) => {
    if (!opts?.silent) setLoading(true)
    try {
      const [pendingRes, dispensedRes, alertsRes] = await Promise.all([
        listEncounters({ page: 1, pageSize: 100, dispenseStatus: DispenseStatus.Paid }),
        listEncounters({ page: 1, pageSize: 50, dispenseStatus: DispenseStatus.Dispensed }),
        getPharmacyAlerts(),
      ])
      setPendingOrders(pendingRes.items)
      setDispensedOrders(dispensedRes.items)
      setAlerts(alertsRes)
    } catch (err) {
      if (!opts?.silent) toastError(err)
    } finally {
      if (!opts?.silent) setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  // Bác sĩ chỉ định/lễ tân thu tiền ở màn khác — tự làm mới đơn chờ cấp phát.
  useAutoRefresh(() => void load({ silent: true }))

  const dispense = async (id: string) => {
    setBusyId(id)
    try {
      await dispenseEncounter(id)
      toastSuccess('Đã cấp phát thuốc và trừ tồn kho.')
      await load()
    } catch (err) {
      toastError(err)
    } finally {
      setBusyId(null)
    }
  }

  const doReturnStock = async (id: string, reason: string, items: ReturnStockItem[]) => {
    setBusyId(id)
    try {
      await returnStock(id, reason, items)
      toastSuccess('Đã hoàn kho — tồn kho được khôi phục về đúng lô.')
      await load()
    } catch (err) {
      toastError(err)
    } finally {
      setBusyId(null)
    }
  }

  const alertCount = alerts ? alerts.lowStock.length + alerts.expiringBatches.length : 0

  return (
    <section className="flex flex-col gap-6">
      <PageHeader
        title="Cấp phát & Cảnh báo kho"
        description="Chờ cấp phát, hoàn kho & cảnh báo tồn kho trong ca — workspace theo vai trò (UX-05)"
        actions={
          <Button variant="outline" onClick={() => void load()} disabled={loading}>
            <RefreshCw className={loading ? 'size-4 animate-spin' : 'size-4'} />
            Làm mới
          </Button>
        }
      />

      <WorkspaceSummaryBar
        items={[
          { icon: PackageCheck, label: 'Chờ cấp phát', value: String(pendingOrders.length) },
          {
            icon: TriangleAlert,
            label: 'Cảnh báo kho',
            value: String(alertCount),
            tone: alertCount > 0 ? 'warning' : 'default',
          },
          { icon: PackagePlus, label: 'Có thể hoàn kho', value: String(dispensedOrders.length) },
        ]}
      />

      {loading ? (
        <p className="text-muted-foreground">Đang tải…</p>
      ) : (
        <>
          {/* Cảnh báo kho — rút gọn, xem đầy đủ ở /pharmacy/alerts */}
          {alerts && alertCount > 0 && (
            <Card>
              <CardContent className="flex flex-col gap-3 p-4">
                <div className="flex items-center justify-between">
                  <h3 className="font-semibold">Cảnh báo kho</h3>
                  <Button asChild size="sm" variant="ghost">
                    <Link to="/pharmacy/alerts">Xem đầy đủ →</Link>
                  </Button>
                </div>
                <div className="flex flex-col gap-2">
                  {alerts.lowStock.slice(0, ALERT_PREVIEW).map((m) => (
                    <div key={m.medicationId} className="flex items-center justify-between text-sm">
                      <span>
                        <span className="font-medium">{m.name}</span>{' '}
                        <span className="text-muted-foreground">({m.code})</span>
                      </span>
                      <TonedBadge tone={m.stockOnHand <= 0 ? 'red' : 'amber'}>
                        Tồn {m.stockOnHand} {m.unit} / ngưỡng {m.reorderLevel}
                      </TonedBadge>
                    </div>
                  ))}
                  {alerts.expiringBatches.slice(0, ALERT_PREVIEW).map((b) => (
                    <div key={b.batchId} className="flex items-center justify-between text-sm">
                      <span>
                        <span className="font-medium">{b.medicationName}</span>{' '}
                        <span className="text-muted-foreground">
                          · lô {b.batchNumber} · HD {b.expiryDate}
                        </span>
                      </span>
                      <TonedBadge tone={b.isExpired ? 'red' : 'amber'}>
                        {b.isExpired ? 'Đã hết hạn' : 'Sắp hết hạn'}
                      </TonedBadge>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Chờ cấp phát */}
          <div className="flex flex-col gap-3">
            <h2 className="text-base font-semibold">Chờ cấp phát</h2>
            {pendingOrders.length === 0 ? (
              <Card>
                <CardContent className="flex flex-col items-center gap-2 py-10 text-muted-foreground">
                  <PackageCheck className="size-8" />
                  <p>Không có đơn thuốc nào đang chờ cấp phát.</p>
                </CardContent>
              </Card>
            ) : (
              pendingOrders.map((e) => (
                <EncounterDispenseCard
                  key={e.id}
                  encounter={e}
                  action="dispense"
                  busy={busyId === e.id}
                  onDispense={(id) => void dispense(id)}
                  onReturnStock={(id, reason, items) => void doReturnStock(id, reason, items)}
                />
              ))
            )}
          </div>

          {/* Đã cấp phát — có thể hoàn kho */}
          {dispensedOrders.length > 0 && (
            <div className="flex flex-col gap-3">
              <h2 className="text-base font-semibold text-muted-foreground">
                Đã cấp phát (có thể hoàn kho)
              </h2>
              {dispensedOrders.map((e) => (
                <EncounterDispenseCard
                  key={e.id}
                  encounter={e}
                  action="return"
                  busy={busyId === e.id}
                  onDispense={(id) => void dispense(id)}
                  onReturnStock={(id, reason, items) => void doReturnStock(id, reason, items)}
                />
              ))}
            </div>
          )}
        </>
      )}
    </section>
  )
}
