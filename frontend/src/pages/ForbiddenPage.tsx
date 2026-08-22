import { Link } from 'react-router-dom'
import { ShieldX } from 'lucide-react'
import { useAuth } from '../store/auth'
import { landingPathFor } from '../config/access'
import { Button } from '@/components/ui/button'

/** Trang 403 thân thiện khi truy cập route ngoài quyền (khớp 403 backend). */
export default function ForbiddenPage() {
  const { user } = useAuth()
  const home = landingPathFor(user?.role)

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-4 text-center">
      <div className="flex size-16 items-center justify-center rounded-full bg-destructive/10 text-destructive">
        <ShieldX className="size-8" />
      </div>
      <div>
        <h1 className="text-2xl font-bold">Không có quyền truy cập</h1>
        <p className="mt-1 text-muted-foreground">
          Bạn không được phép xem trang này với vai trò hiện tại.
        </p>
      </div>
      <Button asChild>
        <Link to={home}>← Về trang chính của bạn</Link>
      </Button>
    </div>
  )
}
