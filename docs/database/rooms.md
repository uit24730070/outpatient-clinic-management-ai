# Bảng `rooms` — Từ điển dữ liệu

Phòng khám (tài nguyên vật lý). Sinh ra từ entity `ClinicManagement.Domain.Resources.Room`, migration `AddRoomsAndDoctorSchedules` ([ADR 0018](../adr/0018-lich-lam-viec-va-tai-nguyen-phong.md)).

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `Code` | `varchar(20)` | Không | **Unique**. Mã phòng dạng `PK-000001`, sinh tự động tăng dần. |
| `Name` | `varchar(150)` | Không | Tên/số hiệu phòng. |
| `Description` | `varchar(500)` | Có | Mô tả thêm. |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (mặc định `false`). |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

## Index & khoá ngoại
- `PK_rooms` — khóa chính trên `Id`.
- `IX_rooms_Code` — **unique**, trên `Code`.
- Được tham chiếu bởi `doctor_work_schedules.RoomId` và `appointments.RoomId` (cả hai `ON DELETE SET NULL`).

## Quy tắc nghiệp vụ
- Sinh mã `Code = "PK-" + (số phòng hiện có kể cả đã xoá + 1)` (6 chữ số) — đếm cả bản ghi đã xoá để tránh trùng mã.
- **Ngừng dùng phòng = xoá mềm** (không có cờ `IsActive` riêng — [ADR 0018](../adr/0018-lich-lam-viec-va-tai-nguyen-phong.md)).
- RBAC: đọc mọi vai trò; ghi `Roles.ManageStaff` (Admin/Lễ tân).
- Chiến lược xoá mềm: xem [ADR 0003](../adr/0003-chien-luoc-soft-delete.md).
