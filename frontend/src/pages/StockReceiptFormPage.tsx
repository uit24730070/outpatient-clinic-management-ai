import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm, useFieldArray } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Plus, Trash2 } from 'lucide-react'
import { createStockReceipt } from '../services/stockReceiptService'
import { listMedications } from '../services/medicationService'
import { applyServerErrors } from '../lib/form'
import { toastError, toastSuccess } from '../lib/toast'
import type { Medication } from '../types/medication'
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const today = () => new Date().toISOString().slice(0, 10)

const itemSchema = z.object({
  medicationId: z.string().min(1, 'Chọn thuốc'),
  batchNumber: z.string(),
  expiryDate: z.string(),
  quantity: z.number(),
  unitCost: z.string(),
})
const schema = z.object({
  supplierName: z.string().min(1, 'Vui lòng nhập nhà cung cấp.'),
  receivedAt: z.string().min(1, 'Vui lòng chọn ngày nhận.'),
  note: z.string(),
  items: z.array(itemSchema).min(1, 'Cần ít nhất một dòng nhập.'),
})
type FormValues = z.infer<typeof schema>

const emptyItem = () => ({ medicationId: '', batchNumber: '', expiryDate: '', quantity: 1, unitCost: '' })

export default function StockReceiptFormPage() {
  const navigate = useNavigate()
  const [medications, setMedications] = useState<Medication[]>([])
  const [loading, setLoading] = useState(true)

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { supplierName: '', receivedAt: today(), note: '', items: [emptyItem()] },
  })
  const { register, handleSubmit, control, watch, setValue, formState } = form
  const { fields, append, remove } = useFieldArray({ control, name: 'items' })
  const items = watch('items')

  useEffect(() => {
    let active = true
    void (async () => {
      try {
        const page = await listMedications({ page: 1, pageSize: 100 })
        if (active) setMedications(page.items)
      } catch (err) {
        toastError(err)
      } finally {
        if (active) setLoading(false)
      }
    })()
    return () => {
      active = false
    }
  }, [])

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createStockReceipt({
        supplierName: values.supplierName.trim(),
        receivedAt: new Date(values.receivedAt).toISOString(),
        note: values.note.trim() || null,
        items: values.items.map((it) => ({
          medicationId: it.medicationId,
          batchNumber: it.batchNumber.trim(),
          expiryDate: it.expiryDate,
          quantity: Number(it.quantity),
          unitCost: it.unitCost === '' || Number.isNaN(Number(it.unitCost)) ? null : Number(it.unitCost),
        })),
      })
      toastSuccess('Đã tạo phiếu nhập kho.')
      navigate('/stock-receipts')
    } catch (err) {
      applyServerErrors(form, err)
    }
  })

  if (loading) return <p className="text-muted-foreground">Đang tải…</p>

  const errors = formState.errors

  return (
    <section className="mx-auto max-w-4xl">
      <PageHeader title="Phiếu nhập kho" />

      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        <Card>
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="supplierName">Nhà cung cấp *</Label>
              <Input id="supplierName" {...register('supplierName')} />
              {errors.supplierName && (
                <p className="text-sm text-destructive">{errors.supplierName.message}</p>
              )}
            </div>
            <div className="grid gap-2">
              <Label htmlFor="receivedAt">Ngày nhận *</Label>
              <Input id="receivedAt" type="date" {...register('receivedAt')} />
              {errors.receivedAt && (
                <p className="text-sm text-destructive">{errors.receivedAt.message}</p>
              )}
            </div>
            <div className="grid gap-2 sm:col-span-2">
              <Label htmlFor="note">Ghi chú</Label>
              <Textarea id="note" rows={2} {...register('note')} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-0">
            <div className="flex items-center justify-between px-6 py-3">
              <div>
                <h3 className="font-semibold">Dòng nhập *</h3>
                {errors.items?.message && (
                  <p className="text-sm text-destructive">{errors.items.message}</p>
                )}
              </div>
              <Button type="button" size="sm" variant="outline" onClick={() => append(emptyItem())}>
                <Plus className="size-4" />
                Thêm dòng
              </Button>
            </div>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[220px]">Thuốc</TableHead>
                  <TableHead>Số lô</TableHead>
                  <TableHead className="w-[150px]">Hạn dùng</TableHead>
                  <TableHead className="w-[100px]">Số lượng</TableHead>
                  <TableHead className="w-[110px]">Đơn giá</TableHead>
                  <TableHead className="w-[60px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {fields.map((f, i) => (
                  <TableRow key={f.id}>
                    <TableCell className="align-top">
                      <Select
                        value={items[i]?.medicationId || undefined}
                        onValueChange={(v) => setValue(`items.${i}.medicationId`, v, { shouldValidate: true })}
                      >
                        <SelectTrigger className="w-full">
                          <SelectValue placeholder="— Chọn thuốc —" />
                        </SelectTrigger>
                        <SelectContent>
                          {medications.map((m) => (
                            <SelectItem key={m.id} value={m.id}>
                              {m.code} · {m.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      {errors.items?.[i]?.medicationId && (
                        <p className="mt-1 text-xs text-destructive">
                          {errors.items[i]?.medicationId?.message}
                        </p>
                      )}
                    </TableCell>
                    <TableCell className="align-top">
                      <Input {...register(`items.${i}.batchNumber`)} />
                    </TableCell>
                    <TableCell className="align-top">
                      <Input type="date" {...register(`items.${i}.expiryDate`)} />
                    </TableCell>
                    <TableCell className="align-top">
                      <Input
                        type="number"
                        min={1}
                        {...register(`items.${i}.quantity`, { valueAsNumber: true })}
                      />
                    </TableCell>
                    <TableCell className="align-top">
                      <Input type="number" min={0} step="0.01" {...register(`items.${i}.unitCost`)} />
                    </TableCell>
                    <TableCell className="align-top">
                      {fields.length > 1 && (
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
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>

        <div className="flex justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate('/stock-receipts')}
            disabled={formState.isSubmitting}
          >
            Huỷ
          </Button>
          <Button type="submit" disabled={formState.isSubmitting}>
            {formState.isSubmitting ? 'Đang lưu…' : 'Tạo phiếu nhập'}
          </Button>
        </div>
      </form>
    </section>
  )
}
