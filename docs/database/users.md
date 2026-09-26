# Bảng `users` — Từ điển dữ liệu

Tài khoản người dùng đăng nhập hệ thống. Sinh ra từ entity `ClinicManagement.Domain.Users.User`, migration `AddUsers`. Nền tảng cho xác thực (JWT) và phân quyền theo vai trò (RBAC) — xem [ADR 0004](../adr/0004-chien-luoc-xac-thuc-va-phan-quyen.md).

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `Username` | `varchar(100)` | Không | **Unique**. Tên đăng nhập (so khớp không phân biệt hoa/thường khi login). |
| `PasswordHash` | `varchar(200)` | Không | Mật khẩu đã băm bằng **BCrypt** (kèm salt). Không bao giờ trả ra client. |
| `FullName` | `varchar(200)` | Không | Họ tên hiển thị. |
| `Role` | `varchar(20)` | Không | Vai trò: `Admin` / `Receptionist` / `Doctor` (enum lưu chuỗi). |
| `Email` | `varchar(200)` | Có | Email (tuỳ chọn). |
| `IsActive` | `boolean` | Không | Cho phép đăng nhập hay không (**khoá mềm**, khác xoá mềm). |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (mặc định `false`). |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

## Index

- `PK_users` — khóa chính trên `Id`.
- `IX_users_Username` — **unique**, trên `Username`.

## Dữ liệu seed

Hai tài khoản khởi tạo (qua `HasData`):

| `Id` | `Username` | Mật khẩu | `Role` | Ghi chú |
|------|-----------|----------|--------|---------|
| `aaaaaaaa-…-aaaa` | `admin` | `Admin@123` | `Admin` | Quản trị hệ thống. |
| `dddddddd-…-dddd` | `bacsi` | `Doctor@123` | `Doctor` | Gắn hồ sơ bác sĩ demo `BS-000001` (ADR 0009). |

> Cả hai hash BCrypt tất định băm sẵn, dán cứng vào seed.

> BCrypt có salt ngẫu nhiên nên **không băm lúc chạy migration** (mỗi lần khác nhau, vỡ migration). Hash được băm sẵn một lần rồi dán vào cấu hình seed. Khi đổi mật khẩu Admin mặc định phải sinh lại hash và cập nhật migration.

## Quy tắc nghiệp vụ

- **Phân biệt khoá vs xoá:** `IsActive = false` là khoá đăng nhập (tài khoản còn tồn tại); `IsDeleted = true` là xoá mềm. **Đăng nhập bị chặn ở cả hai trạng thái**.
- Login sai (không tồn tại / bị khoá / sai mật khẩu) đều trả cùng lỗi `Auth.InvalidCredentials` (401) để **không tiết lộ tài khoản nào tồn tại**.
- Vai trò được đưa vào **claim `role`** khi phát JWT; controller dùng `[Authorize(Roles=...)]` để phân quyền.
- Chiến lược xoá mềm chung: xem [ADR 0003](../adr/0003-chien-luoc-soft-delete.md).

## Quản lý người dùng (API — Admin) — Sprint 9

Nhóm endpoint `/api/users` **chỉ Admin** (`[Authorize(Roles = Admin)]`); không endpoint nào lộ `PasswordHash`. Vai trò trong body nhận **chuỗi** (`"Admin"`/`"Receptionist"`/`"Doctor"`).

| Method | Đường dẫn | Mô tả |
|--------|-----------|-------|
| `POST` | `/api/users` | Tạo tài khoản (username unique → `409 User.UsernameTaken`; băm mật khẩu lúc chạy). |
| `GET` | `/api/users?page=&pageSize=&search=&role=&isActive=` | Danh sách có phân trang, lọc theo vai trò/trạng thái. |
| `GET` | `/api/users/{id}` | Chi tiết một tài khoản (kèm `doctorId` nếu đã gắn). |
| `PUT` | `/api/users/{id}` | Cập nhật họ tên/vai trò/email (không đổi username/mật khẩu). |
| `POST` | `/api/users/{id}/reset-password` | Đặt lại mật khẩu (body `{ newPassword }`). |
| `POST` | `/api/users/{id}/activate` | Mở khoá đăng nhập. |
| `POST` | `/api/users/{id}/deactivate` | Khoá đăng nhập — **chặn tự khoá mình** (`400 User.CannotDeactivateSelf`). |
| `DELETE` | `/api/users/{id}` | Xoá mềm — **chặn tự xoá mình** (`400 User.CannotDeleteSelf`); tự gỡ liên kết hồ sơ bác sĩ nếu có. |

### Gắn/gỡ liên kết `User↔Doctor` (U-D-02)

Đặt trên hồ sơ bác sĩ (Admin):

| Method | Đường dẫn | Lỗi có thể gặp |
|--------|-----------|----------------|
| `POST` | `/api/doctors/{id}/link-user` (body `{ userId }`) | `404 User.NotFound` · `400 Doctor.UserNotDoctor` (user không phải vai trò Doctor) · `409 Doctor.UserAlreadyLinked` (user đã gắn hồ sơ khác) · `409 Doctor.AlreadyLinked` (hồ sơ đã gắn tài khoản). |
| `POST` | `/api/doctors/{id}/unlink-user` | `404 Doctor.NotFound`. |

Quan hệ **1–1**: `Doctor.UserId` unique (nullable → nhiều NULL không xung đột). Chốt chặn ở DB là unique index; service kiểm trước cho thông báo thân thiện. Xem [ADR 0009](../adr/0009-phan-quyen-va-trai-nghiem-giao-dien-theo-vai-tro.md).
</content>
</invoke>
