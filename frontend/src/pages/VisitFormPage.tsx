import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Trash2 } from 'lucide-react'
import { createVisit } from '../services/visitService'
import { listPatients } from '../services/patientService'
import { listDoctors } from '../services/doctorService'
import { listServicePrices } from '../services/servicePriceService'
import { toastError, toastInfo, toastSuccess } from '../lib/toast'
import { formatVnd } from '../lib/format'
import { ServiceCategory, type ServicePrice } from '../types/invoice'
import type { Patient } from '../types/patient'
import type { Doctor } from '../types/doctor'
import type { VisitServiceLineInput } from '../types/visit'
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

// Chuỗi cho input datetime-local theo GIỜ ĐỊA PHƯƠNG (không kèm timezone).
function toLocalInput(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

interface Row {
  doctorId: string
  servicePriceId: string
  startTime: string
  endTime: string
  reason: string
}

function defaultRow(offsetMinutes = 0): Row {
  const start = new Date()
  start.setMinutes(start.getMinutes() + offsetMinutes, 0, 0)
  const end = new Date(start)
  end.setMinutes(end.getMinutes() + 30)
  return { doctorId: '', servicePriceId: '', startTime: toLocalInput(start), endTime: toLocalInput(end), reason: '' }
}

const NONE = 'none'

/**
 * Màn "Tiếp đón" (walk-in) cho Lễ tân (ADR 0017): chọn bệnh nhân + đăng ký NHIỀU dịch vụ khám
 * (mỗi dòng = một bác sĩ + dịch vụ + khung giờ) → tạo một lượt gom tất cả.
 */
export default function VisitFormPage() {
  const navigate = useNavigate()
  const [patientSearch, setPatientSearch] = useState('')
  const [patients, setPatients] = useState<Patient[]>([])
  const [patientId, setPatientId] = useState('')
  const [doctors, setDoctors] = useState<Doctor[]>([])
  const [services, setServices] = useState<ServicePrice[]>([])
  const [note, setNote] = useState('')
  const [rows, setRows] = useState<Row[]>([defaultRow()])
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    void (async () => {
      try {
        const [d, s] = await Promise.all([
          listDoctors({ page: 1, pageSize: 100 }),
          listServicePrices({ page: 1, pageSize: 100, category: ServiceCategory.Consultation }),
        ])
        setDoctors(d.items)
        setServices(s.items)
      } catch (err) {
        toastError(err)
      }
    })()
  }, [])

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const res = await listPatients({ page: 1, pageSize: 20, search: patientSearch.trim() || undefined })
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
  const total = rows.reduce((sum, r) => sum + (serviceMap.get(r.servicePriceId)?.unitPrice ?? 0), 0)

  const updateRow = (idx: number, patch: Partial<Row>) =>
    setRows((cur) => cur.map((r, i) => (i === idx ? { ...r, ...patch } : r)))
  const removeRow = (idx: number) => setRows((cur) => cur.filter((_, i) => i !== idx))
  const addRow = () => setRows((cur) => [...cur, defaultRow(0)])

  const submit = async () => {
    if (!patientId) {
      toastInfo('Hãy chọn bệnh nhân.')
      return
    }
    if (rows.some((r) => !r.doctorId)) {
      toastInfo('Mỗi dịch vụ khám phải chọn bác sĩ.')
      return
    }
    setSubmitting(true)
    try {
      const servicesInput: VisitServiceLineInput[] = rows.map((r) => ({
        doctorId: r.doctorId,
        startTime: new Date(r.startTime).toISOString(),
        endTime: new Date(r.endTime).toISOString(),
        reason: r.reason.trim() || null,
        servicePriceId: r.servicePriceId || null,
      }))
      const visit = await createVisit({ patientId, note: note.trim() || null, services: servicesInput })
      toastSuccess(`Đã tạo lượt tiếp đón ${visit.code}.`)
      navigate(`/visits/${visit.id}`)
    } catch (err) {
      toastError(err)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title="Tiếp đón bệnh nhân"
        description="Đăng ký một lượt khám gồm một hoặc nhiều dịch vụ khám (nhiều bác sĩ/chuyên khoa)."
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

          <div className="grid gap-2">
            <Label>Ghi chú tiếp đón</Label>
            <Input placeholder="Tuỳ chọn" value={note} onChange={(e) => setNote(e.target.value)} />
          </div>
        </CardContent>
      </Card>

      {/* Các dịch vụ khám */}
      <div className="flex flex-col gap-3">
        {rows.map((row, idx) => (
          <Card key={idx}>
            <CardContent className="grid gap-3 p-4 md:grid-cols-2">
              <div className="grid gap-2">
                <Label>Bác sĩ *</Label>
                <Select value={row.doctorId} onValueChange={(v) => updateRow(idx, { doctorId: v })}>
                  <SelectTrigger>
                    <SelectValue placeholder="— Chọn bác sĩ —" />
                  </SelectTrigger>
                  <SelectContent>
                    {doctors.map((d) => (
                      <SelectItem key={d.id} value={d.id}>
                        {d.fullName}
                        {d.specialtyName ? ` · ${d.specialtyName}` : ''}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="grid gap-2">
                <Label>Dịch vụ khám</Label>
                <Select
                  value={row.servicePriceId || NONE}
                  onValueChange={(v) => updateRow(idx, { servicePriceId: v === NONE ? '' : v })}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="— Không gắn dịch vụ —" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={NONE}>— Không gắn dịch vụ —</SelectItem>
                    {services.map((s) => (
                      <SelectItem key={s.id} value={s.id}>
                        {s.name} · {formatVnd(s.unitPrice)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="grid gap-2">
                <Label>Bắt đầu</Label>
                <Input
                  type="datetime-local"
                  value={row.startTime}
                  onChange={(e) => updateRow(idx, { startTime: e.target.value })}
                />
              </div>

              <div className="grid gap-2">
                <Label>Kết thúc</Label>
                <Input
                  type="datetime-local"
                  value={row.endTime}
                  onChange={(e) => updateRow(idx, { endTime: e.target.value })}
                />
              </div>

              <div className="grid gap-2 md:col-span-2">
                <Label>Lý do khám</Label>
                <div className="flex gap-2">
                  <Input
                    placeholder="Tuỳ chọn"
                    value={row.reason}
                    onChange={(e) => updateRow(idx, { reason: e.target.value })}
                  />
                  {rows.length > 1 && (
                    <Button
                      type="button"
                      variant="ghost"
                      className="text-destructive hover:text-destructive"
                      onClick={() => removeRow(idx)}
                    >
                      <Trash2 className="size-4" />
                    </Button>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        ))}

        <div>
          <Button type="button" variant="outline" onClick={addRow}>
            <Plus className="size-4" />
            Thêm dịch vụ khám
          </Button>
        </div>
      </div>

      <div className="flex items-center justify-between">
        <span className="text-sm text-muted-foreground">
          Tạm tính công khám: <span className="font-semibold text-foreground">{formatVnd(total)}</span>
        </span>
        <Button type="button" onClick={() => void submit()} disabled={submitting}>
          Tạo lượt tiếp đón ({rows.length} dịch vụ)
        </Button>
      </div>
    </section>
  )
}
