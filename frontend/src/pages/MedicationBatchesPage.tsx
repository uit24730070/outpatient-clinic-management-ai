import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { getMedication, getMedicationBatches } from '../services/medicationService'
import { toastError } from '../lib/toast'
import type { Medication, MedicationBatch } from '../types/medication'
import { PageHeader } from '../components/PageHeader'
import { TonedBadge } from '../components/StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

export default function MedicationBatchesPage() {
  const { id } = useParams<{ id: string }>()
  const [medication, setMedication] = useState<Medication | null>(null)
  const [batches, setBatches] = useState<MedicationBatch[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    let active = true
    void (async () => {
      try {
        const [m, bs] = await Promise.all([getMedication(id), getMedicationBatches(id)])
        if (active) {
          setMedication(m)
          setBatches(bs)
        }
      } catch (err) {
        if (active) toastError(err)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => {
      active = false
    }
  }, [id])

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  const today = new Date().toISOString().slice(0, 10)

  return (
    <section>
      <PageHeader
        title={medication ? `Lô thuốc · ${medication.name}` : 'Lô thuốc'}
        description={
          medication && (
            <>
              Mã: <strong className="text-foreground">{medication.code}</strong> · Hoạt chất:{' '}
              <strong className="text-foreground">{medication.activeIngredient}</strong> · Tồn tổng:{' '}
              <strong className="text-foreground">{medication.stockOnHand}</strong> {medication.unit}
            </>
          )
        }
        actions={
          <Button asChild variant="outline">
            <Link to="/medications">
              <ArrowLeft className="size-4" />
              Danh mục
            </Link>
          </Button>
        }
      />

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Số lô</TableHead>
                <TableHead>Hạn dùng</TableHead>
                <TableHead>Tồn</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {batches.length === 0 && (
                <TableRow>
                  <TableCell colSpan={3} className="h-24 text-center text-muted-foreground">
                    Chưa có lô nào. Hãy nhập kho để tạo lô.
                  </TableCell>
                </TableRow>
              )}
              {batches.map((b) => {
                const expired = b.expiryDate < today
                return (
                  <TableRow key={b.id}>
                    <TableCell className="font-medium">{b.batchNumber}</TableCell>
                    <TableCell>
                      <span className={expired ? 'text-destructive' : undefined}>{b.expiryDate}</span>
                      {expired && (
                        <TonedBadge tone="red" className="ml-2">
                          Đã hết hạn
                        </TonedBadge>
                      )}
                    </TableCell>
                    <TableCell>{b.quantityOnHand}</TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </section>
  )
}
