import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { createMedication, getMedication, updateMedication } from '../services/medicationService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import { PageHeader } from '../components/PageHeader'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent } from '@/components/ui/card'

const schema = z.object({
  name: z.string().min(1, 'Vui lòng nhập tên thuốc.'),
  activeIngredient: z.string().min(1, 'Vui lòng nhập hoạt chất.'),
  unit: z.string().min(1, 'Vui lòng nhập đơn vị tính.'),
  reorderLevel: z.number().min(0),
  description: z.string(),
})
type FormValues = z.infer<typeof schema>

export default function MedicationFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const [loading, setLoading] = useState(isEdit)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', activeIngredient: '', unit: '', reorderLevel: 0, description: '' },
  })
  const { register, handleSubmit, reset, formState } = form
  const errors = formState.errors

  useEffect(() => {
    if (!id) return
    let active = true
    void (async () => {
      try {
        const m = await getMedication(id)
        if (active)
          reset({
            name: m.name,
            activeIngredient: m.activeIngredient,
            unit: m.unit,
            reorderLevel: m.reorderLevel,
            description: m.description ?? '',
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
      name: values.name.trim(),
      activeIngredient: values.activeIngredient.trim(),
      unit: values.unit.trim(),
      reorderLevel: Number(values.reorderLevel),
      description: values.description.trim() || null,
    }
    try {
      if (isEdit && id) {
        await updateMedication(id, payload)
      } else {
        await createMedication(payload)
      }
      toastSuccess(isEdit ? 'Đã cập nhật thuốc.' : 'Đã thêm thuốc.')
      navigate('/medications')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  return (
    <section className="mx-auto max-w-xl">
      <PageHeader title={isEdit ? 'Sửa thuốc' : 'Thêm thuốc'} />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
            <div className="grid gap-2">
              <Label htmlFor="name">Tên thuốc *</Label>
              <Input id="name" {...register('name')} />
              {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="activeIngredient">Hoạt chất *</Label>
              <Input id="activeIngredient" {...register('activeIngredient')} />
              {errors.activeIngredient && (
                <p className="text-sm text-destructive">{errors.activeIngredient.message}</p>
              )}
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="grid gap-2">
                <Label htmlFor="unit">Đơn vị tính *</Label>
                <Input id="unit" placeholder="viên / vỉ / chai / ống…" {...register('unit')} />
                {errors.unit && <p className="text-sm text-destructive">{errors.unit.message}</p>}
              </div>
              <div className="grid gap-2">
                <Label htmlFor="reorderLevel">Ngưỡng tồn tối thiểu</Label>
                <Input
                  id="reorderLevel"
                  type="number"
                  min={0}
                  {...register('reorderLevel', { valueAsNumber: true })}
                />
                {errors.reorderLevel && (
                  <p className="text-sm text-destructive">{errors.reorderLevel.message}</p>
                )}
              </div>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="description">Mô tả</Label>
              <Textarea id="description" rows={2} {...register('description')} />
            </div>
            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate('/medications')}
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
