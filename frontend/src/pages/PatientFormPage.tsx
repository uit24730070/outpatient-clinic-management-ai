import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { createPatient, getPatient, updatePatient } from '../services/patientService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import { Gender, genderLabels, type GenderValue } from '../types/patient'
import { PageHeader } from '../components/PageHeader'
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

const schema = z.object({
  fullName: z.string().min(1, 'Vui lòng nhập họ tên.'),
  gender: z.number(),
  dateOfBirth: z.string(),
  phoneNumber: z.string(),
  address: z.string(),
})
type FormValues = z.infer<typeof schema>

export default function PatientFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const [loading, setLoading] = useState(isEdit)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { fullName: '', gender: Gender.Unknown, dateOfBirth: '', phoneNumber: '', address: '' },
  })
  const { register, handleSubmit, watch, setValue, reset, formState } = form
  const errors = formState.errors

  useEffect(() => {
    if (!id) return
    let active = true
    void (async () => {
      try {
        const p = await getPatient(id)
        if (!active) return
        reset({
          fullName: p.fullName,
          gender: p.gender,
          dateOfBirth: p.dateOfBirth ?? '',
          phoneNumber: p.phoneNumber ?? '',
          address: p.address ?? '',
        })
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
      gender: values.gender as GenderValue,
      dateOfBirth: values.dateOfBirth || null,
      phoneNumber: values.phoneNumber.trim() || null,
      address: values.address.trim() || null,
    }
    try {
      if (isEdit && id) {
        await updatePatient(id, payload)
      } else {
        await createPatient(payload)
      }
      toastSuccess(isEdit ? 'Đã cập nhật bệnh nhân.' : 'Đã thêm bệnh nhân.')
      navigate('/patients')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  return (
    <section className="mx-auto max-w-xl">
      <PageHeader title={isEdit ? 'Sửa bệnh nhân' : 'Thêm bệnh nhân'} />

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

            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label>Giới tính</Label>
                <Select
                  value={String(watch('gender'))}
                  onValueChange={(v) => setValue('gender', Number(v))}
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
                <Label htmlFor="dateOfBirth">Ngày sinh</Label>
                <Input id="dateOfBirth" type="date" {...register('dateOfBirth')} />
                {errors.dateOfBirth && (
                  <p className="text-sm text-destructive">{errors.dateOfBirth.message}</p>
                )}
              </div>
            </div>

            <div className="grid gap-2">
              <Label htmlFor="phoneNumber">Số điện thoại</Label>
              <Input id="phoneNumber" {...register('phoneNumber')} />
              {errors.phoneNumber && (
                <p className="text-sm text-destructive">{errors.phoneNumber.message}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label htmlFor="address">Địa chỉ</Label>
              <Textarea id="address" rows={2} {...register('address')} />
              {errors.address && (
                <p className="text-sm text-destructive">{errors.address.message}</p>
              )}
            </div>

            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate('/patients')}
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
