import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Users, Stethoscope, ClipboardList, Wallet, Clock, CalendarDays } from 'lucide-react'
import { toastError } from '../lib/toast'
import { formatVnd } from '../lib/format'
import {
  getAppointmentReport,
  getDoctorProductivity,
  getOverview,
  getRevenueReport,
} from '../services/reportService'
import type {
  AppointmentReport,
  DoctorProductivity,
  Overview,
  RevenueReport,
} from '../types/report'
import { PageHeader } from '../components/PageHeader'
import { KpiCard } from '@/components/KpiCard'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

/** Ngày local dạng yyyy-MM-dd (đầu vào <input type=date> và query báo cáo). */
function isoDate(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

/** Nhãn ngày ngắn gọn dd/MM cho trục biểu đồ. */
function shortDay(iso: string): string {
  const [, m, d] = iso.split('-')
  return `${d}/${m}`
}

export default function DashboardPage() {
  const today = useMemo(() => new Date(), [])
  const defaultFrom = useMemo(() => {
    const d = new Date(today)
    d.setDate(d.getDate() - 6)
    return isoDate(d)
  }, [today])

  const [from, setFrom] = useState(defaultFrom)
  const [to, setTo] = useState(isoDate(today))
  const [overview, setOverview] = useState<Overview | null>(null)
  const [appointments, setAppointments] = useState<AppointmentReport | null>(null)
  const [revenue, setRevenue] = useState<RevenueReport | null>(null)
  const [doctors, setDoctors] = useState<DoctorProductivity[]>([])
  const [loading, setLoading] = useState(false)

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const range = { from, to }
      const [ov, appt, rev, docs] = await Promise.all([
        getOverview(),
        getAppointmentReport(range),
        getRevenueReport(range),
        getDoctorProductivity(range),
      ])
      setOverview(ov)
      setAppointments(appt)
      setRevenue(rev)
      setDoctors(docs)
    } catch (err) {
      toastError(err)
    } finally {
      setLoading(false)
    }
  }, [from, to])

  useEffect(() => {
    void load()
    // Chỉ nạp lần đầu; đổi khoảng ngày qua nút "Áp dụng".
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const onApply = (e: FormEvent) => {
    e.preventDefault()
    void load()
  }

  const maxAppt = Math.max(1, ...(appointments?.days.map((d) => d.total) ?? [0]))
  const maxRevenue = Math.max(1, ...(revenue?.days.map((d) => d.total) ?? [0]))

  return (
    <section>
      <PageHeader
        title="Tổng quan"
        description="Bảng điều khiển thống kê vận hành & doanh thu"
      />

      {/* Thẻ KPI — chỉ số hôm nay */}
      <div className="mb-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <KpiCard icon={Users} label="Bệnh nhân đang quản lý" value={String(overview?.totalActivePatients ?? '—')} />
        <KpiCard icon={Stethoscope} label="Bác sĩ" value={String(overview?.totalDoctors ?? '—')} />
        <KpiCard icon={CalendarDays} label="Lịch khám hôm nay" value={String(overview?.appointmentsToday ?? '—')} />
        <KpiCard icon={ClipboardList} label="Phiếu khám hôm nay" value={String(overview?.encountersToday ?? '—')} />
        <KpiCard icon={Clock} label="Đang chờ hàng đợi" value={String(overview?.queueWaiting ?? '—')} />
        <KpiCard icon={Wallet} label="Doanh thu hôm nay" value={overview ? formatVnd(overview.revenueToday) : '—'} />
      </div>

      {/* Bộ lọc khoảng ngày cho các báo cáo theo khoảng */}
      <Card className="mb-6">
        <CardContent>
          <form className="flex flex-wrap items-end gap-3" onSubmit={onApply}>
            <div className="grid gap-1">
              <label className="text-sm text-muted-foreground" htmlFor="from">Từ ngày</label>
              <Input id="from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} />
            </div>
            <div className="grid gap-1">
              <label className="text-sm text-muted-foreground" htmlFor="to">Đến ngày</label>
              <Input id="to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
            </div>
            <Button type="submit" disabled={loading}>{loading ? 'Đang tải…' : 'Áp dụng'}</Button>
          </form>
        </CardContent>
      </Card>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        {/* Biểu đồ lịch khám theo ngày */}
        <Card>
          <CardHeader>
            <CardTitle>Lịch khám theo ngày</CardTitle>
          </CardHeader>
          <CardContent>
            {appointments && appointments.days.length > 0 ? (
              <div className="flex h-48 items-end gap-2">
                {appointments.days.map((d) => (
                  <div key={d.date} className="flex flex-1 flex-col items-center gap-1" title={`${d.date}: ${d.total} lịch (hoàn tất ${d.byStatus.completed})`}>
                    <span className="text-xs font-medium text-muted-foreground">{d.total}</span>
                    <div className="flex w-full flex-1 items-end">
                      <div
                        className="w-full rounded-t bg-primary/80"
                        style={{ height: `${(d.total / maxAppt) * 100}%` }}
                      />
                    </div>
                    <span className="text-[10px] text-muted-foreground">{shortDay(d.date)}</span>
                  </div>
                ))}
              </div>
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">Không có dữ liệu.</p>
            )}
            {appointments && (
              <p className="mt-3 text-sm text-muted-foreground">
                Tổng {appointments.total} lịch trong khoảng.
              </p>
            )}
          </CardContent>
        </Card>

        {/* Biểu đồ doanh thu theo ngày */}
        <Card>
          <CardHeader>
            <CardTitle>Doanh thu theo ngày</CardTitle>
          </CardHeader>
          <CardContent>
            {revenue && revenue.days.length > 0 ? (
              <div className="flex h-48 items-end gap-2">
                {revenue.days.map((d) => (
                  <div key={d.date} className="flex flex-1 flex-col items-center gap-1" title={`${d.date}: ${formatVnd(d.total)}`}>
                    <div className="flex w-full flex-1 items-end">
                      <div
                        className="w-full rounded-t bg-emerald-500/80"
                        style={{ height: `${(d.total / maxRevenue) * 100}%` }}
                      />
                    </div>
                    <span className="text-[10px] text-muted-foreground">{shortDay(d.date)}</span>
                  </div>
                ))}
              </div>
            ) : (
              <p className="py-8 text-center text-sm text-muted-foreground">Không có dữ liệu.</p>
            )}
            {revenue && (
              <p className="mt-3 text-sm font-medium">
                Tổng doanh thu: <span className="text-emerald-600">{formatVnd(revenue.grandTotal)}</span>
              </p>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Tách doanh thu theo loại khoản mục */}
      {revenue && (
        <div className="mt-6 grid grid-cols-2 gap-4 sm:grid-cols-4">
          <KpiCard icon={Wallet} label="Công khám" value={formatVnd(revenue.serviceFeeTotal)} />
          <KpiCard icon={Wallet} label="Tiền thuốc" value={formatVnd(revenue.medicationTotal)} />
          <KpiCard icon={Wallet} label="Cận lâm sàng" value={formatVnd(revenue.paraclinicalTotal)} />
          <KpiCard icon={Wallet} label="Khác" value={formatVnd(revenue.otherTotal)} />
        </div>
      )}

      {/* Năng suất theo bác sĩ */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle>Năng suất theo bác sĩ</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã</TableHead>
                <TableHead>Bác sĩ</TableHead>
                <TableHead className="text-right">Tổng lịch</TableHead>
                <TableHead className="text-right">Hoàn tất</TableHead>
                <TableHead className="text-right">Phiếu khám</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {doctors.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="h-20 text-center text-muted-foreground">
                    Không có dữ liệu.
                  </TableCell>
                </TableRow>
              )}
              {doctors.map((d) => (
                <TableRow key={d.doctorId}>
                  <TableCell className="font-mono text-sm">{d.doctorCode}</TableCell>
                  <TableCell className="font-medium">{d.doctorName}</TableCell>
                  <TableCell className="text-right">{d.totalAppointments}</TableCell>
                  <TableCell className="text-right">{d.completedAppointments}</TableCell>
                  <TableCell className="text-right">{d.encounters}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </section>
  )
}
