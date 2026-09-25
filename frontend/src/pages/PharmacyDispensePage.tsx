import { useCallback, useEffect, useState } from 'react'
import { PackageCheck } from 'lucide-react'
import { dispenseEncounter, listEncounters, returnStock, type ReturnStockItem } from '../services/encounterService'
import { toastError, toastSuccess } from '../lib/toast'
import { DispenseStatus, type Encounter } from '../types/encounter'
import { PageHeader } from '../components/PageHeader'
import { EncounterDispenseCard } from '../components/EncounterDispenseCard'
import { Card, CardContent } from '@/components/ui/card'

/**
 * Màn "Cấp phát thuốc" cho Dược sĩ (ADR 0021, PAY-02 + ADR 0022, REF-02):
 * - Hàng chờ cấp phát: DispenseStatus.Paid → xuất kho FEFO.
 * - Đã cấp phát: DispenseStatus.Dispensed → hoàn kho khi cần (nhập lại tồn đúng lô).
 */
export default function PharmacyDispensePage() {
  const [pendingOrders, setPendingOrders] = useState<Encounter[]>([])
  const [dispensedOrders, setDispensedOrders] = useState<Encounter[]>([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const [pendingRes, dispensedRes] = await Promise.all([
        listEncounters({ page: 1, pageSize: 100, dispenseStatus: DispenseStatus.Paid }),
        listEncounters({ page: 1, pageSize: 50, dispenseStatus: DispenseStatus.Dispensed }),
      ])
      setPendingOrders(pendingRes.items)
      setDispensedOrders(dispensedRes.items)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

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

  return (
    <section className="flex flex-col gap-6">
      <PageHeader
        title="Cấp phát thuốc"
        description="Hàng chờ các phiếu khám đã thu tiền thuốc, chờ cấp phát (trừ tồn FEFO)."
      />

      {loading ? (
        <p className="text-muted-foreground">Đang tải…</p>
      ) : (
        <>
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
