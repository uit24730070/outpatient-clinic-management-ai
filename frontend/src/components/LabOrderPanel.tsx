import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { FlaskConical, Plus, Printer, Receipt, Save, Trash2 } from 'lucide-react'
import {
  cancelLabOrder,
  createLabOrder,
  listLabOrders,
  setLabResult,
} from '../services/labOrderService'
import { createInvoiceFromLabOrder } from '../services/invoiceService'
import { listServicePrices } from '../services/servicePriceService'
import { toastError, toastSuccess } from '../lib/toast'
import { formatVnd } from '../lib/format'
import { ServiceCategory, type ServicePrice } from '../types/invoice'
import {
  LabOrderItemStatus,
  LabOrderStatus,
  type LabOrder,
} from '../types/labOrder'
import { LabOrderStatusBadge } from './StatusBadge'
import { ConfirmDialog } from './ConfirmDialog'
import { ServiceMultiPicker } from './ServiceMultiPicker'
import { useAutoRefresh } from '../hooks/useAutoRefresh'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'

interface Props {
  encounterId: string
  /** Lượt tiếp nhận của lịch khám đang mở — dùng để liệt kê MỌI phiếu CLS trong lượt (kể cả phiếu
   * walk-in lễ tân đăng ký lúc tiếp nhận, hoặc do bác sĩ khác chỉ định), không chỉ phiếu của riêng
   * phiếu khám này — để bác sĩ thấy đủ trước khi chỉ định thêm, tránh chỉ định trùng dịch vụ. */
  visitId?: string | null
  /** Cho phép chỉ định mới (bác sĩ + phiếu khám còn nháp). */
  canOrder: boolean
  /** Cho phép nhập kết quả (bác sĩ/kỹ thuật viên). */
  canRecord: boolean
  /** Cho phép lập hoá đơn phí CLS (vai trò viện phí). */
  canBill?: boolean
}

/**
 * Khối "Cận lâm sàng" nhúng trong màn khám: liệt kê phiếu chỉ định trong cả lượt tiếp nhận (không chỉ
 * riêng phiếu khám này — bác sĩ cần thấy CLS đã đăng ký lúc tiếp nhận/do bác sĩ khác chỉ định để tránh
 * chỉ định trùng, dù backend cũng đã chặn), cho chỉ định dịch vụ Paraclinical mới, nhập kết quả từng
 * mục, và in phiếu kết quả (ADR 0015).
 */
