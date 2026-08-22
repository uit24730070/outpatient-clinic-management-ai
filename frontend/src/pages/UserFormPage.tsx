import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import {
  createUser,
  getUser,
  linkUserToDoctor,
  unlinkUserFromDoctor,
  updateUser,
} from '../services/userService'
import { listDoctors } from '../services/doctorService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import { UserRole, roleLabels, type UserRoleValue } from '../types/auth'
import type { Doctor } from '../types/doctor'
import { PageHeader } from '../components/PageHeader'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

const UNLINKED = 'none'

function makeSchema(isEdit: boolean) {
  return z.object({
    username: isEdit ? z.string() : z.string().min(1, 'Vui lòng nhập tên đăng nhập.'),
    password: isEdit ? z.string() : z.string().min(1, 'Vui lòng nhập mật khẩu.'),
    fullName: z.string().min(1, 'Vui lòng nhập họ tên.'),
    role: z.string(),
    email: z.string(),
  })
}
type FormValues = z.infer<ReturnType<typeof makeSchema>>

export default function UserFormPage() {
  const { id } = useParams<{ id: string }>()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const [loading, setLoading] = useState(true)

  const schema = useMemo(() => makeSchema(isEdit), [isEdit])
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { username: '', password: '', fullName: '', role: UserRole.Receptionist, email: '' },
  })
  const { register, handleSubmit, watch, setValue, reset, formState } = form
  const errors = formState.errors
  const role = watch('role')

  // Trạng thái gắn hồ sơ bác sĩ (chỉ dùng khi edit + role Doctor).
  const [doctors, setDoctors] = useState<Doctor[]>([])
  const [linkedDoctorId, setLinkedDoctorId] = useState<string | null>(null)
  const [selectedDoctorId, setSelectedDoctorId] = useState<string>('')
  const [linkBusy, setLinkBusy] = useState(false)

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const doctorPage = await listDoctors({ page: 1, pageSize: 100 })
        if (active) setDoctors(doctorPage.items)
        if (id) {
          const u = await getUser(id)
          if (active) {
            reset({ username: u.username, password: '', fullName: u.fullName, role: u.role, email: u.email ?? '' })
            setLinkedDoctorId(u.doctorId)
            setSelectedDoctorId(u.doctorId ?? '')
          }
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
    try {
      if (isEdit && id) {
        await updateUser(id, {
          fullName: values.fullName,
          role: values.role as UserRoleValue,
          email: values.email || null,
        })
      } else {
        await createUser({
          username: values.username,
          password: values.password,
          fullName: values.fullName,
          role: values.role as UserRoleValue,
          email: values.email || null,
        })
      }
      toastSuccess(isEdit ? 'Đã cập nhật người dùng.' : 'Đã thêm người dùng.')
      navigate('/users')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  // Áp dụng gắn/gỡ liên kết ngay lập tức (tách khỏi lưu hồ sơ để tránh ràng buộc thứ tự).
  const onApplyLink = async () => {
    if (!id) return
    if (selectedDoctorId === (linkedDoctorId ?? '')) return
    setLinkBusy(true)
    try {
      if (linkedDoctorId) await unlinkUserFromDoctor(linkedDoctorId)
      if (selectedDoctorId) await linkUserToDoctor(selectedDoctorId, id)
      setLinkedDoctorId(selectedDoctorId || null)
      const doctorPage = await listDoctors({ page: 1, pageSize: 100 })
      setDoctors(doctorPage.items)
      toastSuccess('Đã cập nhật liên kết hồ sơ bác sĩ.')
    } catch (err) {
      toastError(err)
    } finally {
      setLinkBusy(false)
    }
  }

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  // Chọn được: bác sĩ chưa gắn tài khoản, hoặc bác sĩ đang gắn chính user này.
  const linkableDoctors = doctors.filter((d) => d.userId === null || d.id === linkedDoctorId)

  return (
    <section className="mx-auto max-w-xl space-y-6">
      <PageHeader title={isEdit ? 'Sửa người dùng' : 'Thêm người dùng'} />

      <Card>
        <CardContent>
          <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
            {!isEdit && (
              <>
                <div className="grid gap-2">
                  <Label htmlFor="username">Tên đăng nhập *</Label>
                  <Input id="username" {...register('username')} />
                  {errors.username && (
                    <p className="text-sm text-destructive">{errors.username.message}</p>
                  )}
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="password">Mật khẩu *</Label>
                  <Input id="password" type="password" {...register('password')} />
                  {errors.password && (
                    <p className="text-sm text-destructive">{errors.password.message}</p>
                  )}
                </div>
              </>
            )}

            <div className="grid gap-2">
              <Label htmlFor="fullName">Họ tên *</Label>
              <Input id="fullName" {...register('fullName')} />
              {errors.fullName && (
                <p className="text-sm text-destructive">{errors.fullName.message}</p>
              )}
            </div>

            <div className="grid gap-2">
              <Label>Vai trò *</Label>
              <Select value={role} onValueChange={(v) => setValue('role', v)}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.values(UserRole).map((r) => (
                    <SelectItem key={r} value={r}>
                      {roleLabels[r]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.role && <p className="text-sm text-destructive">{errors.role.message}</p>}
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
                onClick={() => navigate('/users')}
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

      {/* Gắn hồ sơ bác sĩ — chỉ khi đã có tài khoản (edit) và vai trò Bác sĩ. */}
      {isEdit && role === UserRole.Doctor && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Hồ sơ bác sĩ</CardTitle>
            <CardDescription>
              Gắn tài khoản này với một hồ sơ bác sĩ để bác sĩ thấy "Phòng khám của tôi".
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <div className="grid gap-2">
              <Label>Hồ sơ bác sĩ</Label>
              <Select
                value={selectedDoctorId || UNLINKED}
                onValueChange={(v) => setSelectedDoctorId(v === UNLINKED ? '' : v)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={UNLINKED}>— Không gắn —</SelectItem>
                  {linkableDoctors.map((d) => (
                    <SelectItem key={d.id} value={d.id}>
                      {d.code} · {d.fullName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex justify-end">
              <Button
                type="button"
                onClick={() => void onApplyLink()}
                disabled={linkBusy || selectedDoctorId === (linkedDoctorId ?? '')}
              >
                {linkBusy ? 'Đang áp dụng…' : 'Áp dụng liên kết'}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}
    </section>
  )
}
