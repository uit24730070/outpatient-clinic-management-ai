# 0012. TailwindCSS v4 + shadcn/ui + react-hook-form/zod cho lớp trình bày

- Trạng thái: Accepted
- Ngày: 2026-08-22

## Bối cảnh

Từ Sprint 1 đến 12, frontend dùng **CSS thuần viết tay** (`src/index.css` ~290 dòng với các
class `.btn`, `.table`, `.badge--*`, `.form`, `.field`…) và **form quản lý bằng `useState` thủ
công** (mỗi input một state, lỗi field map tay từ `ApiException.details`). Cách này chạy được
nhưng:

- Giao diện thiếu nhất quán, "tạm bợ" — không đạt chuẩn một ứng dụng quản lý chuyên nghiệp.
- Mỗi form lặp lại nhiều boilerplate (state + handler + hiển thị lỗi), dễ lệch nhau.
- Không có hệ thống component tái dùng (nút, bảng, badge, dialog, toast) → mỗi trang tự chế.

Sprint 13 (Epic 9 — UI/UX Redesign) là sprint **thuần trình bày**: không đổi backend, không thêm
nghiệp vụ, không đổi RBAC/luồng dữ liệu.

## Quyết định

1. **TailwindCSS v4 (CSS-first)** làm nền styling: cấu hình bằng `@import "tailwindcss"` +
   `@theme inline` trong `src/index.css` + plugin `@tailwindcss/vite` (không dùng
   `tailwind.config.js`). Token màu/brand/radius khai báo dưới dạng biến CSS (`:root`), giữ tông
   **primary xanh `#2563eb`** để không lệch nhận diện. Có sẵn biến `.dark` (dark mode để ngỏ,
   **không thuộc DoD sprint này**).

2. **shadcn/ui (new-york, base slate)** cho bộ component: CLI **copy source vào
   `src/components/ui/`** (ta sở hữu và sửa được — **không phải dependency runtime**). Icon dùng
   **lucide-react**. Helper `cn()` (`clsx` + `tailwind-merge`) ở `src/lib/utils.ts`.

3. **Path alias `@/* → src/*`** khai báo ở **cả** `vite.config.ts` (bundler) **và**
   `tsconfig.json`/`tsconfig.app.json` (`paths`, không dùng `baseUrl` vì TS 6 deprecate).

4. **Chuẩn hoá form sang react-hook-form + zod**: mỗi form có **schema zod** (`zodResolver`),
   `useFieldArray` cho dòng động (đơn thuốc, nhập kho). Lỗi field từ server vẫn map qua helper
   **`applyServerErrors(form, err)`** (`src/lib/form.ts`): key **PascalCase**
   (`ApiException.details`) → field **camelCase** của RHF → `form.setError`; lỗi không có
   `details` (409/500/network) → **toast** (`sonner`). **Tên field RHF đặt khớp property C#**
   (camelCase) để `applyServerErrors` ánh xạ đúng.

5. **App shell mới**: `MainLayout` = **sidebar điều hướng** (đọc `navItemsFor` từ
   `config/access.ts` — vẫn là nguồn sự thật RBAC/menu) **+ topbar** (avatar + `DropdownMenu`
   đăng xuất), responsive bằng `Sheet` trên mobile. Primitive tái dùng ở `src/components/`:
   `PageHeader`, `Pager`, `StatusBadge` (map trạng thái lịch/phiếu/vai trò → biến thể `Badge`),
   `ConfirmDialog` (bọc `AlertDialog` thay `window.confirm`).

## Hệ quả

**Ưu điểm**
- Giao diện nhất quán, chuyên nghiệp; thêm màn mới nhanh nhờ component + primitive tái dùng.
- Form ngắn gọn, ít boilerplate; validate client (zod) tức thời, thông báo lỗi thống nhất.
- shadcn source nằm trong repo → tuỳ biến tự do, không khoá vào version thư viện.

**Nhược điểm / đánh đổi**
- **Bundle FE lớn hơn** (kéo `@radix-ui/*`, RHF, zod) — chấp nhận cho app nội bộ; có thể
  code-split sau nếu cần.
- **Rủi ro hồi quy form** khi chuyển toàn bộ sang RHF — giảm thiểu bằng migrate từng form + build
  thường xuyên; **giữ nguyên service/endpoint/DTO request**, tên field khớp DTO để dữ liệu gửi
  lên không lệch.
- **zod vs FluentValidation**: schema zod chỉ chặn lỗi client hiển nhiên; **backend vẫn là nguồn
  validate cuối** — không nhân bản mọi rule để tránh lệch thông báo.
- **Không nới lỏng RBAC**: guard FE chỉ là UX; backend vẫn chốt 401/403. Sidebar chỉ *đọc*
  `config/access.ts`.
- Code shadcn sinh ra phải sửa cho hợp **tsconfig nghiêm ngặt** (`verbatimModuleSyntax` →
  `import type`; `sonner` bỏ phụ thuộc `next-themes` vì chỉ dùng light mode).

## Ghi chú triển khai
- shadcn CLI trên Windows tạo nhầm thư mục literal `@` khi alias chưa vào `tsconfig.json` gốc →
  đã thêm `paths` vào cả `tsconfig.json`.
- Docker `web` (Vite build → nginx) **không cần đổi**: Tailwind biên dịch lúc `npm run build`.
