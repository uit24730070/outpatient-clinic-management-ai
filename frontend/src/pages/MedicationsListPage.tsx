import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Search, Pencil, Boxes, TriangleAlert } from 'lucide-react'
import { deleteMedication, listMedications } from '../services/medicationService'
import { useAuth } from '../store/auth'
import { canManagePharmacy } from '../config/access'
import { toastError, toastSuccess } from '../lib/toast'
import type { PagedResult } from '../types/common'
import type { Medication } from '../types/medication'
import { formatVnd } from '../lib/format'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { ConfirmDialog } from '../components/ConfirmDialog'
import { TonedBadge } from '../components/StatusBadge'
import { SortableTableHead } from '../components/SortableTableHead'
import { useSort } from '../hooks/useSort'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

const PAGE_SIZE = 10

export default function MedicationsListPage() {
  const { user } = useAuth()
  const canManage = canManagePharmacy(user?.role)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Medication> | null>(null)
  const [loading, setLoading] = useState(false)
  const { sort, toggleSort } = useSort(() => setPage(1))

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listMedications({
        page,
        pageSize: PAGE_SIZE,
        search: search.trim() || undefined,
        sortBy: sort.sortBy,
        sortDesc: sort.sortDesc,
      })
      setData(result)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [page, search, sort.sortBy, sort.sortDesc])

  useEffect(() => {
    void load()
  }, [load])

  const onSearchSubmit = (e: FormEvent) => {
    e.preventDefault()
    setPage(1)
    void load()
  }

  const onDelete = async (m: Medication) => {
    try {
      await deleteMedication(m.id)
      toastSuccess('Đã ngừng sử dụng thuốc.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  return (
    <section>
      <PageHeader
        title="Danh mục thuốc"
        description="Quản lý thuốc, tồn kho và ngưỡng cảnh báo"
        actions={
          <>
            <Button asChild variant="outline">
              <Link to="/pharmacy/alerts">
                <TriangleAlert className="size-4" />
                Cảnh báo kho
              </Link>
            </Button>
            {canManage && (
              <Button asChild>
                <Link to="/medications/new">
                  <Plus className="size-4" />
                  Thêm thuốc
                </Link>
              </Button>
            )}
          </>
        }
      />

      <Card className="mb-4">
        <CardContent>
          <form className="flex gap-2" onSubmit={onSearchSubmit}>
            <Input
              type="search"
              placeholder="Tìm theo tên, mã, hoạt chất…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <Button type="submit" variant="secondary">
              <Search className="size-4" />
              Tìm
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <SortableTableHead field="code" sort={sort} onSort={toggleSort}>Mã</SortableTableHead>
                <SortableTableHead field="name" sort={sort} onSort={toggleSort}>Tên thuốc</SortableTableHead>
                <SortableTableHead field="activeIngredient" sort={sort} onSort={toggleSort}>Hoạt chất</SortableTableHead>
                <TableHead>Đơn vị</TableHead>
                <SortableTableHead field="salePrice" sort={sort} onSort={toggleSort} className="text-right" align="right">Giá bán</SortableTableHead>
                <TableHead>Tồn</TableHead>
                <TableHead>Ngưỡng</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={8} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8} className="h-24 text-center text-muted-foreground">
                    Không có thuốc nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((m) => {
                  const low = m.stockOnHand <= m.reorderLevel
                  return (
                    <TableRow key={m.id}>
                      <TableCell className="font-mono text-sm">{m.code}</TableCell>
                      <TableCell className="font-medium">{m.name}</TableCell>
                      <TableCell>{m.activeIngredient}</TableCell>
                      <TableCell>{m.unit}</TableCell>
                      <TableCell className="text-right tabular-nums">{formatVnd(m.salePrice)}</TableCell>
                      <TableCell>
                        <span className={low ? 'font-semibold text-destructive' : undefined}>
                          {m.stockOnHand}
                        </span>
                        {low && (
                          <TonedBadge tone="red" className="ml-2">
                            Tồn thấp
                          </TonedBadge>
                        )}
                      </TableCell>
                      <TableCell>{m.reorderLevel}</TableCell>
                      <TableCell>
                        <div className="flex items-center justify-end gap-1">
                          <Button asChild size="sm" variant="ghost">
                            <Link to={`/medications/${m.id}/batches`}>
                              <Boxes className="size-4" />
                              Xem lô
                            </Link>
                          </Button>
                          {canManage && (
                            <>
                              <Button asChild size="sm" variant="ghost">
                                <Link to={`/medications/${m.id}/edit`}>
                                  <Pencil className="size-4" />
                                  Sửa
                                </Link>
                              </Button>
                              <ConfirmDialog
                                trigger={
                                  <Button size="sm" variant="ghost" className="text-destructive hover:text-destructive">
                                    Xoá
                                  </Button>
                                }
                                title="Ngừng sử dụng thuốc?"
                                description={`Ngừng sử dụng thuốc "${m.name}"?`}
                                confirmText="Xoá"
                                destructive
                                onConfirm={() => void onDelete(m)}
                              />
                            </>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                })}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {data && (
        <Pager
          page={data.page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          onPageChange={setPage}
        />
      )}
    </section>
  )
}
