import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { deleteSpecialty, listSpecialties } from '../services/specialtyService'
import { toApiException } from '../services/apiClient'
import { useAuth } from '../store/auth'
import type { PagedResult } from '../types/common'
import type { Specialty } from '../types/specialty'

const PAGE_SIZE = 10

export default function SpecialtiesListPage() {
  const { canManage } = useAuth()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Specialty> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await listSpecialties({ page, pageSize: PAGE_SIZE, search: search.trim() || undefined })
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

  const onDelete = async (s: Specialty) => {
    if (!window.confirm(`Ngừng sử dụng chuyên khoa "${s.name}"?`)) return
    try {
      await deleteSpecialty(s.id)
      void load()
    } catch (err) {
      setError(toApiException(err).message)
    }
  }

  return (
    <section>
      <div className="page-head">
        <h1>Quản lý chuyên khoa</h1>
        {canManage && <Link className="btn btn--primary" to="/specialties/new">+ Thêm chuyên khoa</Link>}
      </div>

      <form className="toolbar" onSubmit={onSearchSubmit}>
        <input
          type="search"
          placeholder="Tìm theo tên chuyên khoa…"
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
                <th>Tên chuyên khoa</th>
                <th>Mô tả</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 && (
                <tr><td colSpan={3} className="table__empty">Không có chuyên khoa nào.</td></tr>
              )}
              {data.items.map((s) => (
                <tr key={s.id}>
                  <td>{s.name}</td>
                  <td>{s.description ?? '—'}</td>
                  <td className="table__actions">
                    {canManage ? (
                      <>
                        <Link to={`/specialties/${s.id}/edit`}>Sửa</Link>
                        <button className="link-btn link-btn--danger" onClick={() => onDelete(s)}>Xoá</button>
                      </>
                    ) : '—'}
                  </td>
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
