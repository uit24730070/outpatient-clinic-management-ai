# Kiến trúc Frontend (React + TypeScript)

> Cập nhật Sprint 13 (Epic 9 — UI/UX Redesign, [ADR 0012](../adr/0012-tailwind-shadcn-va-chuan-hoa-form.md)).

## Stack
- **React 19 + TypeScript** (tsconfig nghiêm ngặt: `erasableSyntaxOnly`, `verbatimModuleSyntax`,
  `noUnusedLocals/Parameters`), **Vite 8**, lint **oxlint**.
- **TailwindCSS v4 (CSS-first)** + plugin `@tailwindcss/vite` — không dùng `tailwind.config.js`.
  Token màu/brand/radius khai báo trong `src/index.css` (`:root` + `@theme inline`), **primary
  xanh `#2563eb`**, base color slate. Dark mode có sẵn biến (`.dark`) nhưng chưa bật.
- **shadcn/ui** (style new-york): CLI **copy source vào `src/components/ui/`** — không phải
  dependency runtime, ta sở hữu & sửa được. Icon **lucide-react**. Toast **sonner**.
- **react-hook-form + zod** cho toàn bộ form.

## Tổ chức thư mục (`frontend/src/`)
| Thư mục | Vai trò |
|---------|---------|
| `types/` | Kiểu miền, khớp DTO backend. Const-map thay `enum`. |
| `services/` | `apiClient.ts` (axios + `unwrap`/`ApiException`/`toApiException`) + `<feature>Service.ts`. |
| `store/` | `auth.tsx` (context + `useAuth`), dẫn xuất cờ quyền + `doctorId`. |
| `config/access.ts` | **Nguồn sự thật** RBAC/điều hướng: `navItemsFor`, `landingPathFor`, các cờ `canManage*`. |
| `components/ui/` | Component shadcn (sinh bởi CLI). |
| `components/` | Primitive tái dùng: `PageHeader`, `Pager`, `StatusBadge`, `ConfirmDialog`. |
| `lib/` | `utils.ts` (`cn()`), `form.ts` (`applyServerErrors`), `toast.ts`. |
| `layouts/` | `MainLayout` = sidebar + topbar (đọc `navItemsFor`). |
| `pages/` | Trang theo feature. |
| `router/` | react-router 7 (`createBrowserRouter`), guard `RequireAuth`/`RequireRole`. |

## Quy ước
- **Path alias `@/* → src/*`**: khai báo ở **cả** `vite.config.ts` (bundler) **và**
  `tsconfig.json` + `tsconfig.app.json` (`paths`; **không** `baseUrl` — TS 6 deprecate). Import
  shadcn dùng `@/components/ui/...`, `@/lib/utils`.
- **Form (RHF + zod):** `useForm({ resolver: zodResolver(schema), defaultValues })`,
  `useFieldArray` cho dòng động (đơn thuốc, nhập kho). **Tên field RHF đặt khớp property C#
  (camelCase)**. Submit: `try { await service(...) } catch (e) { applyServerErrors(form, e) }`.
- **Lỗi server ↔ RHF:** `applyServerErrors(form, err)` map `ApiException.details` (key PascalCase
  như `FullName`) → field camelCase → `form.setError`; lỗi không có `details` (409/500/network) →
  **toast**. Backend vẫn là nguồn validate cuối; zod chỉ chặn lỗi client hiển nhiên.
- **Trạng thái/badge:** dùng `StatusBadge` (`AppointmentStatusBadge`/`EncounterStatusBadge`/
  `RoleBadge`/`ActiveBadge`/`TonedBadge`) — giữ đúng ngữ nghĩa màu cũ.
- **Xác nhận hành động:** `ConfirmDialog` (bọc `AlertDialog`) thay `window.confirm`; báo thành
  công bằng toast.
- **RBAC là phòng thủ nhiều lớp:** guard FE (`RequireRole`) + ẩn/hiện nút theo cờ chỉ là UX;
  backend vẫn chốt 401/403. FE **không** nới lỏng RBAC.

## Triển khai
- `npm run build` = `tsc -b && vite build` (phải sạch). Docker `web`: Vite build → **nginx** phục
  vụ tĩnh + reverse-proxy `/api`,`/health` → dịch vụ `api` (FE build với `VITE_API_BASE_URL`
  trống → gọi same-origin, không cần CORS).
- `.oxlintrc.json` tắt `react/only-export-components` cho `src/components/ui/**` (code shadcn
  export cả biến lẫn component).
