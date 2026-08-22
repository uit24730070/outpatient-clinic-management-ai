import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Receipt } from 'lucide-react'
import { listInvoices } from '../services/invoiceService'
import { useAuth } from '../store/auth'
import { canManageBilling } from '../config/access'
import { toastError } from '../lib/toast'
import type { PagedResult } from '../types/common'
import type { Invoice, InvoiceStatusValue } from '../types/invoice'
import { InvoiceStatus, invoiceStatusLabels, paymentMethodLabels } from '../types/invoice'
import { formatVnd } from '../lib/format'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { InvoiceStatusBadge } from '../components/StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const PAGE_SIZE = 10
const ALL = 'all'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function InvoicesListPage() {
  const { user } = useAuth()
  const canManage = canManageBilling(user?.role)
  const [status, setStatus] = useState<string>(ALL)
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Invoice> | null>(null)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listInvoices({
        page,
        pageSize: PAGE_SIZE,
        status: status === ALL ? undefined : (Number(status) as InvoiceStatusValue),
      })
      setData(result)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [page, status])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <section>
      <PageHeader
        title="Hoá đơn"
        description="Lập hoá đơn, thu tiền và in cho bệnh nhân"
        actions={
          canManage && (
            <Button asChild>
              <Link to="/invoices/new">
                <Plus className="size-4" />
                Tạo hoá đơn lẻ
              </Link>
            </Button>
          )
        }
      />

      <Card className="mb-4">
        <CardContent>
          <div className="flex flex-wrap items-center gap-3">
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground">Trạng thái</span>
              <Select
                value={status}
                onValueChange={(v) => {
                  setPage(1)
                  setStatus(v)
                }}
              >
                <SelectTrigger className="w-40">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={ALL}>Tất cả</SelectItem>
                  {Object.values(InvoiceStatus).map((v) => (
                    <SelectItem key={v} value={String(v)}>
                      {invoiceStatusLabels[v]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã</TableHead>
                <TableHead>Ngày lập</TableHead>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Thanh toán</TableHead>
                <TableHead className="text-right">Tổng tiền</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={7} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="h-24 text-center text-muted-foreground">
                    Chưa có hoá đơn nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((inv) => (
                  <TableRow key={inv.id}>
                    <TableCell className="font-mono text-sm">{inv.code}</TableCell>
                    <TableCell className="whitespace-nowrap">{formatDate(inv.createdAt)}</TableCell>
                    <TableCell className="font-medium">{inv.patientName ?? '—'}</TableCell>
                    <TableCell>
                      <InvoiceStatusBadge status={inv.status} />
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {inv.paymentMethod != null ? paymentMethodLabels[inv.paymentMethod] : '—'}
                    </TableCell>
                    <TableCell className="text-right font-semibold tabular-nums">
                      {formatVnd(inv.totalAmount)}
                    </TableCell>
                    <TableCell className="text-right">
                      <Button asChild size="sm" variant="ghost">
                        <Link to={`/invoices/${inv.id}`}>
                          <Receipt className="size-4" />
                          Chi tiết
                        </Link>
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {data && (
        <Pager
          page={data.page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          onPageChange={setPage}
        />
      )}
    </section>
  )
}
