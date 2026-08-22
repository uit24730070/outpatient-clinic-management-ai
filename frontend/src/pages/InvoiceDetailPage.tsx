import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, Pencil, Printer, Wallet, Loader2 } from 'lucide-react'
import {
  cancelInvoice,
  deleteInvoice,
  getInvoice,
  payInvoice,
} from '../services/invoiceService'
import { toastError, toastSuccess } from '../lib/toast'
import type { Invoice, PaymentMethodValue } from '../types/invoice'
import {
  InvoiceStatus,
  PaymentMethod,
  invoiceItemTypeLabels,
  paymentMethodLabels,
} from '../types/invoice'
import { formatVnd } from '../lib/format'
import { PageHeader } from '../components/PageHeader'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { InvoiceStatusBadge } from '../components/StatusBadge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { Label } from '@/components/ui/label'
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

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function InvoiceDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [invoice, setInvoice] = useState<Invoice | null>(null)
  const [loading, setLoading] = useState(true)

  const [payOpen, setPayOpen] = useState(false)
  const [method, setMethod] = useState<PaymentMethodValue>(PaymentMethod.Cash)
  const [paying, setPaying] = useState(false)

  const load = useCallback(async () => {
    if (!id) return
    setLoading(true)
    try {
      setInvoice(await getInvoice(id))
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  const onPay = async () => {
    if (!id) return
    setPaying(true)
    try {
      const updated = await payInvoice(id, method)
      setInvoice(updated)
      setPayOpen(false)
      toastSuccess('Đã thu tiền hoá đơn.')
    } catch (err) {
      toastError(err)
    } finally {
      setPaying(false)
    }
  }

  const onCancel = async () => {
    if (!id) return
    try {
      setInvoice(await cancelInvoice(id))
      toastSuccess('Đã huỷ hoá đơn.')
    } catch (err) {
      toastError(err)
    }
  }

  const onDelete = async () => {
    if (!id) return
    try {
      await deleteInvoice(id)
      toastSuccess('Đã xoá hoá đơn.')
      navigate('/invoices')
    } catch (err) {
      toastError(err)
    }
  }

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>
  if (!invoice) return <p className="text-muted-foreground">Không tìm thấy hoá đơn.</p>

  const isDraft = invoice.status === InvoiceStatus.Draft

  return (
    <section className="mx-auto max-w-3xl">
      <PageHeader
        title={`Hoá đơn ${invoice.code}`}
        actions={
          <div className="no-print flex flex-wrap items-center gap-2">
            <Button variant="outline" onClick={() => window.print()}>
              <Printer className="size-4" />
              In
            </Button>
            {isDraft && (
              <>
                <Button asChild variant="outline">
                  <Link to={`/invoices/${invoice.id}/edit`}>
                    <Pencil className="size-4" />
                    Sửa
                  </Link>
                </Button>
                <Dialog open={payOpen} onOpenChange={setPayOpen}>
                  <DialogTrigger asChild>
                    <Button>
                      <Wallet className="size-4" />
                      Thu tiền
                    </Button>
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>Thu tiền hoá đơn {invoice.code}</DialogTitle>
                    </DialogHeader>
                    <div className="grid gap-3 py-2">
                      <div className="flex items-center justify-between">
                        <span className="text-muted-foreground">Tổng tiền</span>
                        <span className="text-lg font-semibold tabular-nums">
                          {formatVnd(invoice.totalAmount)}
                        </span>
                      </div>
                      <div className="grid gap-2">
                        <Label>Phương thức thanh toán</Label>
                        <Select
                          value={String(method)}
                          onValueChange={(v) => setMethod(Number(v) as PaymentMethodValue)}
                        >
                          <SelectTrigger>
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            {Object.values(PaymentMethod).map((v) => (
                              <SelectItem key={v} value={String(v)}>
                                {paymentMethodLabels[v]}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                    </div>
                    <DialogFooter>
                      <Button variant="outline" onClick={() => setPayOpen(false)} disabled={paying}>
                        Huỷ
                      </Button>
                      <Button onClick={() => void onPay()} disabled={paying}>
                        {paying && <Loader2 className="size-4 animate-spin" />}
                        Xác nhận thu
                      </Button>
                    </DialogFooter>
                  </DialogContent>
                </Dialog>
                <ConfirmDialog
                  trigger={<Button variant="outline">Huỷ HĐ</Button>}
                  title="Huỷ hoá đơn?"
                  description={`Huỷ hoá đơn ${invoice.code}? Hoá đơn đã huỷ không thể khôi phục.`}
                  confirmText="Huỷ hoá đơn"
                  destructive
                  onConfirm={() => void onCancel()}
                />
                <ConfirmDialog
                  trigger={
                    <Button variant="ghost" className="text-destructive hover:text-destructive">
                      Xoá
                    </Button>
                  }
                  title="Xoá hoá đơn?"
                  description={`Xoá hoá đơn nháp ${invoice.code}?`}
                  confirmText="Xoá"
                  destructive
                  onConfirm={() => void onDelete()}
                />
              </>
            )}
            <Button asChild variant="ghost">
              <Link to="/invoices">
                <ArrowLeft className="size-4" />
                Danh sách
              </Link>
            </Button>
          </div>
        }
      />

      <Card className="print-area">
        <CardContent className="space-y-6">
          {/* Đầu trang hoá đơn */}
          <div className="flex items-start justify-between border-b pb-4">
            <div>
              <h2 className="text-xl font-bold">PHÒNG KHÁM CLINIC AI</h2>
              <p className="text-sm text-muted-foreground">Hoá đơn dịch vụ khám chữa bệnh</p>
            </div>
            <div className="text-right">
              <p className="font-mono text-lg font-semibold">{invoice.code}</p>
              <InvoiceStatusBadge status={invoice.status} />
            </div>
          </div>

          {/* Thông tin chung */}
          <div className="grid gap-2 text-sm sm:grid-cols-2">
            <div>
              <span className="text-muted-foreground">Bệnh nhân: </span>
              <span className="font-medium">{invoice.patientName ?? '—'}</span>
            </div>
            <div>
              <span className="text-muted-foreground">Ngày lập: </span>
              <span className="font-medium">{formatDate(invoice.createdAt)}</span>
            </div>
            {invoice.paidAt && (
              <>
                <div>
                  <span className="text-muted-foreground">Thanh toán: </span>
                  <span className="font-medium">
                    {invoice.paymentMethod != null
                      ? paymentMethodLabels[invoice.paymentMethod]
                      : '—'}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground">Thời điểm thu: </span>
                  <span className="font-medium">{formatDate(invoice.paidAt)}</span>
                </div>
              </>
            )}
          </div>

          {/* Bảng dòng */}
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-10">#</TableHead>
                <TableHead>Nội dung</TableHead>
                <TableHead>Loại</TableHead>
                <TableHead className="text-right">Đơn giá</TableHead>
                <TableHead className="text-right">SL</TableHead>
                <TableHead className="text-right">Thành tiền</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {invoice.items.map((it, i) => (
                <TableRow key={i}>
                  <TableCell className="text-muted-foreground">{i + 1}</TableCell>
                  <TableCell className="font-medium">{it.description}</TableCell>
                  <TableCell className="text-muted-foreground">
                    {invoiceItemTypeLabels[it.itemType]}
                  </TableCell>
                  <TableCell className="text-right tabular-nums">{formatVnd(it.unitPrice)}</TableCell>
                  <TableCell className="text-right tabular-nums">{it.quantity}</TableCell>
                  <TableCell className="text-right tabular-nums">{formatVnd(it.lineTotal)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>

          {/* Tổng cộng */}
          <div className="flex items-center justify-end gap-6 border-t pt-4">
            <span className="text-muted-foreground">Tổng cộng</span>
            <span className="text-2xl font-bold tabular-nums">{formatVnd(invoice.totalAmount)}</span>
          </div>

          {invoice.note && (
            <p className="text-sm">
              <span className="text-muted-foreground">Ghi chú: </span>
              {invoice.note}
            </p>
          )}
        </CardContent>
      </Card>
    </section>
  )
}
