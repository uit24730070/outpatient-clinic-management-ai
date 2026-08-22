import { useEffect, useMemo, useState } from 'react'
import { FlaskConical, Plus } from 'lucide-react'
import { createWalkInLabOrder, getLabOrder } from '../services/labOrderService'
import { listPatients } from '../services/patientService'
import { listServicePrices } from '../services/servicePriceService'
import { toastError, toastInfo, toastSuccess } from '../lib/toast'
import { formatVnd } from '../lib/format'
import { ServiceCategory, type ServicePrice } from '../types/invoice'
import type { Patient } from '../types/patient'
import type { LabOrder } from '../types/labOrder'
import { LabOrderCard } from '../components/LabOrderPanel'
import { PageHeader } from '../components/PageHeader'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

/**
 * Màn "Đăng ký cận lâm sàng (walk-in)" cho Lễ tân (ADR 0016): chọn bệnh nhân + các dịch vụ CLS
 * (loại Paraclinical) → tạo phiếu chỉ định không cần phiếu khám → in phiếu + lập hoá đơn phí CLS.
 */
export default function LabWalkInPage() {
  const [patientSearch, setPatientSearch] = useState('')
  const [patients, setPatients] = useState<Patient[]>([])
  const [patientId, setPatientId] = useState('')
  const [services, setServices] = useState<ServicePrice[]>([])
  const [picked, setPicked] = useState<string[]>([])
  const [note, setNote] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [created, setCreated] = useState<LabOrder[]>([])

  useEffect(() => {
    void (async () => {
      try {
        const res = await listServicePrices({
          page: 1,
          pageSize: 100,
          category: ServiceCategory.Paraclinical,
        })
        setServices(res.items)
      } catch (err) {
        toastError(err)
      }
    })()
  }, [])

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const res = await listPatients({
          page: 1,
          pageSize: 20,
          search: patientSearch.trim() || undefined,
        })
        if (active) setPatients(res.items)
      } catch {
        // Bỏ qua lỗi tìm kiếm.
      }
    })()
    return () => {
      active = false
    }
  }, [patientSearch])

  const serviceMap = useMemo(() => new Map(services.map((s) => [s.id, s])), [services])
  const pickedTotal = picked.reduce((sum, id) => sum + (serviceMap.get(id)?.unitPrice ?? 0), 0)

  const togglePick = (id: string) =>
    setPicked((cur) => (cur.includes(id) ? cur.filter((x) => x !== id) : [...cur, id]))

  const submit = async () => {
    if (!patientId) {
      toastInfo('Hãy chọn bệnh nhân.')
      return
    }
    if (picked.length === 0) return
    setSubmitting(true)
    try {
      const order = await createWalkInLabOrder({
        patientId,
        appointmentId: null,
        note: note.trim() || null,
        items: picked.map((servicePriceId) => ({ servicePriceId })),
      })
      toastSuccess(`Đã đăng ký phiếu ${order.code}.`)
      setCreated((cur) => [order, ...cur])
      setPicked([])
      setNote('')
    } catch (err) {
      toastError(err)
    } finally {
      setSubmitting(false)
    }
  }

  // Sau khi lập HĐ CLS/huỷ, tải lại các phiếu vừa tạo để phản ánh trạng thái (đã lập HĐ…).
  const refreshCreated = async () => {
    try {
      const fresh = await Promise.all(created.map((o) => getLabOrder(o.id)))
      setCreated(fresh)
    } catch (err) {
      toastError(err)
    }
  }

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title="Đăng ký cận lâm sàng (walk-in)"
        description="Bệnh nhân yêu cầu cận lâm sàng không qua khám — tạo phiếu, in, rồi lập hoá đơn để thu tiền."
      />

      <Card>
        <CardContent className="flex flex-col gap-4 p-6">
          {/* Bệnh nhân */}
          <div className="grid gap-2">
            <Label>Bệnh nhân *</Label>
            <Input
              type="search"
              placeholder="Tìm bệnh nhân theo tên, mã…"
              value={patientSearch}
              onChange={(e) => setPatientSearch(e.target.value)}
            />
            <Select value={patientId} onValueChange={setPatientId}>
              <SelectTrigger>
                <SelectValue placeholder="— Chọn bệnh nhân —" />
              </SelectTrigger>
              <SelectContent>
                {patients.map((p) => (
                  <SelectItem key={p.id} value={p.id}>
                    {p.fullName} ({p.code})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Dịch vụ CLS */}
          <div className="grid gap-2">
            <Label>Dịch vụ cận lâm sàng</Label>
            {services.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                Chưa có dịch vụ cận lâm sàng trong bảng giá.
              </p>
            ) : (
              <div className="flex flex-wrap gap-2">
                {services.map((s) => {
                  const on = picked.includes(s.id)
                  return (
                    <button
                      key={s.id}
                      type="button"
                      onClick={() => togglePick(s.id)}
                      className={
                        on
                          ? 'rounded-full border border-primary bg-primary/10 px-3 py-1 text-sm text-primary'
                          : 'rounded-full border px-3 py-1 text-sm text-muted-foreground hover:bg-muted'
                      }
                    >
                      {s.name} · {formatVnd(s.unitPrice)}
                    </button>
                  )
                })}
              </div>
            )}
          </div>

          <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
            <Input
              placeholder="Ghi chú (tuỳ chọn)"
              value={note}
              onChange={(e) => setNote(e.target.value)}
            />
            <Button
              type="button"
              onClick={() => void submit()}
              disabled={submitting || picked.length === 0}
            >
              <Plus className="size-4" />
              Đăng ký ({picked.length}) · {formatVnd(pickedTotal)}
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Phiếu vừa tạo trong phiên */}
      {created.length > 0 && (
        <div className="flex flex-col gap-3">
          <div className="flex items-center gap-2">
            <FlaskConical className="size-4 text-primary" />
            <h3 className="font-semibold">Phiếu vừa đăng ký</h3>
          </div>
          {created.map((o) => (
            <LabOrderCard key={o.id} order={o} canRecord={false} canBill onChanged={refreshCreated} />
          ))}
        </div>
      )}
    </section>
  )
}
