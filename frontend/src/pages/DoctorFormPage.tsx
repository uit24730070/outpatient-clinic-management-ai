import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { createDoctor, getDoctor, updateDoctor } from '../services/doctorService'
import { listSpecialties } from '../services/specialtyService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import type { Specialty } from '../types/specialty'
import { PageHeader } from '../components/PageHeader'
import { DoctorScheduleManager } from '../components/DoctorScheduleManager'
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

const schema = z.object({
  fullName: z.string().min(1, 'Vui lòng nhập họ tên.'),
  specialtyId: z.string().min(1, 'Vui lòng chọn chuyên khoa.'),
  phoneNumber: z.string(),
  email: z.string(),
})
type FormValues = z.infer<typeof schema>

export default function DoctorFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const [specialties, setSpecialties] = useState<Specialty[]>([])
  const [loading, setLoading] = useState(true)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { fullName: '', specialtyId: '', phoneNumber: '', email: '' },
  })
  const { register, handleSubmit, watch, setValue, reset, formState } = form
  const errors = formState.errors

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const specialtyPage = await listSpecialties({ page: 1, pageSize: 100 })
        if (active) setSpecialties(specialtyPage.items)
        if (id) {
          const d = await getDoctor(id)
          if (active)
            reset({
              fullName: d.fullName,
              specialtyId: d.specialtyId,
              phoneNumber: d.phoneNumber ?? '',
              email: d.email ?? '',
            })
        }
      } catch (err) {
        toastError(err)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => {
      active = false
    }
  }, [id, reset])

  const onSubmit = handleSubmit(async (values) => {
    const payload = {
      fullName: values.fullName.trim(),
      specialtyId: values.specialtyId,
      phoneNumber: values.phoneNumber.trim() || null,
      email: values.email.trim() || null,
    }
    try {
      if (isEdit && id) {
        await updateDoctor(id, payload)
      } else {
        await createDoctor(payload)
      }
      toastSuccess(isEdit ? 'Đã cập nhật bác sĩ.' : 'Đã thêm bác sĩ.')
      navigate('/doctors')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  return (
    <section className={`mx-auto ${isEdit ? 'max-w-3xl' : 'max-w-xl'}`}>
      <PageHeader title={isEdit ? 'Sửa bác sĩ' : 'Thêm bác sĩ'} />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
            <div className="grid gap-2">
              <Label htmlFor="fullName">Họ tên *</Label>
              <Input id="fullName" {...register('fullName')} />
              {errors.fullName && (
                <p className="text-sm text-destructive">{errors.fullName.message}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label>Chuyên khoa *</Label>
              <Select
                value={watch('specialtyId')}
                onValueChange={(v) => setValue('specialtyId', v, { shouldValidate: true })}
              >
                <SelectTrigger>
                  <SelectValue placeholder="— Chọn chuyên khoa —" />
                </SelectTrigger>
                <SelectContent>
                  {specialties.map((s) => (
                    <SelectItem key={s.id} value={s.id}>
                      {s.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.specialtyId && (
                <p className="text-sm text-destructive">{errors.specialtyId.message}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="phoneNumber">Số điện thoại</Label>
              <Input id="phoneNumber" {...register('phoneNumber')} />
              {errors.phoneNumber && (
                <p className="text-sm text-destructive">{errors.phoneNumber.message}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" {...register('email')} />
              {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
            </div>

            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate('/doctors')}
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

      {isEdit && id && <DoctorScheduleManager doctorId={id} />}
    </section>
  )
}
