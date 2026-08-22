import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus, X } from 'lucide-react'
import { listVisits } from '../services/visitService'
import { useAuth } from '../store/auth'
import { toastError } from '../lib/toast'
import { VisitStatus, visitStatusLabels, type VisitListItem } from '../types/visit'
import type { PagedResult } from '../types/common'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { VisitStatusBadge } from '../components/StatusBadge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
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

const PAGE_SIZE = 10
const ALL = 'all'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export default function VisitsListPage() {
  const { canManage } = useAuth()
  const [date, setDate] = useState('')
  const [status, setStatus] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<VisitListItem> | null>(null)
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const result = await listVisits({
        page,
        pageSize: PAGE_SIZE,
        date: date || undefined,
        status: status === '' ? undefined : (Number(status) as VisitListItem['status']),
      })
      setData(result)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [page, date, status])

  useEffect(() => {
    void load()
  }, [load])

  const hasFilter = Boolean(date || status)

  return (
    <section>
      <PageHeader
        title="Lượt tiếp đón"
        description="Một lần bệnh nhân đến khám — gom nhiều dịch vụ khám, cận lâm sàng và viện phí"
        actions={
          canManage && (
            <Button asChild>
              <Link to="/visits/new">
                <Plus className="size-4" />
                Tiếp đón
              </Link>
            </Button>
          )
        }
      />

      <Card className="mb-4">
        <CardContent className="flex flex-wrap items-center gap-3">
          <Input
            type="date"
            className="w-auto"
            value={date}
            onChange={(e) => {
              setPage(1)
              setDate(e.target.value)
            }}
          />
          <Select
            value={status === '' ? ALL : status}
            onValueChange={(v) => {
              setPage(1)
              setStatus(v === ALL ? '' : v)
            }}
          >
            <SelectTrigger className="w-[180px]">
              <SelectValue placeholder="Trạng thái" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>Tất cả trạng thái</SelectItem>
              {Object.values(VisitStatus).map((v) => (
                <SelectItem key={v} value={String(v)}>
                  {visitStatusLabels[v]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {hasFilter && (
            <Button
              variant="ghost"
              onClick={() => {
                setPage(1)
                setDate('')
                setStatus('')
              }}
            >
              <X className="size-4" />
              Xoá lọc
            </Button>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã lượt</TableHead>
                <TableHead>Thời gian</TableHead>
                <TableHead>Bệnh nhân</TableHead>
                <TableHead className="text-center">Số dịch vụ</TableHead>
                <TableHead>Trạng thái</TableHead>
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
                    Không có lượt tiếp đón nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((v) => (
                  <TableRow key={v.id}>
                    <TableCell className="font-medium">{v.code}</TableCell>
                    <TableCell className="whitespace-nowrap">{formatDate(v.createdAt)}</TableCell>
                    <TableCell>{v.patientName ?? '—'}</TableCell>
                    <TableCell className="text-center">{v.serviceCount}</TableCell>
                    <TableCell>
                      <VisitStatusBadge status={v.status} />
                    </TableCell>
                    <TableCell className="text-right">
                      <Button asChild size="sm" variant="ghost">
                        <Link to={`/visits/${v.id}`}>Chi tiết</Link>
                      </Button>
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
