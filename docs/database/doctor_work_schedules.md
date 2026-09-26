# Bảng `doctor_work_schedules` — Từ điển dữ liệu

Khung giờ làm việc của bác sĩ theo **mẫu lặp hằng tuần**. Sinh ra từ entity `ClinicManagement.Domain.Doctors.DoctorWorkSchedule`, migration `AddRoomsAndDoctorSchedules` ([ADR 0018](../adr/0018-lich-lam-viec-va-tai-nguyen-phong.md)).

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. |
| `DoctorId` | `uuid` | Không | **FK** → `doctors.Id`. |
| `DayOfWeek` | `varchar(20)` | Không | Thứ trong tuần (System.DayOfWeek lưu **chuỗi**: `Sunday`…`Saturday`). |
| `StartTime` | `time` | Không | Giờ bắt đầu (giờ **địa phương** phòng khám). |
| `EndTime` | `time` | Không | Giờ kết thúc (> `StartTime`). |
| `RoomId` | `uuid` | Có | **FK** → `rooms.Id` (tuỳ chọn). |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm. |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

## Index & khoá ngoại
- `PK_doctor_work_schedules` — khóa chính trên `Id`.
- `IX_doctor_work_schedules_DoctorId_DayOfWeek` — hỗ trợ tra khung theo bác sĩ + thứ.
- `IX_doctor_work_schedules_RoomId` — hỗ trợ join phòng.
- `FK_doctor_work_schedules_doctors_DoctorId` — `ON DELETE RESTRICT`.
- `FK_doctor_work_schedules_rooms_RoomId` — `ON DELETE SET NULL`.

## Quy tắc nghiệp vụ
- Validator: `EndTime > StartTime`; `DayOfWeek` hợp lệ.
- Service chặn **khung chồng nhau** trong cùng bác sĩ/thứ → `Doctor.ScheduleOverlap` (409). Kiểm phòng tồn tại nếu gắn (`Room.NotFound`).
- CRUD theo bác sĩ: `GET/POST/PUT/DELETE /api/doctors/{id}/schedules`; RBAC `Roles.ManageStaff`.
- Dùng để **ràng buộc đặt lịch**: giờ khám (UTC, quy đổi về UTC+7) phải nằm trong một khung của bác sĩ hôm đó, nếu không → `Appointment.OutsideWorkingHours` (409). Bác sĩ **chưa khai lịch nào** ⇒ không ràng buộc.
