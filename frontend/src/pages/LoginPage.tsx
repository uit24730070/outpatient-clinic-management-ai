import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import {
  Bot,
  CalendarCheck,
  Hospital,
  Loader2,
  Pill,
  Stethoscope,
  Wallet,
} from 'lucide-react'
import { useAuth } from '../store/auth'
import { toApiException } from '../services/apiClient'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

// Vài nét về nghiệp vụ hệ thống — cho khung đăng nhập "cảm giác phần mềm phòng khám" thay vì
// một form admin trần trụi bất kỳ (Epic 18, VIS-05).
const highlights = [
  { icon: CalendarCheck, label: 'Tiếp nhận & hàng đợi khám' },
  { icon: Stethoscope, label: 'Bệnh án & kê đơn điện tử' },
  { icon: Pill, label: 'Kho thuốc theo lô, hạn dùng' },
  { icon: Wallet, label: 'Viện phí & thu ngân' },
  { icon: Bot, label: 'Trợ lý AI tra cứu nghiệp vụ' },
]

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login } = useAuth()

  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Sau khi đăng nhập, quay lại trang người dùng định vào (nếu có), mặc định /patients.
  const from = (location.state as { from?: string } | null)?.from ?? '/patients'

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await login({ username: username.trim(), password })
      navigate(from, { replace: true })
    } catch (err) {
      setError(toApiException(err).message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/40 p-4">
      <div className="grid w-full max-w-4xl overflow-hidden rounded-2xl border bg-card shadow-sm md:grid-cols-2">
        {/* Panel thương hiệu — chỉ hiện từ md trở lên, truyền tải "đây là phần mềm phòng khám". */}
        <div className="hidden flex-col justify-between bg-primary p-8 text-primary-foreground md:flex">
          <div className="flex items-center gap-3">
            <div className="flex size-11 items-center justify-center rounded-xl bg-primary-foreground/15">
              <Hospital className="size-6" />
            </div>
            <div className="leading-tight">
              <div className="font-bold">Clinic AI</div>
              <div className="text-sm text-primary-foreground/80">Quản lý phòng khám</div>
            </div>
          </div>

          <div className="flex flex-col gap-4">
            <p className="text-lg font-semibold leading-snug">
              Một hệ thống, trọn quy trình khám ngoại trú.
            </p>
            <ul className="flex flex-col gap-2.5">
              {highlights.map(({ icon: Icon, label }) => (
                <li key={label} className="flex items-center gap-2.5 text-sm text-primary-foreground/90">
                  <Icon className="size-4 shrink-0" />
                  {label}
                </li>
              ))}
            </ul>
          </div>

          <p className="text-xs text-primary-foreground/60">Đồ án tốt nghiệp — Clinic Management AI</p>
        </div>

        {/* Form đăng nhập */}
        <div className="flex flex-col justify-center p-6 sm:p-10">
          <div className="mb-6 flex flex-col items-center gap-2 text-center md:hidden">
            <div className="flex size-12 items-center justify-center rounded-xl bg-primary text-primary-foreground">
              <Hospital className="size-7" />
            </div>
            <h1 className="text-xl font-bold">Clinic AI</h1>
          </div>

          <CardHeader className="px-0 pt-0">
            <CardTitle>Đăng nhập</CardTitle>
            <CardDescription>Nhập tài khoản được cấp để vào ca làm việc.</CardDescription>
          </CardHeader>
          <CardContent className="px-0 pb-0">
            <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
              {error && (
                <div className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">
                  {error}
                </div>
              )}

              <div className="grid gap-2">
                <Label htmlFor="username">Tên đăng nhập</Label>
                <Input
                  id="username"
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  autoFocus
                />
              </div>

              <div className="grid gap-2">
                <Label htmlFor="password">Mật khẩu</Label>
                <Input
                  id="password"
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
              </div>

              <Button type="submit" disabled={submitting} className="mt-1 w-full">
                {submitting && <Loader2 className="size-4 animate-spin" />}
                {submitting ? 'Đang đăng nhập…' : 'Đăng nhập'}
              </Button>
            </form>
          </CardContent>
        </div>
      </div>
    </div>
  )
}
