import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { deletePatient, listPatients } from '../services/patientService'
import { toApiException } from '../services/apiClient'
import { useAuth } from '../store/auth'
import { genderLabels, type PagedResult, type Patient } from '../types/patient'

const PAGE_SIZE = 10

export default function PatientsListPage() {
  const { canManage } = useAuth()
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [data, setData] = useState<PagedResult<Patient> | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await listPatients({ page, pageSize: PAGE_SIZE, search: search.trim() || undefined })
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

  const onDelete = async (p: Patient) => {
    if (!window.confirm(`Ngừng sử dụng hồ sơ bệnh nhân "${p.fullName}"?`)) return
    try {
      await deletePatient(p.id)
      void load()
    } catch (err) {
      setError(toApiException(err).message)
    }
  }

  return (
    <section>
      <div className="page-head">
        <h1>Quản lý bệnh nhân</h1>
        {canManage && <Link className="btn btn--primary" to="/patients/new">+ Thêm bệnh nhân</Link>}
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
                <th>Mã BN</th>
                <th>Họ tên</th>
                <th>Giới tính</th>
                <th>Ngày sinh</th>
                <th>Điện thoại</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.items.length === 0 && (
                <tr><td colSpan={6} className="table__empty">Không có bệnh nhân nào.</td></tr>
              )}
              {data.items.map((p) => (
                <tr key={p.id}>
                  <td>{p.code}</td>
                  <td>{p.fullName}</td>
                  <td>{genderLabels[p.gender]}</td>
                  <td>{p.dateOfBirth ?? '—'}</td>
                  <td>{p.phoneNumber ?? '—'}</td>
                  <td className="table__actions">
                    {canManage ? (
                      <>
                        <Link to={`/patients/${p.id}/edit`}>Sửa</Link>
                        <button className="link-btn link-btn--danger" onClick={() => onDelete(p)}>Xoá</button>
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
