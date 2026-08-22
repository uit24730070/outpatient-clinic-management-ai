import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { deleteMedication, listMedications } from '../services/medicationService'
import { toApiException } from '../services/apiClient'
import { useAuth } from '../store/auth'
import { canManagePharmacy } from '../config/access'
import type { PagedResult } from '../types/common'
import type { Medication } from '../types/medication'

const PAGE_SIZE = 10

export default function MedicationsListPage() {
  const { user } = useAuth()
  const canManage = canManagePharmacy(user?.role)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Medication> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await listMedications({ page, pageSize: PAGE_SIZE, search: search.trim() || undefined })
      setData(result)
    } catch (err) {
      setError(toApiException(err).message)
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

  const onDelete = async (m: Medication) => {
    if (!window.confirm(`Ngừng sử dụng thuốc "${m.name}"?`)) return
    try {
      await deleteMedication(m.id)
      void load()
    } catch (err) {
      setError(toApiException(err).message)
    }
  }

  return (
    <section>
      <div className="page-head">
        <h1>Danh mục thuốc</h1>
        {canManage && <Link className="btn btn--primary" to="/medications/new">+ Thêm thuốc</Link>}
      </div>

      <form className="toolbar" onSubmit={onSearchSubmit}>
        <input
          type="search"
          placeholder="Tìm theo tên, mã, hoạt chất…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <button className="btn" type="submit">Tìm</button>
      </form>

      {error && <p className="alert alert--error">{error}</p>}
      {loading && <p>Đang tải…</p>}

      {data && (
        <>
          <table className="table">
            <thead>
              <tr>
                <th>Mã</th>
                <th>Tên thuốc</th>
                <th>Hoạt chất</th>
                <th>Đơn vị</th>
                <th>Tồn</th>
                <th>Ngưỡng</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 && (
                <tr><td colSpan={7} className="table__empty">Không có thuốc nào.</td></tr>
              )}
              {data.items.map((m) => {
                const low = m.stockOnHand <= m.reorderLevel
                return (
                  <tr key={m.id}>
                    <td>{m.code}</td>
                    <td>{m.name}</td>
                    <td>{m.activeIngredient}</td>
                    <td>{m.unit}</td>
                    <td className={low ? 'text-danger' : undefined}>{m.stockOnHand}</td>
                    <td>{m.reorderLevel}</td>
                    <td className="table__actions">
                      <Link to={`/medications/${m.id}/batches`}>Xem lô</Link>
                      {canManage && (
                        <>
                          <Link to={`/medications/${m.id}/edit`}>Sửa</Link>
                          <button className="link-btn link-btn--danger" onClick={() => onDelete(m)}>Xoá</button>
                        </>
                      )}
                    </td>
                  </tr>
                )
              })}
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
