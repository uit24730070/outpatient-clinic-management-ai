import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { createRoom, getRoom, updateRoom } from '../services/roomService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import { PageHeader } from '../components/PageHeader'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { Card, CardContent } from '@/components/ui/card'

const schema = z.object({
  name: z.string().min(1, 'Vui lòng nhập tên phòng.'),
  description: z.string(),
})
type FormValues = z.infer<typeof schema>

export default function RoomFormPage() {
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
        const r = await getRoom(id)
        if (active) reset({ name: r.name, description: r.description ?? '' })
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
        await updateRoom(id, payload)
      } else {
        await createRoom(payload)
      }
      toastSuccess(isEdit ? 'Đã cập nhật phòng khám.' : 'Đã thêm phòng khám.')
      navigate('/rooms')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  return (
    <section className="mx-auto max-w-xl">
      <PageHeader title={isEdit ? 'Sửa phòng khám' : 'Thêm phòng khám'} />
      <Card>
        <CardContent>
          <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
            <div className="grid gap-2">
              <Label htmlFor="name">Tên phòng *</Label>
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
                onClick={() => navigate('/rooms')}
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
