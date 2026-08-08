import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { deleteDoctor, listDoctors } from '../services/doctorService'
import { toApiException } from '../services/apiClient'
import { useAuth } from '../store/auth'
import type { PagedResult } from '../types/common'
import type { Doctor } from '../types/doctor'

const PAGE_SIZE = 10

export default function DoctorsListPage() {
  const { canManage } = useAuth()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Doctor> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await listDoctors({ page, pageSize: PAGE_SIZE, search: search.trim() || undefined })
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

  const onDelete = async (d: Doctor) => {
    if (!window.confirm(`Ngừng sử dụng hồ sơ bác sĩ "${d.fullName}"?`)) return
    try {
      await deleteDoctor(d.id)
      void load()
    } catch (err) {
      setError(toApiException(err).message)
    }
  }

  return (
    <section>
      <div className="page-head">
        <h1>Quản lý bác sĩ</h1>
        {canManage && <Link className="btn btn--primary" to="/doctors/new">+ Thêm bác sĩ</Link>}
      </div>

      <form className="toolbar" onSubmit={onSearchSubmit}>
        <input
          type="search"
          placeholder="Tìm theo tên, mã, số điện thoại…"
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
                <th>Mã BS</th>
                <th>Họ tên</th>
                <th>Chuyên khoa</th>
                <th>Điện thoại</th>
                <th>Email</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 && (
                <tr><td colSpan={6} className="table__empty">Không có bác sĩ nào.</td></tr>
              )}
              {data.items.map((d) => (
                <tr key={d.id}>
                  <td>{d.code}</td>
                  <td>{d.fullName}</td>
                  <td>{d.specialtyName ?? '—'}</td>
                  <td>{d.phoneNumber ?? '—'}</td>
                  <td>{d.email ?? '—'}</td>
                  <td className="table__actions">
                    {canManage ? (
                      <>
                        <Link to={`/doctors/${d.id}/edit`}>Sửa</Link>
                        <button className="link-btn link-btn--danger" onClick={() => onDelete(d)}>Xoá</button>
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
