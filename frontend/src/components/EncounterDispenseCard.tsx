import { PackageCheck, Pill, Undo2 } from 'lucide-react'
import { DispenseStatusBadge } from './StatusBadge'
import { PatientContextHeader } from './PatientContextHeader'
import { ConfirmDialog } from './ConfirmDialog'
import { Button } from '@/components/ui/button'
import type { Encounter } from '../types/encounter'

interface Props {
  encounter: Encounter
  action: 'dispense' | 'return'
  busy: boolean
  onDispense: (id: string) => void
  onReturnStock: (id: string) => void
}

/**
 * Thẻ một đơn thuốc chờ cấp phát/hoàn kho — tách khỏi `PharmacyDispensePage` để dùng lại được ở
 * `PharmacyWorkspacePage` (Epic 17, UX-05) mà không trùng lặp code.
 */
export function EncounterDispenseCard({ encounter: e, action, busy, onDispense, onReturnStock }: Props) {
  const meds = e.prescriptionItems.filter((i) => i.medicationId != null)
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
          <ConfirmDialog
            trigger={
              <Button
                size="sm"
                variant="outline"
                className="text-destructive hover:text-destructive"
                disabled={busy}
              >
                <Undo2 className="size-4" />
                Hoàn kho
              </Button>
            }
            title="Hoàn kho đơn thuốc?"
            description={`Nhập lại tồn đúng lô đã trừ cho đơn của ${e.patientName ?? 'bệnh nhân'}? Thao tác ghi bút toán bù — sổ cái giữ nguyên.`}
            confirmText="Hoàn kho"
            destructive
            onConfirm={() => onReturnStock(e.id)}
          />
        )}
      </div>
    </div>
  )
}
