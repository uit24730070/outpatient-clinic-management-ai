import { useCallback, useEffect, useState } from 'react'
import { PackageCheck, Pill } from 'lucide-react'
import { dispenseEncounter, listEncounters } from '../services/encounterService'
import { toastError, toastSuccess } from '../lib/toast'
import { DispenseStatus, type Encounter } from '../types/encounter'
import { PageHeader } from '../components/PageHeader'
import { DispenseStatusBadge } from '../components/StatusBadge'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'

/**
 * Màn "Cấp phát thuốc" cho Dược sĩ (ADR 0021, PAY-02): hàng chờ các phiếu khám đã thu tiền thuốc
 * (DispenseStatus = Paid) để xuất kho thực theo FEFO. Chốt phiếu (bác sĩ) chỉ giữ tồn; thu tiền
 * (lễ tân) mở cổng; cấp phát ở đây mới trừ tồn vật lý.
 */
export default function PharmacyDispensePage() {
  const [orders, setOrders] = useState<Encounter[]>([])
  const [loading, setLoading] = useState(true)
  const [busyId, setBusyId] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      const res = await listEncounters({
        page: 1,
        pageSize: 100,
        dispenseStatus: DispenseStatus.Paid,
      })
      setOrders(res.items)
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

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title="Cấp phát thuốc"
        description="Hàng chờ các phiếu khám đã thu tiền thuốc, chờ cấp phát (trừ tồn FEFO)."
      />

      {loading ? (
        <p className="text-muted-foreground">Đang tải…</p>
      ) : orders.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-2 py-10 text-muted-foreground">
            <PackageCheck className="size-8" />
            <p>Không có đơn thuốc nào đang chờ cấp phát.</p>
          </CardContent>
        </Card>
      ) : (
        <div className="flex flex-col gap-3">
          {orders.map((e) => {
            const meds = e.prescriptionItems.filter((i) => i.medicationId != null)
            return (
              <div key={e.id} className="rounded-lg border bg-card p-4">
                <div className="mb-3 flex items-center justify-between gap-2">
                  <div className="text-sm text-muted-foreground">
                    Bệnh nhân:{' '}
                    <span className="font-medium text-foreground">{e.patientName ?? '—'}</span>
                    {e.doctorName && <> · BS: {e.doctorName}</>}
                  </div>
                  <DispenseStatusBadge status={e.dispenseStatus} />
                </div>

                <ul className="mb-3 flex flex-col gap-1 text-sm">
                  {meds.map((i, idx) => (
                    <li key={idx} className="flex items-center gap-2">
                      <Pill className="size-3.5 text-primary" />
                      <span className="font-medium">{i.drugName}</span>
                      <span className="text-muted-foreground">
                        · {i.dosage} · SL {i.quantity}
                      </span>
                    </li>
                  ))}
                </ul>

                <div className="flex justify-end">
                  <ConfirmDialog
                    trigger={
                      <Button size="sm" disabled={busyId === e.id}>
                        <PackageCheck className="size-4" />
                        Cấp phát
                      </Button>
                    }
                    title="Cấp phát thuốc?"
                    description={`Xuất kho theo FEFO cho đơn của ${e.patientName ?? 'bệnh nhân'}? Thao tác trừ tồn thực và không thể hoàn tác.`}
                    confirmText="Cấp phát"
                    onConfirm={() => void dispense(e.id)}
                  />
                </div>
              </div>
            )
          })}
        </div>
      )}
    </section>
  )
}
