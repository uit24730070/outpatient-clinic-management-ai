import { useState } from 'react'
import { Loader2, PackageCheck, Pill, Undo2 } from 'lucide-react'
import { DispenseStatusBadge } from './StatusBadge'
import { PatientContextHeader } from './PatientContextHeader'
import { ConfirmDialog } from './ConfirmDialog'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import type { Encounter } from '../types/encounter'
import type { ReturnStockItem } from '../services/encounterService'

interface Props {
  encounter: Encounter
  action: 'dispense' | 'return'
  busy: boolean
  onDispense: (id: string) => void
  onReturnStock: (id: string, reason: string, items: ReturnStockItem[]) => void
}

// Cửa sổ hoàn kho — phải khớp Encounter.ReturnWindow phía backend (ADR 0022 bổ sung).
const RETURN_WINDOW_HOURS = 24

/**
 * Thẻ một đơn thuốc chờ cấp phát/hoàn kho — tách khỏi `PharmacyDispensePage` để dùng lại được ở
 * `PharmacyWorkspacePage` (Epic 17, UX-05) mà không trùng lặp code.
 */
export function EncounterDispenseCard({ encounter: e, action, busy, onDispense, onReturnStock }: Props) {
  const meds = e.prescriptionItems.filter(
    (i): i is typeof i & { medicationId: string } => i.medicationId != null,
  )
  const [returnOpen, setReturnOpen] = useState(false)
  const [returnReason, setReturnReason] = useState('')
  const [returnQty, setReturnQty] = useState<Record<string, number>>({})

  const dispensedAtMs = e.dispensedAt ? new Date(e.dispensedAt).getTime() : null
  const withinReturnWindow = dispensedAtMs == null || Date.now() - dispensedAtMs <= RETURN_WINDOW_HOURS * 3_600_000

  const openReturnDialog = () => {
    setReturnReason('')
    setReturnQty(Object.fromEntries(meds.map((i) => [i.medicationId, i.quantity])))
    setReturnOpen(true)
  }

  const hasReturnableQty = meds.some((i) => (returnQty[i.medicationId] ?? 0) > 0)
  return (
    <div className="rounded-lg border bg-card p-4">
      <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
        <PatientContextHeader patientId={e.patientId} fallbackName={e.patientName} variant="inline" />
        <div className="flex items-center gap-2">
          {e.doctorName && <span className="text-sm text-muted-foreground">BS: {e.doctorName}</span>}
          <DispenseStatusBadge status={e.dispenseStatus} />
        </div>
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
        {action === 'dispense' ? (
          <ConfirmDialog
            trigger={
              <Button size="sm" disabled={busy}>
                <PackageCheck className="size-4" />
                Cấp phát
              </Button>
            }
            title="Cấp phát thuốc?"
            description={`Xuất kho theo FEFO cho đơn của ${e.patientName ?? 'bệnh nhân'}? Thao tác trừ tồn thực.`}
            confirmText="Cấp phát"
            onConfirm={() => onDispense(e.id)}
          />
        ) : (
          <Dialog open={returnOpen} onOpenChange={(open) => (open ? openReturnDialog() : setReturnOpen(false))}>
            <DialogTrigger asChild>
              <Button
                size="sm"
                variant="outline"
                className="text-destructive hover:text-destructive"
                disabled={busy || !withinReturnWindow}
                title={withinReturnWindow ? undefined : `Đã quá ${RETURN_WINDOW_HOURS}h kể từ khi cấp phát`}
              >
                <Undo2 className="size-4" />
                {withinReturnWindow ? 'Hoàn kho' : `Quá hạn hoàn kho (${RETURN_WINDOW_HOURS}h)`}
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Hoàn kho đơn thuốc?</DialogTitle>
              </DialogHeader>
              <div className="grid gap-3 py-2">
                <p className="text-sm text-muted-foreground">
                  Chọn số lượng muốn hoàn cho từng thuốc của đơn {e.patientName ?? 'bệnh nhân'} (tối đa số đã
                  cấp). Thao tác ghi bút toán bù — sổ cái giữ nguyên, không thể hoàn tiếp phần còn lại sau khi
                  xác nhận.
                </p>
                <ul className="flex flex-col gap-2 text-sm">
                  {meds.map((i, idx) => (
                    <li key={idx} className="flex items-center gap-2">
                      <Pill className="size-3.5 shrink-0 text-primary" />
                      <span className="min-w-0 flex-1">
                        <span className="font-medium">{i.drugName}</span>
                        <span className="text-muted-foreground"> · {i.dosage} · đã cấp {i.quantity}</span>
                      </span>
                      <Input
                        type="number"
                        min={0}
                        max={i.quantity}
                        className="w-20"
                        value={returnQty[i.medicationId] ?? 0}
                        onChange={(ev) => {
                          const raw = Number(ev.target.value)
                          const clamped = Number.isFinite(raw) ? Math.max(0, Math.min(i.quantity, raw)) : 0
                          setReturnQty((prev) => ({ ...prev, [i.medicationId]: clamped }))
                        }}
                      />
                    </li>
                  ))}
                </ul>
                <div className="grid gap-2">
                  <Label>Lý do hoàn kho</Label>
                  <Textarea
                    placeholder="Nhập lý do hoàn kho…"
                    value={returnReason}
                    onChange={(ev) => setReturnReason(ev.target.value)}
                    rows={3}
                  />
                </div>
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={() => setReturnOpen(false)} disabled={busy}>
                  Đóng
                </Button>
                <Button
                  variant="destructive"
                  disabled={busy || !returnReason.trim() || !hasReturnableQty}
                  onClick={() => {
                    const items: ReturnStockItem[] = meds
                      .map((i) => ({ medicationId: i.medicationId, quantity: returnQty[i.medicationId] ?? 0 }))
                      .filter((i) => i.quantity > 0)
                    onReturnStock(e.id, returnReason.trim(), items)
                    setReturnOpen(false)
                    setReturnReason('')
                  }}
                >
                  {busy && <Loader2 className="size-4 animate-spin" />}
                  Xác nhận hoàn kho
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        )}
      </div>
    </div>
  )
}
