import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  createServicePrice,
  getServicePrice,
  updateServicePrice,
} from '../services/servicePriceService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
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
import { ServiceCategory, serviceCategoryLabels, type ServiceCategoryValue } from '../types/invoice'

const schema = z.object({
  name: z.string().min(1, 'Vui lòng nhập tên dịch vụ.'),
  unitPrice: z.number().min(0, 'Đơn giá không được âm.'),
  description: z.string(),
  category: z.number(),
})
type FormValues = z.infer<typeof schema>

export default function ServicePriceFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const [loading, setLoading] = useState(isEdit)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', unitPrice: 0, description: '', category: ServiceCategory.Consultation },
  })
  const { register, handleSubmit, reset, watch, setValue, formState } = form
  const errors = formState.errors
  const category = watch('category') as ServiceCategoryValue

  useEffect(() => {
    if (!id) return
    let active = true
    void (async () => {
      try {
        const s = await getServicePrice(id)
        if (active)
          reset({
            name: s.name,
            unitPrice: s.unitPrice,
            description: s.description ?? '',
            category: s.category,
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
      unitPrice: Number(values.unitPrice),
      description: values.description.trim() || null,
      category: values.category as ServiceCategoryValue,
    }
    try {
      if (isEdit && id) {
        await updateServicePrice(id, payload)
      } else {
        await createServicePrice(payload)
      }
      toastSuccess(isEdit ? 'Đã cập nhật dịch vụ.' : 'Đã thêm dịch vụ.')
      navigate('/service-prices')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  return (
    <section className="mx-auto max-w-xl">
      <PageHeader title={isEdit ? 'Sửa dịch vụ' : 'Thêm dịch vụ'} />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
            <div className="grid gap-2">
              <Label htmlFor="name">Tên dịch vụ *</Label>
              <Input id="name" placeholder="Khám tổng quát / Tái khám…" {...register('name')} />
              {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="unitPrice">Đơn giá (VND) *</Label>
              <Input
                id="unitPrice"
                type="number"
                min={0}
                step={1}
                {...register('unitPrice', { valueAsNumber: true })}
              />
              {errors.unitPrice && (
                <p className="text-sm text-destructive">{errors.unitPrice.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label>Phân loại *</Label>
              <Select
                value={String(category)}
                onValueChange={(v) => setValue('category', Number(v) as ServiceCategoryValue)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.values(ServiceCategory).map((c) => (
                    <SelectItem key={c} value={String(c)}>
                      {serviceCategoryLabels[c]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-muted-foreground">
                Dịch vụ loại <strong>Cận lâm sàng</strong> mới chỉ định được trong lúc khám.
              </p>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="description">Mô tả</Label>
              <Textarea id="description" rows={2} {...register('description')} />
            </div>
            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate('/service-prices')}
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
