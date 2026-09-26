# Bảng `doctors` — Từ điển dữ liệu

Hồ sơ bác sĩ. Sinh ra từ entity `ClinicManagement.Domain.Doctors.Doctor`, migration `AddSpecialtiesDoctorsAndSoftDelete` (cột `UserId` thêm ở `AddDoctorUserLink`). Có **khoá ngoại tới `specialties`** (many-to-one) và **liên kết tuỳ chọn 1–1 tới `users`** (`UserId`).

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `Code` | `varchar(20)` | Không | **Unique**. Mã bác sĩ dạng `BS-000001`, sinh tự động tăng dần. |
| `FullName` | `varchar(200)` | Không | Họ tên. Có index. |
| `SpecialtyId` | `uuid` | Không | **FK** → `specialties.Id`. Có index. |
| `PhoneNumber` | `varchar(20)` | Có | Số điện thoại. |
| `Email` | `varchar(200)` | Có | Email. |
| `UserId` | `uuid` | Có | **Unique (bỏ qua NULL)**. **FK** → `users.Id`. Liên kết tài khoản đăng nhập ↔ hồ sơ bác sĩ ([ADR 0009](../adr/0009-phan-quyen-va-trai-nghiem-giao-dien-theo-vai-tro.md)). `NULL` = chưa gắn tài khoản. |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (mặc định `false`). |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

## Index & khoá ngoại
- `PK_doctors` — khóa chính trên `Id`.
- `IX_doctors_Code` — **unique**, trên `Code`.
- `IX_doctors_FullName` — hỗ trợ tìm kiếm theo tên.
- `IX_doctors_SpecialtyId` — hỗ trợ join/lọc theo chuyên khoa.
- `IX_doctors_UserId` — **unique** trên `UserId` (Postgres cho phép nhiều `NULL` → nhiều bác sĩ chưa gắn tài khoản không xung đột; đã gắn thì 1–1).
- `FK_doctors_specialties_SpecialtyId` — `ON DELETE RESTRICT`: không cho xoá (vật lý) chuyên khoa khi còn bác sĩ tham chiếu.
- `FK_doctors_users_UserId` — `ON DELETE SET NULL`: xoá tài khoản chỉ gỡ liên kết, giữ lại hồ sơ bác sĩ.

## Quy tắc nghiệp vụ
- Khi tạo/sửa, lớp Application **kiểm tra `SpecialtyId` tồn tại**; nếu không → lỗi `Doctor.SpecialtyNotFound` (400 Validation).
- DTO trả về kèm `specialtyName` (join từ `specialties`); `null` nếu chuyên khoa đã bị xoá mềm.
- Sinh mã `Code = "BS-" + (số bác sĩ hiện có kể cả đã xoá + 1)` định dạng 6 chữ số (đếm cả bản ghi đã xoá để tránh trùng mã). Cùng hạn chế race như bảng `patients`.
- **Liên kết tài khoản** (`UserId`): một tài khoản (`users.Id`) gắn tối đa một hồ sơ bác sĩ. Lộ `doctorId` của người đăng nhập qua `GET /api/auth/me` (tra cứu server-side, không nhét claim JWT) để bác sĩ lọc "lịch/phiếu của tôi". Xem [ADR 0009](../adr/0009-phan-quyen-va-trai-nghiem-giao-dien-theo-vai-tro.md).
- **Seed demo:** migration `AddDoctorUserLink` tạo tài khoản Bác sĩ `bacsi` (`Doctor@123`) gắn hồ sơ `BS-000001` "Bác sĩ Demo" (chuyên khoa Nội tổng quát).
- Chiến lược xoá mềm: xem [ADR 0003](../adr/0003-chien-luoc-soft-delete.md).
