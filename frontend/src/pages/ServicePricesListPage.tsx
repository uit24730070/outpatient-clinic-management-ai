import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Search, Pencil } from 'lucide-react'
import { deleteServicePrice, listServicePrices } from '../services/servicePriceService'
import { useAuth } from '../store/auth'
import { canManageBilling } from '../config/access'
import { toastError, toastSuccess } from '../lib/toast'
import type { PagedResult } from '../types/common'
import { paraclinicalGroupLabels, serviceCategoryLabels, type ServicePrice } from '../types/invoice'
import { formatVnd } from '../lib/format'
import { TonedBadge } from '../components/StatusBadge'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { ConfirmDialog } from '../components/ConfirmDialog'
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

export default function ServicePricesListPage() {
  const { user } = useAuth()
  const canManage = canManageBilling(user?.role)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<ServicePrice> | null>(null)
  const [loading, setLoading] = useState(false)
  const { sort, toggleSort } = useSort(() => setPage(1))

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listServicePrices({
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

  const onDelete = async (s: ServicePrice) => {
    try {
      await deleteServicePrice(s.id)
      toastSuccess('Đã ngừng sử dụng dịch vụ.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  return (
    <section>
      <PageHeader
        title="Bảng giá dịch vụ"
        description="Công khám, thủ thuật, tư vấn… dùng để lập hoá đơn"
        actions={
          canManage && (
            <Button asChild>
              <Link to="/service-prices/new">
                <Plus className="size-4" />
                Thêm dịch vụ
              </Link>
            </Button>
          )
        }
      />

      <Card className="mb-4">
        <CardContent>
          <form className="flex gap-2" onSubmit={onSearchSubmit}>
            <Input
              type="search"
              placeholder="Tìm theo tên, mã…"
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
                <SortableTableHead field="name" sort={sort} onSort={toggleSort}>Tên dịch vụ</SortableTableHead>
                <SortableTableHead field="category" sort={sort} onSort={toggleSort}>Phân loại</SortableTableHead>
                <TableHead>Mô tả</TableHead>
                <SortableTableHead field="unitPrice" sort={sort} onSort={toggleSort} className="text-right" align="right">Đơn giá</SortableTableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                    Chưa có dịch vụ nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((s) => (
                  <TableRow key={s.id}>
                    <TableCell className="font-mono text-sm">{s.code}</TableCell>
                    <TableCell className="font-medium">{s.name}</TableCell>
                    <TableCell>
                      <TonedBadge tone={s.category === 1 ? 'cyan' : s.category === 0 ? 'blue' : 'gray'}>
                        {serviceCategoryLabels[s.category] ?? '—'}
                      </TonedBadge>
                      {s.category === 1 && (
                        <span className="ml-1.5 text-xs text-muted-foreground">
                          · {s.group !== null ? paraclinicalGroupLabels[s.group] : 'chưa phân nhóm'}
                        </span>
                      )}
                    </TableCell>
                    <TableCell className="text-muted-foreground">{s.description ?? '—'}</TableCell>
                    <TableCell className="text-right tabular-nums">{formatVnd(s.unitPrice)}</TableCell>
                    <TableCell>
                      <div className="flex items-center justify-end gap-1">
                        {canManage && (
                          <>
                            <Button asChild size="sm" variant="ghost">
                              <Link to={`/service-prices/${s.id}/edit`}>
                                <Pencil className="size-4" />
                                Sửa
                              </Link>
                            </Button>
                            <ConfirmDialog
                              trigger={
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  className="text-destructive hover:text-destructive"
                                >
                                  Xoá
                                </Button>
                              }
                              title="Ngừng sử dụng dịch vụ?"
                              description={`Ngừng sử dụng dịch vụ "${s.name}"? Hoá đơn cũ không bị ảnh hưởng.`}
                              confirmText="Xoá"
                              destructive
                              onConfirm={() => void onDelete(s)}
                            />
                          </>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
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
