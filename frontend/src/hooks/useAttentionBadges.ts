import { useEffect, useState } from 'react'
import { listAppointments } from '../services/appointmentService'
import { listEncounters } from '../services/encounterService'
import { listLabOrders } from '../services/labOrderService'
import { listQueue } from '../services/queueService'
import { getPharmacyAlerts } from '../services/pharmacyService'
import { UserRole, type UserRoleValue } from '../types/auth'
import { AppointmentStatus } from '../types/appointment'
import { DispenseStatus } from '../types/encounter'
import { LabOrderStatus } from '../types/labOrder'
import { QueueTicketStatus } from '../types/queue'
import type { KpiTone } from '@/components/KpiCard'

export interface AttentionBadge {
  key: string
  label: string
  count: number
  to: string
  tone?: KpiTone
}

const REFRESH_MS = 60_000

function todayLocal(): string {
  const now = new Date()
  const offset = now.getTimezoneOffset()
  return new Date(now.getTime() - offset * 60_000).toISOString().slice(0, 10)
}

async function loadBadges(role: UserRoleValue, doctorId: string | null): Promise<AttentionBadge[]> {
  const today = todayLocal()

  switch (role) {
    case UserRole.Receptionist: {
      const q = await listQueue({ date: today })
      const waiting = q.filter(
        (t) => t.status === QueueTicketStatus.Waiting || t.status === QueueTicketStatus.Called,
      ).length
      return waiting > 0
        ? [{ key: 'queue', label: 'Đang chờ gọi', count: waiting, to: '/front-desk' }]
        : []
    }

    case UserRole.Nurse: {
      const q = await listQueue({ date: today })
      const waiting = q.filter(
        (t) => t.status === QueueTicketStatus.Waiting || t.status === QueueTicketStatus.Called,
      ).length
      return waiting > 0 ? [{ key: 'queue', label: 'Đang chờ gọi', count: waiting, to: '/nurse' }] : []
    }

    case UserRole.Doctor: {
      if (!doctorId) return []
      const res = await listAppointments({
        page: 1,
        pageSize: 1,
        date: today,
        doctorId,
        status: AppointmentStatus.CheckedIn,
      })
      return res.totalCount > 0
        ? [{ key: 'checkedin', label: 'Đã tiếp nhận, chờ khám', count: res.totalCount, to: '/my-clinic' }]
        : []
    }

    case UserRole.Pharmacist: {
      const [pending, alerts] = await Promise.all([
        listEncounters({ page: 1, pageSize: 1, dispenseStatus: DispenseStatus.Paid }),
        getPharmacyAlerts(),
      ])
      const badges: AttentionBadge[] = []
      if (pending.totalCount > 0) {
        badges.push({
          key: 'dispense',
          label: 'Chờ cấp phát',
          count: pending.totalCount,
          to: '/pharmacy/workspace',
        })
      }
      const alertCount = alerts.lowStock.length + alerts.expiringBatches.length
      if (alertCount > 0) {
        badges.push({
          key: 'stock',
          label: 'Cảnh báo kho',
          count: alertCount,
          to: '/pharmacy/alerts',
          tone: 'warning',
        })
      }
      return badges
    }

    case UserRole.Technician: {
      const res = await listLabOrders({ page: 1, pageSize: 1, status: LabOrderStatus.Ordered })
      return res.totalCount > 0
        ? [{ key: 'lab', label: 'CLS chờ thực hiện', count: res.totalCount, to: '/lab/technician' }]
        : []
    }

    case UserRole.Admin: {
      const alerts = await getPharmacyAlerts()
      const alertCount = alerts.lowStock.length + alerts.expiringBatches.length
      return alertCount > 0
        ? [{ key: 'stock', label: 'Cảnh báo kho', count: alertCount, to: '/pharmacy/alerts', tone: 'warning' }]
        : []
    }

    default:
      return []
  }
}

/**
 * Topbar theo ngữ cảnh (Epic 18, VIS-01): số lượng việc cần chú ý ngay theo vai trò hiện tại
 * (hàng đợi đang chờ, đơn chờ cấp phát, CLS chờ thực hiện, cảnh báo tồn kho...), điều hướng thẳng
 * tới nơi xử lý khi bấm. Nạp lại định kỳ để không bị lệch quá xa thực tế trong ca làm việc.
 */
export function useAttentionBadges(role: UserRoleValue | undefined, doctorId: string | null) {
  const [badges, setBadges] = useState<AttentionBadge[]>([])

  useEffect(() => {
    if (!role) {
      setBadges([])
      return
    }
    let cancelled = false

    const refresh = () => {
      loadBadges(role, doctorId)
        .then((result) => {
          if (!cancelled) setBadges(result)
        })
        .catch(() => {
          // Badge ngữ cảnh chỉ là gợi ý — lỗi tải không nên chặn/ồn ào cả giao diện.
        })
    }

    refresh()
    const interval = setInterval(refresh, REFRESH_MS)
    return () => {
      cancelled = true
      clearInterval(interval)
    }
  }, [role, doctorId])

  return badges
}
