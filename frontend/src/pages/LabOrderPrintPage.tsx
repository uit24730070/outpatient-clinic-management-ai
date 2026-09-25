import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { Printer } from 'lucide-react'
import { getLabOrder } from '../services/labOrderService'
import { toastError } from '../lib/toast'
import { formatVnd } from '../lib/format'
import { labOrderStatusLabels, type LabOrder } from '../types/labOrder'
import { Button } from '@/components/ui/button'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/**
 * Phiếu kết quả cận lâm sàng (bản in). Layout tối giản, không sidebar; nút "In" gọi window.print().
 * CSS @media print ẩn thanh công cụ (ADR 0015).
 */
export default function LabOrderPrintPage() {
  const { id } = useParams<{ id: string }>()
  const [order, setOrder] = useState<LabOrder | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!id) return
    void (async () => {
      try {
        setOrder(await getLabOrder(id))
      } catch (err) {
        toastError(err)
      } finally {
        setLoading(false)
      }
    })()
  }, [id])

  if (loading) return <p className="p-8 text-muted-foreground">Đang tải…</p>
  if (!order) return <p className="p-8 text-destructive">Không tìm thấy phiếu chỉ định.</p>

  return (
    <div className="mx-auto max-w-3xl p-8 text-sm text-black">
      <style>{`@media print { .no-print { display: none !important; } }`}</style>

      <div className="no-print mb-4 flex justify-end">
        <Button onClick={() => window.print()}>
          <Printer className="size-4" />
          In phiếu
        </Button>
      </div>

      <header className="mb-6 text-center">
        <h1 className="text-xl font-bold uppercase">Phiếu kết quả cận lâm sàng</h1>
        <p className="mt-1 text-muted-foreground">
          Mã phiếu: <strong>{order.code}</strong> · Trạng thái:{' '}
          {labOrderStatusLabels[order.status]}
        </p>
      </header>

      <div className="mb-4 grid grid-cols-2 gap-2">
        <p>
          <span className="text-muted-foreground">Bệnh nhân:</span>{' '}
          <strong>{order.patientName ?? '—'}</strong>
        </p>
        <p>
          <span className="text-muted-foreground">Bác sĩ chỉ định:</span>{' '}
          {order.doctorName ?? '—'}
        </p>
        <p>
          <span className="text-muted-foreground">Ngày chỉ định:</span>{' '}
          {formatDate(order.createdAt)}
        </p>
      </div>

      {order.note && (
        <p className="mb-4">
          <span className="text-muted-foreground">Ghi chú:</span> {order.note}
        </p>
      )}

      <table className="w-full border-collapse">
        <thead>
          <tr className="border-y">
            <th className="py-2 text-left">Dịch vụ</th>
            <th className="py-2 text-left">Kết quả</th>
            <th className="py-2 text-left">Kết luận</th>
            <th className="py-2 text-right">Đơn giá</th>
          </tr>
        </thead>
        <tbody>
          {order.items.map((it) => (
            <tr key={it.id} className="border-b align-top">
              <td className="py-2 font-medium">{it.serviceName}</td>
              <td className="py-2">
                {it.parameters.length > 0 ? (
                  <table className="w-full border-collapse text-xs">
                    <tbody>
                      {it.parameters.map((p, idx) => (
                        <tr key={idx}>
                          <td className="pr-2 py-0.5">{p.name}</td>
                          <td className={`pr-2 py-0.5 ${p.isAbnormal ? 'font-bold' : ''}`}>
                            {p.value}
                            {p.isAbnormal ? ' *' : ''}
                          </td>
                          <td className="pr-2 py-0.5 text-gray-500">{p.unit ?? ''}</td>
                          <td className="py-0.5 text-gray-500">
                            {p.referenceRange ? `(${p.referenceRange})` : ''}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                ) : (
                  (it.resultText ?? '—')
                )}
              </td>
              <td className="py-2">{it.conclusion ?? '—'}</td>
              <td className="py-2 text-right tabular-nums">{formatVnd(it.unitPrice)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <td colSpan={3} className="py-2 text-right font-medium">
              Tổng phí:
            </td>
            <td className="py-2 text-right font-bold tabular-nums">
              {formatVnd(order.totalAmount)}
            </td>
          </tr>
        </tfoot>
      </table>

      {order.items.some((it) => it.parameters.some((p) => p.isAbnormal)) && (
        <p className="mt-2 text-xs text-gray-500">* Ngoài khoảng tham chiếu.</p>
      )}

      <div className="mt-12 flex justify-end pr-8 text-center">
        <div>
          <p className="italic text-muted-foreground">Ngày ... tháng ... năm ...</p>
          <p className="mt-1 font-medium">Người thực hiện</p>
          <p className="mt-16">{order.doctorName ?? ''}</p>
        </div>
      </div>
    </div>
  )
}
