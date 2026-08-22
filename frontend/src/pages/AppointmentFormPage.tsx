import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { UserPlus, X } from 'lucide-react'
import {
  createAppointment,
  getAppointment,
  updateAppointment,
} from '../services/appointmentService'
import { listPatients, createPatient } from '../services/patientService'
import { listDoctors } from '../services/doctorService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import { Gender, genderLabels, type Patient, type GenderValue } from '../types/patient'
import type { Doctor } from '../types/doctor'
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

// Chuyển ISO (UTC) → giá trị cho input datetime-local (giờ địa phương).
function toLocalInput(iso: string): string {
  const d = new Date(iso)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

// Giá trị datetime-local (giờ địa phương) → ISO (UTC) để gửi backend.
function toIso(local: string): string {
  return new Date(local).toISOString()
}

const schema = z.object({
  patientId: z.string().min(1, 'Vui lòng chọn bệnh nhân.'),
  doctorId: z.string().min(1, 'Vui lòng chọn bác sĩ.'),
  startTime: z.string().min(1, 'Vui lòng chọn thời gian bắt đầu.'),
  endTime: z.string().min(1, 'Vui lòng chọn thời gian kết thúc.'),
  reason: z.string(),
})
type FormValues = z.infer<typeof schema>

const patientSchema = z.object({
  fullName: z.string().min(1, 'Vui lòng nhập họ tên.'),
  gender: z.number(),
  dateOfBirth: z.string(),
  phoneNumber: z.string(),
  address: z.string(),
})
type PatientForm = z.infer<typeof patientSchema>

export default function AppointmentFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()

  const [loading, setLoading] = useState(true)
  const [readonlyNames, setReadonlyNames] = useState<{ patient: string; doctor: string } | null>(null)
  const [patientSearch, setPatientSearch] = useState('')
  const [patients, setPatients] = useState<Patient[]>([])
  const [doctors, setDoctors] = useState<Doctor[]>([])
  const [showCreatePatient, setShowCreatePatient] = useState(false)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { patientId: '', doctorId: '', startTime: '', endTime: '', reason: '' },
  })
  const { register, handleSubmit, setValue, watch, reset, formState } = form
  const errors = formState.errors
  const patientId = watch('patientId')
  const doctorId = watch('doctorId')

  const patientForm = useForm<PatientForm>({
    resolver: zodResolver(patientSchema),
    defaultValues: { fullName: '', gender: Gender.Unknown, dateOfBirth: '', phoneNumber: '', address: '' },
  })

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const doctorPage = await listDoctors({ page: 1, pageSize: 100 })
        if (active) setDoctors(doctorPage.items)

        if (id) {
          const a = await getAppointment(id)
          if (active) {
            reset({
              patientId: a.patientId,
              doctorId: a.doctorId,
              startTime: toLocalInput(a.startTime),
              endTime: toLocalInput(a.endTime),
              reason: a.reason ?? '',
            })
            setReadonlyNames({ patient: a.patientName ?? '—', doctor: a.doctorName ?? '—' })
          }
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
  }, [id, reset])

  // Tìm kiếm bệnh nhân cho dropdown (chỉ khi tạo mới).
  useEffect(() => {
    if (isEdit) return
    let active = true
    void (async () => {
      try {
        const result = await listPatients({
          page: 1,
          pageSize: 20,
          search: patientSearch.trim() || undefined,
        })
        if (active) setPatients(result.items)
      } catch {
        // Bỏ qua lỗi tìm kiếm; người dùng thử lại.
      }
    })()
    return () => {
      active = false
    }
  }, [isEdit, patientSearch])

  // Tạo bệnh nhân rồi tự chọn vào lịch (không rời trang).
  const onCreatePatient = patientForm.handleSubmit(async (values) => {
    try {
      const created = await createPatient({
        fullName: values.fullName.trim(),
        gender: values.gender as GenderValue,
        dateOfBirth: values.dateOfBirth || null,
        phoneNumber: values.phoneNumber.trim() || null,
        address: values.address.trim() || null,
      })
      setPatients((prev) => [created, ...prev.filter((p) => p.id !== created.id)])
      setValue('patientId', created.id, { shouldValidate: true })
      setShowCreatePatient(false)
      patientForm.reset()
      setPatientSearch('')
      toastSuccess('Đã tạo bệnh nhân mới.')
    } catch (err) {
      applyServerErrors(patientForm, err)
    }
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (isEdit && id) {
        await updateAppointment(id, {
          startTime: toIso(values.startTime),
          endTime: toIso(values.endTime),
          reason: values.reason.trim() || null,
        })
      } else {
        await createAppointment({
          patientId: values.patientId,
          doctorId: values.doctorId,
          startTime: toIso(values.startTime),
          endTime: toIso(values.endTime),
          reason: values.reason.trim() || null,
        })
      }
      toastSuccess(isEdit ? 'Đã cập nhật lịch khám.' : 'Đã đặt lịch khám.')
      navigate('/appointments')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  return (
    <section className="mx-auto max-w-2xl">
      <PageHeader title={isEdit ? 'Sửa lịch khám' : 'Đặt lịch khám'} />

      <Card>
        <CardContent>
          <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
            {/* Bệnh nhân */}
            <div className="grid gap-2">
              <Label>Bệnh nhân *</Label>
              {isEdit ? (
                <Input value={readonlyNames?.patient ?? ''} disabled />
              ) : (
                <>
                  <Input
                    type="search"
                    placeholder="Tìm bệnh nhân theo tên, mã…"
                    value={patientSearch}
                    onChange={(e) => setPatientSearch(e.target.value)}
                  />
                  <Select
                    value={patientId}
                    onValueChange={(v) => setValue('patientId', v, { shouldValidate: true })}
                  >
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
                  <Button
                    type="button"
                    variant="link"
                    className="h-auto w-fit p-0"
                    onClick={() => setShowCreatePatient((v) => !v)}
                  >
                    {showCreatePatient ? (
                      <>
                        <X className="size-4" />
                        Đóng tạo mới
                      </>
                    ) : (
                      <>
                        <UserPlus className="size-4" />
                        Không tìm thấy? Tạo bệnh nhân mới
                      </>
                    )}
                  </Button>

                  {showCreatePatient && (
                    <div className="flex flex-col gap-3 rounded-lg border border-dashed bg-muted/40 p-4">
                      <p className="font-medium">Tạo nhanh bệnh nhân</p>
                      <div className="grid gap-2">
                        <Label>Họ tên *</Label>
                        <Input placeholder="Nguyễn Văn A" {...patientForm.register('fullName')} />
                        {patientForm.formState.errors.fullName && (
                          <p className="text-sm text-destructive">
                            {patientForm.formState.errors.fullName.message}
                          </p>
                        )}
                      </div>
                      <div className="grid grid-cols-2 gap-3">
                        <div className="grid gap-2">
                          <Label>Giới tính</Label>
                          <Select
                            value={String(patientForm.watch('gender'))}
                            onValueChange={(v) => patientForm.setValue('gender', Number(v))}
                          >
                            <SelectTrigger>
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              {Object.entries(genderLabels).map(([value, label]) => (
                                <SelectItem key={value} value={value}>
                                  {label}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        </div>
                        <div className="grid gap-2">
                          <Label>Ngày sinh</Label>
                          <Input type="date" {...patientForm.register('dateOfBirth')} />
                        </div>
                      </div>
                      <div className="grid gap-2">
                        <Label>Số điện thoại</Label>
                        <Input placeholder="09xxxxxxxx" {...patientForm.register('phoneNumber')} />
                      </div>
                      <div className="grid gap-2">
                        <Label>Địa chỉ</Label>
                        <Input {...patientForm.register('address')} />
                      </div>
                      <div className="flex justify-end">
                        <Button
                          type="button"
                          disabled={patientForm.formState.isSubmitting}
                          onClick={() => void onCreatePatient()}
                        >
                          Tạo & chọn bệnh nhân
                        </Button>
                      </div>
                    </div>
                  )}
                </>
              )}
              {errors.patientId && (
                <p className="text-sm text-destructive">{errors.patientId.message}</p>
              )}
            </div>

            {/* Bác sĩ */}
            <div className="grid gap-2">
              <Label>Bác sĩ *</Label>
              {isEdit ? (
                <Input value={readonlyNames?.doctor ?? ''} disabled />
              ) : (
                <Select
                  value={doctorId}
                  onValueChange={(v) => setValue('doctorId', v, { shouldValidate: true })}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="— Chọn bác sĩ —" />
                  </SelectTrigger>
                  <SelectContent>
                    {doctors.map((d) => (
                      <SelectItem key={d.id} value={d.id}>
                        {d.fullName}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
              {errors.doctorId && (
                <p className="text-sm text-destructive">{errors.doctorId.message}</p>
              )}
            </div>

            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="startTime">Bắt đầu *</Label>
                <Input id="startTime" type="datetime-local" {...register('startTime')} />
                {errors.startTime && (
                  <p className="text-sm text-destructive">{errors.startTime.message}</p>
                )}
              </div>
              <div className="grid gap-2">
                <Label htmlFor="endTime">Kết thúc *</Label>
                <Input id="endTime" type="datetime-local" {...register('endTime')} />
                {errors.endTime && (
                  <p className="text-sm text-destructive">{errors.endTime.message}</p>
                )}
              </div>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="reason">Lý do khám</Label>
              <Input id="reason" {...register('reason')} />
              {errors.reason && (
                <p className="text-sm text-destructive">{errors.reason.message}</p>
              )}
            </div>

            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate('/appointments')}
                disabled={formState.isSubmitting}
              >
                Huỷ
              </Button>
              <Button type="submit" disabled={formState.isSubmitting}>
                {formState.isSubmitting ? 'Đang lưu…' : 'Lưu'}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </section>
  )
}
