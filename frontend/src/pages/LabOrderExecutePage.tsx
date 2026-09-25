import { useCallback, useEffect, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { getLabOrder } from '../services/labOrderService'
import { toastError } from '../lib/toast'
import { useAuth } from '../store/auth'
import { canRecordEncounter } from '../config/access'
import type { LabOrder } from '../types/labOrder'
import { LabOrderCard } from '../components/LabOrderPanel'
import { PageHeader } from '../components/PageHeader'
import { PatientContextHeader } from '../components/PatientContextHeader'
import { Button } from '@/components/ui/button'

/**
 * Màn "Thực hiện" một phiếu chỉ định CLS cụ thể (Kỹ thuật viên) — tách từ danh sách hàng chờ
 * (`TechnicianLabPage`) để mỗi lượt chỉ tập trung nhập kết quả cho một bệnh nhân.
 */
export default function LabOrderExecutePage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { user } = useAuth()
  const canCancel = canRecordEncounter(user?.role)
  const [order, setOrder] = useState<LabOrder | null>(null)
  const [loading, setLoading] = useState(true)

  const load = useCallback(async () => {
    if (!id) return
    try {
      const res = await getLabOrder(id)
      setOrder(res)
    } catch (err) {
      toastError(err)
      navigate('/lab/technician')
    } finally {
      setLoading(false)
    }
  }, [id, navigate])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <section className="flex flex-col gap-4">
      <PageHeader
        title="Thực hiện cận lâm sàng"
        description={order ? `Phiếu chỉ định ${order.code}` : 'Đang tải…'}
        actions={
          <Button asChild variant="outline">
            <Link to="/lab/technician">
              <ArrowLeft className="size-4" />
              Về danh sách
            </Link>
          </Button>
        }
      />

      {loading ? (
        <p className="text-muted-foreground">Đang tải…</p>
      ) : !order ? null : (
        <>
          <PatientContextHeader patientId={order.patientId} fallbackName={order.patientName} />
          <LabOrderCard order={order} canRecord canCancel={canCancel} onChanged={load} />
        </>
      )}
    </section>
  )
}
