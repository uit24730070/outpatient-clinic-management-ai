import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { createPatient } from '../services/patientService'
import { applyServerErrors } from '../lib/form'
import { toastSuccess } from '../lib/toast'
import { Gender, genderLabels, type Patient } from '../types/patient'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
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

/**
 * Hộp thoại tạo nhanh bệnh nhân mới ngay trong luồng khác (vd tiếp đón), tránh
 * lễ tân phải rời màn sang `/patients/new` rồi quay lại chọn lại bệnh nhân.
 * Dùng chung 1 request `createPatient` với `PatientFormPage`.
 */
export function PatientQuickCreateDialog({
  open,
  onOpenChange,
  onCreated,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  onCreated: (patient: Patient) => void
}) {
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { fullName: '', gender: Gender.Unknown, dateOfBirth: '', phoneNumber: '', address: '' },
  })
  const { register, handleSubmit, watch, setValue, reset, formState } = form
  const errors = formState.errors

  const onSubmit = handleSubmit(async (values) => {
    try {
      const patient = await createPatient({
        fullName: values.fullName.trim(),
        gender: values.gender as Patient['gender'],
        dateOfBirth: values.dateOfBirth || null,
        phoneNumber: values.phoneNumber.trim() || null,
        address: values.address.trim() || null,
      })
      toastSuccess(`Đã thêm bệnh nhân ${patient.fullName}.`)
      reset()
      onCreated(patient)
      onOpenChange(false)
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (!next) reset()
        onOpenChange(next)
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Thêm bệnh nhân mới</DialogTitle>
        </DialogHeader>

        <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
          <div className="grid gap-2">
            <Label htmlFor="qc-fullName">Họ tên *</Label>
            <Input id="qc-fullName" {...register('fullName')} />
            {errors.fullName && <p className="text-sm text-destructive">{errors.fullName.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="grid gap-2">
              <Label>Giới tính</Label>
              <Select value={String(watch('gender'))} onValueChange={(v) => setValue('gender', Number(v))}>
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
              <Label htmlFor="qc-dateOfBirth">Ngày sinh</Label>
              <Input id="qc-dateOfBirth" type="date" {...register('dateOfBirth')} />
              {errors.dateOfBirth && (
                <p className="text-sm text-destructive">{errors.dateOfBirth.message}</p>
              )}
            </div>
          </div>

          <div className="grid gap-2">
            <Label htmlFor="qc-phoneNumber">Số điện thoại</Label>
            <Input id="qc-phoneNumber" {...register('phoneNumber')} />
            {errors.phoneNumber && (
              <p className="text-sm text-destructive">{errors.phoneNumber.message}</p>
            )}
          </div>

          <div className="grid gap-2">
            <Label htmlFor="qc-address">Địa chỉ</Label>
            <Input id="qc-address" {...register('address')} />
            {errors.address && <p className="text-sm text-destructive">{errors.address.message}</p>}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={formState.isSubmitting}>
              Huỷ
            </Button>
            <Button type="submit" disabled={formState.isSubmitting}>
              {formState.isSubmitting ? 'Đang lưu…' : 'Thêm bệnh nhân'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