export function LabOrderPanel({ encounterId, visitId, canOrder, canRecord, canBill }: Props) {
  const [orders, setOrders] = useState<LabOrder[]>([])
  const [services, setServices] = useState<ServicePrice[]>([])
  const [loading, setLoading] = useState(true)
  const [picked, setPicked] = useState<string[]>([])
  const [note, setNote] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const load = useCallback(async (opts?: { silent?: boolean }) => {
    try {
      const res = visitId
        ? await listLabOrders({ page: 1, pageSize: 50, visitId })
        : await listLabOrders({ page: 1, pageSize: 50, encounterId })
      setOrders(res.items)
    } catch (err) {
      if (!opts?.silent) toastError(err)
    } finally {
      if (!opts?.silent) setLoading(false)
    }
  }, [encounterId, visitId])

  useEffect(() => {
    void load()
  }, [load])

  // Lễ tân/bác sĩ khác có thể chỉ định thêm CLS vào cùng lượt trong lúc popup khám đang mở — tự làm
  // mới để thấy ngay, không cần đóng/mở lại tab (khớp pattern ở các workspace khác).
  useAutoRefresh(() => void load({ silent: true }))

  useEffect(() => {
    if (!canOrder) return
    void (async () => {
      try {
        const res = await listServicePrices({
          page: 1,
          pageSize: 100,
          category: ServiceCategory.Paraclinical,
        })
        setServices(res.items)
      } catch {
        // Không tải được danh mục CLS — vẫn xem được phiếu đã chỉ định.
      }
    })()
  }, [canOrder])

  const serviceMap = useMemo(() => new Map(services.map((s) => [s.id, s])), [services])

  const togglePick = (id: string) => {
    setPicked((cur) => (cur.includes(id) ? cur.filter((x) => x !== id) : [...cur, id]))
  }

  const submitOrder = async () => {
    if (picked.length === 0) return
    setSubmitting(true)
    try {
      await createLabOrder({
        encounterId,
        note: note.trim() || null,
        items: picked.map((servicePriceId) => ({ servicePriceId })),
      })
      toastSuccess('Đã tạo phiếu chỉ định cận lâm sàng.')
      setPicked([])
      setNote('')
      await load()
    } catch (err) {
      toastError(err)
    } finally {
      setSubmitting(false)
    }
  }

  const pickedTotal = picked.reduce((sum, id) => sum + (serviceMap.get(id)?.unitPrice ?? 0), 0)

  return (
    <Card>
      <CardContent className="flex flex-col gap-4 p-6">
        <div className="flex items-center gap-2">
          <FlaskConical className="size-4 text-primary" />
          <h3 className="font-semibold">Cận lâm sàng</h3>
        </div>

        {/* Chỉ định mới */}
        {canOrder && (
          <div className="rounded-md border p-3">
            <p className="mb-2 text-sm font-medium">Chỉ định dịch vụ</p>
            <ServiceMultiPicker services={services} picked={picked} onToggle={togglePick} />
            {picked.length > 0 && (
              <div className="mt-3 flex items-center gap-2">
                <Input
                  placeholder="Ghi chú chỉ định (tuỳ chọn)"
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
                <Button type="button" onClick={() => void submitOrder()} disabled={submitting}>
                  <Plus className="size-4" />
                  Chỉ định ({picked.length}) · {formatVnd(pickedTotal)}
                </Button>
              </div>
            )}
          </div>
        )}

        {/* Danh sách phiếu chỉ định */}
        {loading ? (
          <p className="text-sm text-muted-foreground">Đang tải…</p>
        ) : orders.length === 0 ? (
          <p className="text-sm text-muted-foreground">Chưa có chỉ định cận lâm sàng.</p>
        ) : (
          <div className="flex flex-col gap-3">
            {orders.map((o) => (
              <LabOrderCard
                key={o.id}
                order={o}
                // Phiếu không gắn phiếu khám này (đăng ký lúc tiếp nhận, hoặc do bác sĩ khác chỉ định
                // trong cùng lượt) — gắn nhãn để bác sĩ biết, tránh chỉ định trùng dịch vụ.
                fromOtherSource={o.encounterId !== encounterId}
                canRecord={canRecord}
                // Panel này chỉ nhúng ở màn khám (canRecord truyền vào = canRecordEncounter của bác sĩ),
                // nên trùng luôn quyền huỷ phiếu (Roles.RecordEncounter) — khác TechnicianLabPage.
                canCancel={canRecord}
                canBill={canBill}
                onChanged={load}
              />
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

/**
 * Thẻ một phiếu chỉ định: tiêu đề (mã + trạng thái + lập HĐ/in/huỷ) và các dòng mục để nhập kết quả.
 * Tái dùng ở màn khám (LabOrderPanel) lẫn màn "Thực hiện CLS" của kỹ thuật viên (ADR 0016).
 */
export function LabOrderCard({
  order,
  fromOtherSource,
  canRecord,
  canCancel,
  canBill,
  onChanged,
}: {
  order: LabOrder
  /** Phiếu không gắn phiếu khám hiện tại (đăng ký lúc tiếp nhận hoặc do bác sĩ khác chỉ định cùng lượt). */
  fromOtherSource?: boolean
  /** Cho phép nhập/sửa kết quả — khớp Roles.RecordLabResult (Admin/Bác sĩ/Kỹ thuật viên). */
  canRecord: boolean
  /** Cho phép huỷ phiếu chỉ định — khớp Roles.RecordEncounter (chỉ Admin/Bác sĩ, không gồm Kỹ thuật viên). */
  canCancel?: boolean
  canBill?: boolean
  onChanged: () => Promise<void>
}) {
  const active = order.status !== LabOrderStatus.Completed && order.status !== LabOrderStatus.Cancelled
  // Gating thanh toán trước khi thực hiện (ADR 0021, PAY-01): chưa thu phí CLS → không cho nhập kết quả.
  const paid = order.paidAt != null
  const [billing, setBilling] = useState(false)

  const cancel = async () => {
    try {
      await cancelLabOrder(order.id)
      toastSuccess('Đã huỷ phiếu chỉ định.')
      await onChanged()
    } catch (err) {
      toastError(err)
    }
  }

  const bill = async () => {
    setBilling(true)
    try {
      await createInvoiceFromLabOrder(order.id)
      toastSuccess('Đã lập hoá đơn phí cận lâm sàng.')
      await onChanged()
    } catch (err) {
      toastError(err)
    } finally {
      setBilling(false)
    }
  }

  return (
    <div className="rounded-md border">
      <div className="flex items-center justify-between border-b bg-muted/30 px-3 py-2">
        <div className="flex items-center gap-2">
          <span className="font-mono text-sm">{order.code}</span>
          <LabOrderStatusBadge status={order.status} />
          {fromOtherSource && (
            <span className="text-xs text-muted-foreground">
              · {order.doctorName ? `BS. ${order.doctorName} chỉ định` : 'đăng ký lúc tiếp nhận'}
            </span>
          )}
          {order.invoicedAt && (
            <span className="text-xs text-muted-foreground">· đã lập HĐ</span>
          )}
          {!paid && order.status !== LabOrderStatus.Cancelled && (
            <span className="text-xs font-medium text-amber-600">· chưa thanh toán</span>
          )}
        </div>
        <div className="flex items-center gap-1">
          {canBill && order.status !== LabOrderStatus.Cancelled && !order.invoicedAt && (
            <Button size="sm" variant="ghost" onClick={() => void bill()} disabled={billing}>
              <Receipt className="size-4" />
              Lập HĐ CLS
            </Button>
          )}
          <Button asChild size="sm" variant="ghost">
            <Link to={`/lab-orders/${order.id}/print`} target="_blank">
              <Printer className="size-4" />
              In
            </Link>
          </Button>
          {canCancel && active && (
            <ConfirmDialog
              trigger={
                <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive">
                  <Trash2 className="size-4" />
                  Huỷ
                </Button>
              }
              title="Huỷ phiếu chỉ định?"
              description={`Huỷ phiếu chỉ định ${order.code}? Không thể hoàn tác.`}
              confirmText="Huỷ phiếu"
              destructive
              onConfirm={() => void cancel()}
            />
          )}
        </div>
      </div>
      <div className="divide-y">
        {order.items.map((it) => (
          <LabItemRow
            key={it.id}
            orderId={order.id}
            item={it}
            editable={canRecord && active && paid}
            blockedUnpaid={canRecord && active && !paid}
            onChanged={onChanged}
          />
        ))}
      </div>
    </div>
  )
}

function LabItemRow({
  orderId,
  item,
  editable,
  blockedUnpaid,
  onChanged,
}: {
  orderId: string
  item: LabOrder['items'][number]
  editable: boolean
  /** Được phép nhập nhưng bị chặn vì chưa thu phí CLS (ADR 0021, PAY-01). */
  blockedUnpaid?: boolean
  onChanged: () => Promise<void>
}) {
  const [resultText, setResultText] = useState(item.resultText ?? '')
  const [conclusion, setConclusion] = useState(item.conclusion ?? '')
  const [saving, setSaving] = useState(false)
  const done = item.status === LabOrderItemStatus.Completed

  const save = async () => {
    setSaving(true)
    try {
      await setLabResult(orderId, item.id, {
        resultText: resultText.trim() || null,
        conclusion: conclusion.trim() || null,
      })
      toastSuccess('Đã lưu kết quả.')
      await onChanged()
    } catch (err) {
      toastError(err)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="px-3 py-2">
      <div className="flex items-center justify-between">
        <span className="font-medium">{item.serviceName}</span>
        <span className="text-sm text-muted-foreground">
          {done ? 'Đã có kết quả' : 'Chờ kết quả'}
        </span>
      </div>
      {editable ? (
        <div className="mt-2 flex flex-col gap-2 sm:flex-row">
          <Input
            placeholder="Kết quả"
            value={resultText}
            onChange={(e) => setResultText(e.target.value)}
          />
          <Input
            placeholder="Kết luận (tuỳ chọn)"
            value={conclusion}
            onChange={(e) => setConclusion(e.target.value)}
          />
          <Button type="button" size="sm" onClick={() => void save()} disabled={saving}>
            <Save className="size-4" />
            Lưu
          </Button>
        </div>
      ) : blockedUnpaid ? (
        <p className="mt-1 text-sm text-amber-600">
          Cần thu phí cận lâm sàng (lập hoá đơn + thanh toán) trước khi nhập kết quả.
        </p>
      ) : (
        <div className="mt-1 text-sm">
          {item.resultText ? (
            <>
              <span className="text-muted-foreground">Kết quả:</span> {item.resultText}
              {item.conclusion && (
                <>
                  {' '}
                  · <span className="text-muted-foreground">Kết luận:</span> {item.conclusion}
                </>
              )}
            </>
          ) : (
            <span className="text-muted-foreground">Chưa có kết quả.</span>
          )}
        </div>
      )}
    </div>
  )
}
