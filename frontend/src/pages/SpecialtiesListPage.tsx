import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Search, Pencil } from 'lucide-react'
import { deleteSpecialty, listSpecialties } from '../services/specialtyService'
import { useAuth } from '../store/auth'
import { toastError, toastSuccess } from '../lib/toast'
import type { PagedResult } from '../types/common'
import type { Specialty } from '../types/specialty'
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

export default function SpecialtiesListPage() {
  const { canManage } = useAuth()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Specialty> | null>(null)
  const [loading, setLoading] = useState(false)
  const { sort, toggleSort } = useSort(() => setPage(1))

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listSpecialties({
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

  const onDelete = async (s: Specialty) => {
    try {
      await deleteSpecialty(s.id)
      toastSuccess('Đã ngừng sử dụng chuyên khoa.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  return (
    <section>
      <PageHeader
        title="Quản lý chuyên khoa"
        description="Danh mục chuyên khoa của phòng khám"
        actions={
          canManage && (
            <Button asChild>
              <Link to="/specialties/new">
                <Plus className="size-4" />
                Thêm chuyên khoa
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
              placeholder="Tìm theo tên chuyên khoa…"
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
                <SortableTableHead field="name" sort={sort} onSort={toggleSort}>Tên chuyên khoa</SortableTableHead>
                <TableHead>Mô tả</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={3} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={3} className="h-24 text-center text-muted-foreground">
                    Không có chuyên khoa nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((s) => (
                  <TableRow key={s.id}>
                    <TableCell className="font-medium">{s.name}</TableCell>
                    <TableCell className="text-muted-foreground">{s.description ?? '—'}</TableCell>
                    <TableCell>
                      <div className="flex items-center justify-end gap-1">
                        {canManage ? (
                          <>
                            <Button asChild size="sm" variant="ghost">
                              <Link to={`/specialties/${s.id}/edit`}>
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
                              title="Ngừng sử dụng chuyên khoa?"
                              description={`Ngừng sử dụng chuyên khoa "${s.name}"?`}
                              confirmText="Xoá"
                              destructive
                              onConfirm={() => void onDelete(s)}
                            />
                          </>
                        ) : (
                          <span className="text-muted-foreground">—</span>
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
