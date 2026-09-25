import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Plus, Search, History, Pencil } from 'lucide-react'
import { deletePatient, listPatients } from '../services/patientService'
import { useAuth } from '../store/auth'
import { toastError, toastSuccess } from '../lib/toast'
import { genderLabels, type PagedResult, type Patient } from '../types/patient'
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

export default function PatientsListPage() {
  const { canManage } = useAuth()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Patient> | null>(null)
  const [loading, setLoading] = useState(false)
  const { sort, toggleSort } = useSort(() => setPage(1))

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listPatients({
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

  const onDelete = async (p: Patient) => {
    try {
      await deletePatient(p.id)
      toastSuccess('Đã ngừng sử dụng hồ sơ bệnh nhân.')
      void load()
    } catch (err) {
      toastError(err)
    }
  }

  return (
    <section>
      <PageHeader
        title="Quản lý bệnh nhân"
        description="Danh sách hồ sơ bệnh nhân của phòng khám"
        actions={
          canManage && (
            <Button asChild>
              <Link to="/patients/new">
                <Plus className="size-4" />
                Thêm bệnh nhân
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
                <SortableTableHead field="code" sort={sort} onSort={toggleSort}>Mã BN</SortableTableHead>
                <SortableTableHead field="fullName" sort={sort} onSort={toggleSort}>Họ tên</SortableTableHead>
                <TableHead>Giới tính</TableHead>
                <SortableTableHead field="dateOfBirth" sort={sort} onSort={toggleSort}>Ngày sinh</SortableTableHead>
                <SortableTableHead field="phoneNumber" sort={sort} onSort={toggleSort}>Điện thoại</SortableTableHead>
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
                    Không có bệnh nhân nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((p) => (
                  <TableRow key={p.id}>
                    <TableCell className="font-mono text-sm">{p.code}</TableCell>
                    <TableCell className="font-medium">{p.fullName}</TableCell>
                    <TableCell>{genderLabels[p.gender]}</TableCell>
                    <TableCell>{p.dateOfBirth ?? '—'}</TableCell>
                    <TableCell>{p.phoneNumber ?? '—'}</TableCell>
                    <TableCell>
                      <div className="flex items-center justify-end gap-1">
                        <Button asChild size="sm" variant="ghost">
                          <Link to={`/patients/${p.id}/encounters`}>
                            <History className="size-4" />
                            Lịch sử
                          </Link>
                        </Button>
                        {canManage && (
                          <>
                            <Button asChild size="sm" variant="ghost">
                              <Link to={`/patients/${p.id}/edit`}>
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
                              description={`Ngừng sử dụng hồ sơ bệnh nhân "${p.fullName}"?`}
                              confirmText="Xoá"
                              destructive
                              onConfirm={() => void onDelete(p)}
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
