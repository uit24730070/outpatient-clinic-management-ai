# 0004. Chiến lược xác thực & phân quyền (Auth & RBAC)

- Trạng thái: Accepted
- Ngày: 2026-08-08

## Bối cảnh
Sprint 3 đưa **xác thực (authentication)** và **phân quyền theo vai trò (RBAC)** vào hệ thống, làm nền tảng bảo mật cho mọi tính năng sau (đặt lịch, bệnh án). Yêu cầu:
- Đăng nhập bằng tài khoản nội bộ (username + mật khẩu), phát token cho client React (SPA).
- Ba vai trò cố định: **Admin**, **Lễ tân (Receptionist)**, **Bác sĩ (Doctor)**.
- Giữ nguyên phong cách Clean Architecture của dự án (Domain thuần, Application điều phối, Infrastructure hiện thực hạ tầng) và envelope `ApiResponse` thống nhất.

## Các phương án

### Cơ chế xác thực
1. **JWT tự phát hành + `User` tự định nghĩa (kế thừa `Entity`)** — tự quản lý bảng người dùng, tự băm mật khẩu, tự phát token bằng `Microsoft.AspNetCore.Authentication.JwtBearer`. Nhẹ, khớp phong cách entity hiện có (được soft delete, audit sẵn), toàn quyền kiểm soát claim.
2. **ASP.NET Core Identity** — bộ khung đầy đủ (UserManager, RoleManager, store EF, xác nhận email, khoá tài khoản, lockout…). Mạnh nhưng nặng: kéo theo schema Identity riêng (không kế thừa `Entity`/soft delete của dự án), nhiều bảng, API stateful hơn, lệch với pattern service `Result<T>` đang dùng.

### Băm mật khẩu
- **BCrypt** (`BCrypt.Net-Next`) — hàm băm thích ứng, có salt tự sinh, chuẩn công nghiệp, API tối giản (`HashPassword`/`Verify`). So với PBKDF2 của Identity thì độc lập, dễ dùng ngoài Identity.

### Mô hình vai trò
- **Enum `UserRole` lưu chuỗi** (`Admin`/`Receptionist`/`Doctor`), mỗi user **một vai trò** — đơn giản, đủ cho 3 vai trò cố định, đồng nhất với cách map `Gender` (`HasConversion<string>()`).
- Entity `Role` + bảng nối many-to-many — linh hoạt (nhiều vai trò, phân quyền động) nhưng thừa ở quy mô đồ án.

## Quyết định
Chọn **JWT tự phát hành + `User` tự định nghĩa + BCrypt + `UserRole` enum (một vai trò/user)**.

- **Domain:** `User : Entity` với `Username` (unique), `Email?`, `PasswordHash`, `FullName`, `Role` (`UserRole`), `IsActive`. Phương thức nghiệp vụ: `ChangePassword`, `Deactivate`/`Activate`. Được soft delete sẵn qua `Entity`.
- **Phân biệt khoá vs xoá:** `IsActive = false` là **khoá đăng nhập** (tài khoản còn tồn tại); `IsDeleted = true` là **xoá mềm**. Đăng nhập chặn cả hai.
- **Băm mật khẩu:** abstraction `IPasswordHasher` (Application), hiện thực `BCryptPasswordHasher` (Infrastructure).
- **Phát token:** abstraction `IJwtTokenGenerator` (Application), hiện thực đọc `JwtSettings` (Issuer, Audience, Key, ExpiresMinutes). Token HS256, claim gồm `sub` (user id), `name`, `role`, `unique_name` (username). Key ≥ 32 byte.
- **RBAC:** dùng role claim + `[Authorize(Roles = "...")]` sẵn có của ASP.NET Core (không cần policy tuỳ biến ở giai đoạn này). Quy ước: ai đăng nhập cũng **đọc** được danh mục; chỉ **Admin/Lễ tân** được **ghi** (tạo/sửa/xoá) Bệnh nhân/Bác sĩ/Chuyên khoa.
- **Envelope 401/403:** override `JwtBearerEvents.OnChallenge`/`OnForbidden` để body giữ chuẩn `ApiResponse` (`code = "Auth.Unauthorized"` / `"Auth.Forbidden"`), thay vì body rỗng mặc định.
- **Seed:** một tài khoản Admin khởi tạo bằng `HasData`, **hash BCrypt băm sẵn tất định** dán cứng vào seed (BCrypt có salt ngẫu nhiên nên không băm lúc chạy migration).

## Hệ quả
- **Ưu:** nhẹ, khớp Clean Architecture và pattern `Result<T>`/`ApiResponse`; toàn quyền kiểm soát claim & luồng lỗi; `User` hưởng sẵn audit + soft delete; kiểm thử service bằng InMemory như các slice khác.
- **Nhược/đánh đổi & lưu ý:**
  - Tự làm nên **thiếu sẵn** các tính năng của Identity: refresh token, lockout, xác nhận email, quên mật khẩu — sẽ bổ sung sau nếu nghiệp vụ cần (đã ghi ngoài phạm vi Sprint 3).
  - **Bảo mật khoá JWT:** `JwtSettings:Key` không commit giá trị thật; dùng giá trị dev trong cấu hình local/User Secrets, môi trường thật đặt qua biến môi trường/secret store.
  - Một vai trò/user: nếu sau này cần user kiêm nhiều vai trò phải migrate sang bảng nối — chấp nhận ở quy mô hiện tại.
  - Seed hash tất định: khi đổi mật khẩu Admin mặc định phải sinh lại hash và cập nhật migration seed.
</content>
</invoke>
