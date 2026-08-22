import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { ArrowLeft, Plus, Trash2, CheckCircle2 } from 'lucide-react'
import {
  completeEncounter,
  createEncounter,
  getEncounterByAppointment,
  updateEncounter,
} from '../services/encounterService'
import { getAppointment } from '../services/appointmentService'
import { listMedications } from '../services/medicationService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import { EncounterStatus, type Encounter } from '../types/encounter'
import type { Medication } from '../types/medication'
import { PageHeader } from '../components/PageHeader'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
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

const OUT = 'out' // giá trị sentinel cho "ngoài danh mục" (Select không nhận value rỗng)

const itemSchema = z.object({
  medicationId: z.string().nullable(),
  drugName: z.string(),
  dosage: z.string(),
  quantity: z.number(),
  instruction: z.string().nullable(),
})
const schema = z.object({
  symptoms: z.string(),
  diagnosis: z.string().min(1, 'Vui lòng nhập chẩn đoán.'),
  notes: z.string(),
  prescriptionItems: z.array(itemSchema),
})
type FormValues = z.infer<typeof schema>

export default function EncounterFormPage() {
  const { appointmentId } = useParams<{ appointmentId: string }>()
  const navigate = useNavigate()

  const [encounter, setEncounter] = useState<Encounter | null>(null)
  const [patientName, setPatientName] = useState('')
  const [doctorName, setDoctorName] = useState('')
  const [medications, setMedications] = useState<Medication[]>([])
  const [loading, setLoading] = useState(true)

  const isCompleted = encounter?.status === EncounterStatus.Completed

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { symptoms: '', diagnosis: '', notes: '', prescriptionItems: [] },
  })
  const { register, handleSubmit, control, watch, setValue, reset, formState } = form
  const { fields, append, remove } = useFieldArray({ control, name: 'prescriptionItems' })
  const items = watch('prescriptionItems')

  const medMap = useMemo(() => new Map(medications.map((m) => [m.id, m])), [medications])

  useEffect(() => {
    if (!appointmentId) return
    let active = true
    void (async () => {
      try {
        try {
          const meds = await listMedications({ page: 1, pageSize: 100 })
          if (active) setMedications(meds.items)
        } catch {
          // Không có quyền đọc danh mục / lỗi tải: vẫn cho kê đơn gõ tay.
        }
        const appt = await getAppointment(appointmentId)
        if (active) {
          setPatientName(appt.patientName ?? '—')
          setDoctorName(appt.doctorName ?? '—')
        }
        const existing = await getEncounterByAppointment(appointmentId)
        if (active && existing) {
          setEncounter(existing)
          reset({
            symptoms: existing.symptoms ?? '',
            diagnosis: existing.diagnosis,
            notes: existing.notes ?? '',
            prescriptionItems: existing.prescriptionItems,
          })
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
  }, [appointmentId, reset])

  const buildValues = (values: FormValues) => ({
    symptoms: values.symptoms.trim() || null,
    diagnosis: values.diagnosis.trim(),
    notes: values.notes.trim() || null,
    prescriptionItems: values.prescriptionItems.map((it) => ({
      medicationId: it.medicationId,
      drugName: it.drugName.trim(),
      dosage: it.dosage.trim(),
      quantity: Number(it.quantity),
      instruction: it.instruction?.trim() || null,
    })),
  })

  const onSubmit = handleSubmit(async (values) => {
    if (!appointmentId) return
    try {
      const saved = encounter
        ? await updateEncounter(encounter.id, buildValues(values))
        : await createEncounter({ appointmentId, ...buildValues(values) })
      setEncounter(saved)
      reset({
        symptoms: saved.symptoms ?? '',
        diagnosis: saved.diagnosis,
        notes: saved.notes ?? '',
        prescriptionItems: saved.prescriptionItems,
      })
      toastSuccess(encounter ? 'Đã lưu phiếu khám.' : 'Đã tạo phiếu khám.')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  const onComplete = async () => {
    if (!encounter) return
    try {
      await completeEncounter(encounter.id)
      toastSuccess('Đã chốt phiếu khám.')
      navigate('/appointments')
    } catch (err) {
      toastError(err)
    }
  }

  // Chọn thuốc từ danh mục: điền sẵn tên thuốc (vẫn cho sửa tay).
  const onSelectMedication = (index: number, value: string) => {
    if (value === OUT) {
      setValue(`prescriptionItems.${index}.medicationId`, null)
      return
    }
    const med = medMap.get(value)
    setValue(`prescriptionItems.${index}.medicationId`, value)
    if (med) setValue(`prescriptionItems.${index}.drugName`, med.name)
  }

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  const hasLinked = items.some((it) => it.medicationId)
  const completeMessage = hasLinked
    ? 'Thuốc trong danh mục sẽ được cấp phát (trừ tồn theo hạn dùng gần nhất). Nếu không đủ tồn, việc chốt sẽ bị huỷ. Sau khi chốt sẽ không sửa được.'
    : 'Sau khi chốt sẽ không sửa được và lịch khám chuyển sang Hoàn tất.'

  return (
    <section className="mx-auto max-w-4xl">
      <PageHeader
        title="Phiếu khám"
        description={
          <>
            Bệnh nhân: <strong className="text-foreground">{patientName}</strong> · Bác sĩ:{' '}
            <strong className="text-foreground">{doctorName}</strong>
            {isCompleted && ' · Đã chốt'}
            {encounter?.dispensedAt && ' · Đã cấp phát thuốc'}
          </>
        }
      />

      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <Card>
          <CardContent className="flex flex-col gap-4">
            <div className="grid gap-2">
              <Label htmlFor="symptoms">Triệu chứng</Label>
              <Textarea id="symptoms" rows={2} disabled={isCompleted} {...register('symptoms')} />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="diagnosis">Chẩn đoán *</Label>
              <Textarea id="diagnosis" rows={2} disabled={isCompleted} {...register('diagnosis')} />
              {formState.errors.diagnosis && (
                <p className="text-sm text-destructive">{formState.errors.diagnosis.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="notes">Chỉ định / Ghi chú</Label>
              <Textarea id="notes" rows={2} disabled={isCompleted} {...register('notes')} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-0">
            <div className="flex items-center justify-between px-6 py-3">
              <h3 className="font-semibold">Đơn thuốc</h3>
              {!isCompleted && (
                <Button
                  type="button"
                  size="sm"
                  variant="outline"
                  onClick={() =>
                    append({ medicationId: null, drugName: '', dosage: '', quantity: 1, instruction: null })
                  }
                >
                  <Plus className="size-4" />
                  Thêm thuốc
                </Button>
              )}
            </div>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[220px]">Danh mục (để trừ tồn)</TableHead>
                  <TableHead>Tên thuốc</TableHead>
                  <TableHead className="w-[110px]">Liều</TableHead>
                  <TableHead className="w-[100px]">Số lượng</TableHead>
                  <TableHead>Cách dùng</TableHead>
                  <TableHead className="w-[60px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {fields.length === 0 && (
                  <TableRow>
                    <TableCell colSpan={6} className="h-16 text-center text-muted-foreground">
                      Chưa có thuốc nào.
                    </TableCell>
                  </TableRow>
                )}
                {fields.map((f, i) => {
                  const row = items[i]
                  const med = row?.medicationId ? medMap.get(row.medicationId) : undefined
                  const notEnough = med != null && Number(row.quantity) > med.stockOnHand
                  return (
                    <TableRow key={f.id}>
                      <TableCell className="align-top">
                        <Select
                          value={row?.medicationId ?? OUT}
                          disabled={isCompleted}
                          onValueChange={(v) => onSelectMedication(i, v)}
                        >
                          <SelectTrigger className="w-full">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value={OUT}>— Ngoài danh mục —</SelectItem>
                            {medications.map((m) => (
                              <SelectItem key={m.id} value={m.id}>
                                {m.name} (tồn {m.stockOnHand})
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        {med && (
                          <p className={notEnough ? 'mt-1 text-xs text-destructive' : 'mt-1 text-xs text-muted-foreground'}>
                            Tồn khả dụng: {med.stockOnHand} {med.unit}
                            {notEnough && ' — không đủ để cấp phát'}
                          </p>
                        )}
                      </TableCell>
                      <TableCell className="align-top">
                        <Input disabled={isCompleted} {...register(`prescriptionItems.${i}.drugName`)} />
                      </TableCell>
                      <TableCell className="align-top">
                        <Input disabled={isCompleted} {...register(`prescriptionItems.${i}.dosage`)} />
                      </TableCell>
                      <TableCell className="align-top">
                        <Input
                          type="number"
                          min={1}
                          disabled={isCompleted}
                          {...register(`prescriptionItems.${i}.quantity`, { valueAsNumber: true })}
                        />
                      </TableCell>
                      <TableCell className="align-top">
                        <Input disabled={isCompleted} {...register(`prescriptionItems.${i}.instruction`)} />
                      </TableCell>
                      <TableCell className="align-top">
                        {!isCompleted && (
                          <Button
                            type="button"
                            size="icon"
                            variant="ghost"
                            className="text-destructive hover:text-destructive"
                            onClick={() => remove(i)}
                          >
                            <Trash2 className="size-4" />
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </CardContent>
        </Card>

        <div className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate('/appointments')}
            disabled={formState.isSubmitting}
          >
            <ArrowLeft className="size-4" />
            Quay lại
          </Button>
          {!isCompleted && (
            <Button type="submit" disabled={formState.isSubmitting}>
              {formState.isSubmitting ? 'Đang lưu…' : encounter ? 'Lưu' : 'Tạo phiếu'}
            </Button>
          )}
          {encounter && !isCompleted && (
            <ConfirmDialog
              trigger={
                <Button type="button" disabled={formState.isSubmitting}>
                  <CheckCircle2 className="size-4" />
                  Chốt phiếu
                </Button>
              }
              title="Chốt phiếu khám?"
              description={completeMessage}
              confirmText="Chốt phiếu"
              onConfirm={() => void onComplete()}
            />
          )}
        </div>
      </form>
    </section>
  )
}
