import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { History } from 'lucide-react'
import { getPatient } from '../services/patientService'
import { genderLabels, type Patient } from '../types/patient'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'

function ageFromDob(dob: string | null): number | null {
  if (!dob) return null
  const birth = new Date(dob)
  if (Number.isNaN(birth.getTime())) return null
  const now = new Date()
  let age = now.getFullYear() - birth.getFullYear()
  const m = now.getMonth() - birth.getMonth()
  if (m < 0 || (m === 0 && now.getDate() < birth.getDate())) age--
  return age
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/)
  const first = parts[0]?.[0] ?? ''
  const last = parts.length > 1 ? parts[parts.length - 1][0] : ''
  return (first + last).toUpperCase() || '?'
}

interface Props {
  patientId: string
  /** Tên hiển thị ngay trong lúc chờ tải chi tiết bệnh nhân — tránh nháy trắng. */
  fallbackName?: string | null
  /** 'banner' (mặc định): dải lớn đầu trang cho 1 bệnh nhân. 'inline': gọn trong 1 dòng của danh sách nhiều bệnh nhân. */
  variant?: 'banner' | 'inline'
  /** Ẩn nút "Lịch sử khám" ở variant banner — dùng khi đã có lối vào lịch sử khám ngay bên dưới (vd RecentEncountersCard), tránh trùng lặp. */
  showHistoryLink?: boolean
  /** Bỏ khung `<Card>` bọc ngoài ở variant banner — dùng khi ghép chung vào một Card khác (vd với RecentEncountersCard). */
  bare?: boolean
  className?: string
}

/**
 * Ngữ cảnh bệnh nhân cố định khi đang thao tác trên hồ sơ một bệnh nhân cụ thể (Epic 18, VIS-03) —
 * thay việc phải dò tên trong bảng để biết đang xử lý ai. Tự tải chi tiết (tuổi/giới tính tính từ
 * ngày sinh, mã BN) qua `getPatient` — mỗi nơi gắn component chỉ cần truyền `patientId`.
 */
export function PatientContextHeader({
  patientId,
  fallbackName,
  variant = 'banner',
  showHistoryLink = true,
  bare = false,
  className,
}: Props) {
  const [patient, setPatient] = useState<Patient | null>(null)

  useEffect(() => {
    let active = true
    setPatient(null)
    void (async () => {
      try {
        const p = await getPatient(patientId)
        if (active) setPatient(p)
      } catch {
        // Ngữ cảnh chỉ là gợi ý hiển thị — lỗi tải không nên chặn màn thao tác chính.
      }
    })()
    return () => {
      active = false
    }
  }, [patientId])

  const name = patient?.fullName ?? fallbackName ?? '—'
  const age = ageFromDob(patient?.dateOfBirth ?? null)
  const genderLabel = patient ? genderLabels[patient.gender] : null
  const details = [patient?.code, age != null ? `${age} tuổi` : null, genderLabel]
    .filter((s): s is string => Boolean(s))
    .join(' · ')

  if (variant === 'inline') {
    return (
      <div className={cn('flex flex-wrap items-center gap-2 text-sm', className)}>
        <span className="font-medium text-foreground">{name}</span>
        {details && <span className="text-muted-foreground">· {details}</span>}
        <Button asChild size="icon" variant="ghost" className="size-6 text-muted-foreground">
          <Link to={`/patients/${patientId}/encounters`} title="Lịch sử khám">
            <History className="size-3.5" />
          </Link>
        </Button>
      </div>
    )
  }

  const body = (
    <>
      <Avatar className="size-11">
        <AvatarFallback className="bg-primary/10 text-sm font-semibold text-primary">
          {initials(name)}
        </AvatarFallback>
      </Avatar>
      <div className="flex-1">
        <p className="text-base font-semibold">{name}</p>
        <p className="text-sm text-muted-foreground">{details || 'Đang tải thông tin…'}</p>
      </div>
      {showHistoryLink && (
        <Button asChild size="sm" variant="ghost">
          <Link to={`/patients/${patientId}/encounters`}>
            <History className="size-4" />
            Lịch sử khám
          </Link>
        </Button>
      )}
    </>
  )

  if (bare) return <div className={cn('flex items-center gap-4 py-4', className)}>{body}</div>

  return (
    <Card className={className}>
      <CardContent className="flex items-center gap-4 py-4">{body}</CardContent>
    </Card>
  )
}
