import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { createSpecialty, getSpecialty, updateSpecialty } from '../services/specialtyService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import { PageHeader } from '../components/PageHeader'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent } from '@/components/ui/card'

const schema = z.object({
  name: z.string().min(1, 'Vui lòng nhập tên chuyên khoa.'),
  description: z.string(),
})
type FormValues = z.infer<typeof schema>

export default function SpecialtyFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const [loading, setLoading] = useState(isEdit)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { name: '', description: '' },
  })
  const { register, handleSubmit, reset, formState } = form
  const errors = formState.errors

  useEffect(() => {
    if (!id) return
    let active = true
    void (async () => {
      try {
        const s = await getSpecialty(id)
        if (active) reset({ name: s.name, description: s.description ?? '' })
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
    const payload = { name: values.name.trim(), description: values.description.trim() || null }
    try {
      if (isEdit && id) {
        await updateSpecialty(id, payload)
      } else {
        await createSpecialty(payload)
      }
      toastSuccess(isEdit ? 'Đã cập nhật chuyên khoa.' : 'Đã thêm chuyên khoa.')
      navigate('/specialties')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  return (
    <section className="mx-auto max-w-xl">
      <PageHeader title={isEdit ? 'Sửa chuyên khoa' : 'Thêm chuyên khoa'} />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
            <div className="grid gap-2">
              <Label htmlFor="name">Tên chuyên khoa *</Label>
              <Input id="name" {...register('name')} />
              {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="description">Mô tả</Label>
              <Textarea id="description" rows={3} {...register('description')} />
              {errors.description && (
                <p className="text-sm text-destructive">{errors.description.message}</p>
              )}
            </div>
            <div className="flex justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate('/specialties')}
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
