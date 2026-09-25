import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { Users, Stethoscope, ClipboardList, Wallet, Clock, CalendarDays } from 'lucide-react'
import { Bar, BarChart, CartesianGrid, XAxis } from 'recharts'
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
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  ChartTooltipContent,
  type ChartConfig,
} from '@/components/ui/chart'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

/** Bar chart 2 chuỗi (Hoàn tất/Khác) — dùng chart-1/2 đã khai ở src/index.css (theme sáng+tối sẵn). */
const appointmentChartConfig = {
  completed: { label: 'Hoàn tất', color: 'var(--chart-1)' },
  other: { label: 'Khác', color: 'var(--chart-2)' },
} satisfies ChartConfig

/** Bar chart 4 chuỗi khớp đúng 4 khoản mục doanh thu (thẻ KPI bên dưới) — chart-1..4. */
const revenueChartConfig = {
  serviceFee: { label: 'Công khám', color: 'var(--chart-1)' },
  medication: { label: 'Tiền thuốc', color: 'var(--chart-2)' },
  paraclinical: { label: 'Cận lâm sàng', color: 'var(--chart-3)' },
  other: { label: 'Khác', color: 'var(--chart-4)' },
} satisfies ChartConfig

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

  const apptChartData = useMemo(
    () =>
      (appointments?.days ?? []).map((d) => ({
        date: d.date,
        completed: d.byStatus.completed,
        other: d.total - d.byStatus.completed,
      })),
    [appointments],
  )

  const revenueChartData = useMemo(
    () =>
      (revenue?.days ?? []).map((d) => ({
        date: d.date,
        serviceFee: d.serviceFee,
        medication: d.medication,
        paraclinical: d.paraclinical,
        other: d.other,
      })),
    [revenue],
  )

  // Top 5 bác sĩ có lịch/phiếu khám trong khoảng — bỏ bác sĩ không có số liệu, giữ bảng gọn.
  const topDoctors = useMemo(
    () =>
      doctors
        .filter((d) => d.totalAppointments > 0 || d.encounters > 0)
        .sort((a, b) => b.totalAppointments - a.totalAppointments)
        .slice(0, 5),
    [doctors],
  )

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
              <ChartContainer config={appointmentChartConfig} className="aspect-auto h-56 w-full">
                <BarChart data={apptChartData} barCategoryGap={apptChartData.length > 14 ? '15%' : '30%'}>
                  <CartesianGrid vertical={false} />
                  <XAxis
                    dataKey="date"
                    tickFormatter={shortDay}
                    tickLine={false}
                    axisLine={false}
                    tickMargin={8}
                  />
                  <ChartTooltip content={<ChartTooltipContent labelFormatter={(_, p) => shortDay(String(p[0]?.payload.date ?? ''))} />} />
                  <ChartLegend content={<ChartLegendContent />} />
                  <Bar dataKey="other" stackId="appt" fill="var(--color-other)" radius={[0, 0, 0, 0]} />
                  <Bar dataKey="completed" stackId="appt" fill="var(--color-completed)" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ChartContainer>
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
              <ChartContainer config={revenueChartConfig} className="aspect-auto h-56 w-full">
                <BarChart data={revenueChartData} barCategoryGap={revenueChartData.length > 14 ? '15%' : '30%'}>
                  <CartesianGrid vertical={false} />
                  <XAxis
                    dataKey="date"
                    tickFormatter={shortDay}
                    tickLine={false}
                    axisLine={false}
                    tickMargin={8}
                  />
                  <ChartTooltip
                    content={
                      <ChartTooltipContent
                        labelFormatter={(_, p) => shortDay(String(p[0]?.payload.date ?? ''))}
                        formatter={(value, _name, item) => (
                          <div className="flex w-full items-center justify-between gap-4">
                            <span className="text-muted-foreground">
                              {revenueChartConfig[item.dataKey as keyof typeof revenueChartConfig]?.label}
                            </span>
                            <span className="text-foreground font-mono font-medium tabular-nums">
                              {formatVnd(Number(value))}
                            </span>
                          </div>
                        )}
                      />
                    }
                  />
                  <ChartLegend content={<ChartLegendContent />} />
                  <Bar dataKey="serviceFee" stackId="rev" fill="var(--color-serviceFee)" radius={[0, 0, 0, 0]} />
                  <Bar dataKey="medication" stackId="rev" fill="var(--color-medication)" radius={[0, 0, 0, 0]} />
                  <Bar dataKey="paraclinical" stackId="rev" fill="var(--color-paraclinical)" radius={[0, 0, 0, 0]} />
                  <Bar dataKey="other" stackId="rev" fill="var(--color-other)" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ChartContainer>
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

      {/* Năng suất theo bác sĩ — top 5, bỏ bác sĩ không có số liệu trong khoảng */}
      <Card className="mt-6">
        <CardHeader>
          <CardTitle>Năng suất theo bác sĩ (Top 5)</CardTitle>
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
              {topDoctors.length === 0 && (
                <TableRow>
                  <TableCell colSpan={5} className="h-20 text-center text-muted-foreground">
                    Không có dữ liệu.
                  </TableCell>
                </TableRow>
              )}
              {topDoctors.map((d) => (
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
