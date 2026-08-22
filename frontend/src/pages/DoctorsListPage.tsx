import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Search, Pencil } from 'lucide-react'
import { deleteDoctor, listDoctors } from '../services/doctorService'
import { useAuth } from '../store/auth'
import { toastError, toastSuccess } from '../lib/toast'
import type { PagedResult } from '../types/common'
import type { Doctor } from '../types/doctor'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { ConfirmDialog } from '../components/ConfirmDialog'
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

export default function DoctorsListPage() {
  const { canManage } = useAuth()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Doctor> | null>(null)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listDoctors({ page, pageSize: PAGE_SIZE, search: search.trim() || undefined })
      setData(result)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [page, search])

  useEffect(() => {
    void load()
  }, [load])

  const onSearchSubmit = (e: FormEvent) => {
    e.preventDefault()
    setPage(1)
    void load()
  }

  const onDelete = async (d: Doctor) => {
    try {
      await deleteDoctor(d.id)
      toastSuccess('Đã ngừng sử dụng hồ sơ bác sĩ.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  return (
    <section>
      <PageHeader
        title="Quản lý bác sĩ"
        description="Hồ sơ bác sĩ và chuyên khoa"
        actions={
          canManage && (
            <Button asChild>
              <Link to="/doctors/new">
                <Plus className="size-4" />
                Thêm bác sĩ
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
              placeholder="Tìm theo tên, mã, số điện thoại…"
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
                <TableHead>Mã BS</TableHead>
                <TableHead>Họ tên</TableHead>
                <TableHead>Chuyên khoa</TableHead>
                <TableHead>Điện thoại</TableHead>
                <TableHead>Email</TableHead>
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
                    Không có bác sĩ nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((d) => (
                  <TableRow key={d.id}>
                    <TableCell className="font-mono text-sm">{d.code}</TableCell>
                    <TableCell className="font-medium">{d.fullName}</TableCell>
                    <TableCell>{d.specialtyName ?? '—'}</TableCell>
                    <TableCell>{d.phoneNumber ?? '—'}</TableCell>
                    <TableCell>{d.email ?? '—'}</TableCell>
                    <TableCell>
                      <div className="flex items-center justify-end gap-1">
                        {canManage ? (
                          <>
                            <Button asChild size="sm" variant="ghost">
                              <Link to={`/doctors/${d.id}/edit`}>
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
                              title="Ngừng sử dụng hồ sơ?"
                              description={`Ngừng sử dụng hồ sơ bác sĩ "${d.fullName}"?`}
                              confirmText="Xoá"
                              destructive
                              onConfirm={() => void onDelete(d)}
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
