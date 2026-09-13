import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Pill, PackagePlus } from 'lucide-react'
import { getPharmacyAlerts } from '../services/pharmacyService'
import { toastError } from '../lib/toast'
import type { PharmacyAlerts } from '../types/medication'
import { PageHeader } from '../components/PageHeader'
import { TonedBadge } from '../components/StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const EXPIRING_IN_DAYS = 30

export default function PharmacyAlertsPage() {
  const [data, setData] = useState<PharmacyAlerts | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const result = await getPharmacyAlerts(EXPIRING_IN_DAYS)
        if (active) setData(result)
      } catch (err) {
        if (active) toastError(err)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => {
      active = false
    }
  }, [])

  return (
    <section className="space-y-6">
      <PageHeader
        title="Cảnh báo kho"
        description="Thuốc tồn thấp và lô sắp/đã hết hạn"
        actions={
          <Button asChild variant="outline">
            <Link to="/medications">
              <Pill className="size-4" />
              Danh mục thuốc
            </Link>
          </Button>
        }
      />

      {loading && <p className="text-muted-foreground">Đang tải…</p>}

      {data && (
        <>
          <Card>
            <CardHeader>
              <CardTitle className="text-base">
                Tồn thấp{' '}
                <span className="font-normal text-muted-foreground">(≤ ngưỡng đặt lại)</span>
              </CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Mã</TableHead>
                    <TableHead>Tên thuốc</TableHead>
                    <TableHead>Tồn</TableHead>
                    <TableHead>Ngưỡng</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.lowStock.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={6} className="h-16 text-center text-muted-foreground">
                        Không có thuốc tồn thấp.
                      </TableCell>
                    </TableRow>
                  )}
                  {data.lowStock.map((m) => (
                    <TableRow key={m.medicationId}>
                      <TableCell className="font-mono text-sm">{m.code}</TableCell>
                      <TableCell className="font-medium">{m.name}</TableCell>
                      <TableCell className="font-semibold text-destructive">
                        {m.stockOnHand} {m.unit}
                      </TableCell>
                      <TableCell>{m.reorderLevel}</TableCell>
                      <TableCell>
                        <TonedBadge tone={m.stockOnHand <= 0 ? 'red' : 'amber'}>
                          {m.stockOnHand <= 0 ? 'Hết hàng' : 'Tồn thấp'}
                        </TonedBadge>
                      </TableCell>
                      <TableCell className="text-right">
                        <Button asChild size="sm" variant="ghost">
                          <Link to="/stock-receipts/new">
                            <PackagePlus className="size-4" />
                            Nhập thêm
                          </Link>
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">
                Sắp / đã hết hạn{' '}
                <span className="font-normal text-muted-foreground">
                  (trong {data.expiringInDays} ngày tới, còn tồn)
                </span>
              </CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Thuốc</TableHead>
                    <TableHead>Số lô</TableHead>
                    <TableHead>Hạn dùng</TableHead>
                    <TableHead>Tồn lô</TableHead>
                    <TableHead>Trạng thái</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.expiringBatches.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={5} className="h-16 text-center text-muted-foreground">
                        Không có lô sắp/đã hết hạn.
                      </TableCell>
                    </TableRow>
                  )}
                  {data.expiringBatches.map((b) => (
                    <TableRow key={b.batchId}>
                      <TableCell className="font-medium">
                        {b.medicationName}{' '}
                        <span className="text-muted-foreground">({b.medicationCode})</span>
                      </TableCell>
                      <TableCell>{b.batchNumber}</TableCell>
                      <TableCell className={b.isExpired ? 'text-destructive' : undefined}>
                        {b.expiryDate}
                      </TableCell>
                      <TableCell>{b.quantityOnHand}</TableCell>
                      <TableCell>
                        <TonedBadge tone={b.isExpired ? 'red' : 'amber'}>
                          {b.isExpired ? 'Đã hết hạn' : 'Sắp hết hạn'}
                        </TonedBadge>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </CardContent>
          </Card>
        </>
      )}
    </section>
  )
}
