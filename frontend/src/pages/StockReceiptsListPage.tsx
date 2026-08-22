import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { listStockReceipts } from '../services/stockReceiptService'
import { toApiException } from '../services/apiClient'
import type { PagedResult } from '../types/common'
import type { StockReceipt } from '../types/medication'

const PAGE_SIZE = 10

export default function StockReceiptsListPage() {
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<StockReceipt> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      setData(await listStockReceipts({ page, pageSize: PAGE_SIZE }))
    } catch (err) {
      setError(toApiException(err).message)
    } finally {
      setLoading(false)
    }
  }, [page])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <section>
      <div className="page-head">
        <h1>Nhập kho</h1>
        <Link className="btn btn--primary" to="/stock-receipts/new">+ Phiếu nhập mới</Link>
      </div>

      {error && <p className="alert alert--error">{error}</p>}
      {loading && <p>Đang tải…</p>}

      {data && (
        <>
          <table className="table">
            <thead>
              <tr>
                <th>Mã phiếu</th>
                <th>Nhà cung cấp</th>
                <th>Ngày nhận</th>
                <th>Số dòng</th>
                <th>Ghi chú</th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 && (
                <tr><td colSpan={5} className="table__empty">Chưa có phiếu nhập nào.</td></tr>
              )}
              {data.items.map((r) => (
                <tr key={r.id}>
                  <td>{r.code}</td>
                  <td>{r.supplierName}</td>
                  <td>{new Date(r.receivedAt).toLocaleDateString('vi-VN')}</td>
                  <td>{r.items.length}</td>
                  <td>{r.note ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="pager">
            <button className="btn" disabled={!data.hasPreviousPage} onClick={() => setPage((p) => p - 1)}>
              ← Trước
            </button>
            <span>Trang {data.page}/{Math.max(data.totalPages, 1)} · {data.totalCount} bản ghi</span>
            <button className="btn" disabled={!data.hasNextPage} onClick={() => setPage((p) => p + 1)}>
              Sau →
            </button>
          </div>
        </>
      )}
    </section>
  )
}
