import { useCallback, useEffect, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ArrowLeft, Plus, Receipt } from 'lucide-react'
import { getInvoicesByAppointment, listInvoices } from '../services/invoiceService'
import { useAuth } from '../store/auth'
import { canManageBilling } from '../config/access'
import { toastError } from '../lib/toast'
import type { PagedResult } from '../types/common'
import type { AppointmentInvoices, Invoice, InvoiceStatusValue } from '../types/invoice'
import { InvoiceStatus, invoiceStatusLabels, paymentMethodLabels } from '../types/invoice'
import { formatVnd } from '../lib/format'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { InvoiceStatusBadge } from '../components/StatusBadge'
import { SortableTableHead } from '../components/SortableTableHead'
import { useSort } from '../hooks/useSort'
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
  const [searchParams] = useSearchParams()
  const appointmentId = searchParams.get('appointmentId')
  const [status, setStatus] = useState<string>(ALL)
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Invoice> | null>(null)
  const [summary, setSummary] = useState<AppointmentInvoices | null>(null)
  const [loading, setLoading] = useState(false)
  const { sort, toggleSort } = useSort(() => setPage(1))

  const load = useCallback(async () => {
    setLoading(true)
    try {
      if (appointmentId) {
        // Chế độ gom theo lượt tiếp nhận: danh sách + tổng tính phía server.
        const group = await getInvoicesByAppointment(appointmentId)
        setSummary(group)
        setData(null)
      } else {
        const result = await listInvoices({
          page,
          pageSize: PAGE_SIZE,
          status: status === ALL ? undefined : (Number(status) as InvoiceStatusValue),
          sortBy: sort.sortBy,
          sortDesc: sort.sortDesc,
        })
        setData(result)
        setSummary(null)
      }
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [appointmentId, page, status, sort.sortBy, sort.sortDesc])

  useEffect(() => {
    void load()
  }, [load])

  // Nguồn dòng hiển thị: theo lượt (summary) hoặc danh sách phân trang.
  const rows: Invoice[] = appointmentId ? (summary?.invoices ?? []) : (data?.items ?? [])

  return (
    <section>
      <PageHeader
        title={appointmentId ? 'Hoá đơn của lượt khám' : 'Hoá đơn'}
        description={
          appointmentId
            ? 'Các hoá đơn độc lập cùng một lượt khám'
            : 'Lập hoá đơn, thu tiền và in cho bệnh nhân'
        }
        actions={
          canManage &&
          !appointmentId && (
            <Button asChild>
              <Link to="/invoices/new">
                <Plus className="size-4" />
                Tạo hoá đơn lẻ
              </Link>
            </Button>
          )
        }
      />

      {appointmentId ? (
        <Card className="mb-4">
          <CardContent className="flex flex-wrap items-center justify-between gap-4">
            <Button asChild variant="ghost" size="sm">
              <Link to="/appointments">
                <ArrowLeft className="size-4" />
                Về danh sách lịch khám
              </Link>
            </Button>
            <div className="flex flex-wrap gap-6 text-sm">
              <div>
                <span className="text-muted-foreground">Đã lập: </span>
                <span className="font-semibold tabular-nums">{formatVnd(summary?.totalBilled ?? 0)}</span>
              </div>
              <div>
                <span className="text-muted-foreground">Đã thu: </span>
                <span className="font-semibold tabular-nums text-emerald-600">
                  {formatVnd(summary?.totalPaid ?? 0)}
                </span>
              </div>
              <div>
                <span className="text-muted-foreground">Còn nợ: </span>
                <span className="font-semibold tabular-nums text-amber-600">
                  {formatVnd(summary?.totalOutstanding ?? 0)}
                </span>
              </div>
            </div>
          </CardContent>
        </Card>
      ) : (
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
      )}

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                {appointmentId ? (
                  <TableHead>Mã</TableHead>
                ) : (
                  <SortableTableHead field="code" sort={sort} onSort={toggleSort}>Mã</SortableTableHead>
                )}
                {appointmentId ? (
                  <TableHead>Ngày lập</TableHead>
                ) : (
                  <SortableTableHead field="createdAt" sort={sort} onSort={toggleSort}>Ngày lập</SortableTableHead>
                )}
                <TableHead>Bệnh nhân</TableHead>
                {appointmentId ? (
                  <TableHead>Trạng thái</TableHead>
                ) : (
                  <SortableTableHead field="status" sort={sort} onSort={toggleSort}>Trạng thái</SortableTableHead>
                )}
                <TableHead>Thanh toán</TableHead>
                {appointmentId ? (
                  <TableHead className="text-right">Tổng tiền</TableHead>
                ) : (
                  <SortableTableHead
                    field="totalAmount"
                    sort={sort}
                    onSort={toggleSort}
                    className="text-right"
                    align="right"
                  >
                    Tổng tiền
                  </SortableTableHead>
                )}
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
              {!loading && rows.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7} className="h-24 text-center text-muted-foreground">
                    Chưa có hoá đơn nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                rows.map((inv) => (
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

      {!appointmentId && data && (
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
