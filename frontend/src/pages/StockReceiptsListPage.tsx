import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { listStockReceipts } from '../services/stockReceiptService'
import { toastError } from '../lib/toast'
import type { PagedResult } from '../types/common'
import type { StockReceipt } from '../types/medication'
import { PageHeader } from '../components/PageHeader'
import { Pager } from '../components/Pager'
import { SortableTableHead } from '../components/SortableTableHead'
import { useSort } from '../hooks/useSort'
import { Button } from '@/components/ui/button'
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

export default function StockReceiptsListPage() {
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<StockReceipt> | null>(null)
  const [loading, setLoading] = useState(false)
  const { sort, toggleSort } = useSort(() => setPage(1))

  const load = useCallback(async () => {
    setLoading(true)
    try {
      setData(await listStockReceipts({ page, pageSize: PAGE_SIZE, sortBy: sort.sortBy, sortDesc: sort.sortDesc }))
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [page, sort.sortBy, sort.sortDesc])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <section>
      <PageHeader
        title="Nhập kho"
        description="Phiếu nhập thuốc theo lô và hạn dùng"
        actions={
          <Button asChild>
            <Link to="/stock-receipts/new">
              <Plus className="size-4" />
              Phiếu nhập mới
            </Link>
          </Button>
        }
      />

      <Card>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <SortableTableHead field="code" sort={sort} onSort={toggleSort}>Mã phiếu</SortableTableHead>
                <SortableTableHead field="supplierName" sort={sort} onSort={toggleSort}>Nhà cung cấp</SortableTableHead>
                <SortableTableHead field="receivedAt" sort={sort} onSort={toggleSort}>Ngày nhận</SortableTableHead>
                <TableHead>Số dòng</TableHead>
                <TableHead>Ghi chú</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {loading && (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    Đang tải…
                  </TableCell>
                </TableRow>
              )}
              {!loading && data?.items.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="h-24 text-center text-muted-foreground">
                    Chưa có phiếu nhập nào.
                  </TableCell>
                </TableRow>
              )}
              {!loading &&
                data?.items.map((r) => (
                  <TableRow key={r.id}>
                    <TableCell className="font-mono text-sm">{r.code}</TableCell>
                    <TableCell className="font-medium">{r.supplierName}</TableCell>
                    <TableCell>{new Date(r.receivedAt).toLocaleDateString('vi-VN')}</TableCell>
                    <TableCell>{r.items.length}</TableCell>
                    <TableCell className="text-muted-foreground">{r.note ?? '—'}</TableCell>
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
