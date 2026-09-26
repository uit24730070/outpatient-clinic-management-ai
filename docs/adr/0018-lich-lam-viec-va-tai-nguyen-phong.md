# 0018. Lịch làm việc bác sĩ & tài nguyên phòng khám

- Trạng thái: Accepted
- Ngày: 2026-09-12

## Bối cảnh
Sprint 18 mở **Epic 12 — Tài nguyên & Lịch làm việc**: quản lý **phòng khám** và **khung giờ làm việc của bác sĩ**, đồng thời ràng buộc **đặt lịch** phải nằm trong khung giờ làm việc và cho phép **gán phòng**. Đây là nền cho hàng đợi (Sprint 19). Cần chốt: (1) mô hình lịch làm việc; (2) cách ràng buộc đặt lịch + quy ước múi giờ; (3) mô hình phòng + gán phòng; (4) phạm vi (phần nợ).

## Quyết định

### 1. Phòng khám (`Room`) — vertical slice CRUD
- Entity `Domain/Resources/Room`: `Code` (mã `PK-000001`, sinh tuần tự đếm cả bản ghi đã xoá — như `Medication`/`Doctor`), `Name`, `Description?`.
- **Chỉ soft delete** để ngừng dùng phòng — **không** thêm cờ `IsActive` (tránh lặp hai cờ vòng đời như `User`).
- RBAC: đọc **mọi vai trò** (form đặt lịch/lịch làm việc cần tra); ghi `Roles.ManageStaff` (Admin/Lễ tân).

### 2. Lịch làm việc bác sĩ (`DoctorWorkSchedule`) — mẫu lặp theo tuần
- Entity `Domain/Doctors/DoctorWorkSchedule`: `DoctorId` × `DayOfWeek` (System.DayOfWeek, **lưu chuỗi**) × `StartTime`/`EndTime` (`TimeOnly` → cột `time`, giờ **địa phương** phòng khám) × `RoomId?`.
- **Đơn giản = mẫu tuần**; ngày nghỉ/đột xuất (override) để phần nợ.
- CRUD theo bác sĩ: `GET/POST/PUT/DELETE /api/doctors/{id}/schedules`. Chặn **khung chồng nhau** trong cùng bác sĩ/thứ → `Doctor.ScheduleOverlap` (409). Kiểm phòng tồn tại nếu gắn (`Room.NotFound`). RBAC `ManageStaff`.

### 3. Ràng buộc đặt lịch + quy ước múi giờ
- Mở rộng `AppointmentService` (Create/Update): giờ khám phải nằm trong **một** khung làm việc của bác sĩ hôm đó → nếu không: `Appointment.OutsideWorkingHours` (409). Giữ nguyên chống trùng giờ (Sprint 4).
- **Bác sĩ chưa khai lịch làm việc nào ⇒ không ràng buộc** (đặt tự do) — tương thích ngược lịch/bác sĩ cũ.
- **Múi giờ:** lịch khám lưu `timestamptz` (UTC); khung làm việc lưu `time` giờ địa phương. Quy đổi bằng **hằng số UTC+7** (`ClinicOffset`, Việt Nam không có DST) khi so khung. Khung khám không vắt qua nửa đêm (giữ đơn giản).
- **Gán phòng:** `Appointment.RoomId?` nullable (ctor tham số mặc định — tương thích Sprint 4); chọn tay ở form. FK `SetNull` khi xoá phòng.

### 4. Phạm vi (phần nợ)
- **Gợi ý khung trống** (`GET availability` = khung làm việc − lịch đã đặt) — chưa làm; sprint này chỉ **chặn** giờ ngoài khung.
- Ngày nghỉ lễ/nghỉ phép đột xuất (override theo ngày cụ thể).
- Ràng buộc **một phòng không hai bác sĩ cùng giờ** (mới kiểm ở mức bác sĩ).
- Ràng buộc giờ làm việc **chưa áp cho `VisitService`** (tiếp đón walk-in) — chấp nhận (bệnh nhân đến tận nơi).

## Hệ quả
- **Ưu:** có nền tài nguyên (phòng) + lịch làm việc cho hàng đợi Sprint 19; đặt lịch tôn trọng giờ làm việc; tương thích ngược hoàn toàn (không schedule → tự do; lịch cũ không phòng không gãy).
- **Nhược/đánh đổi:**
  - Chống chồng khung + chống trùng lịch kiểm ở service (có race như các slice trước) — chấp nhận ở quy mô đồ án; có thể siết constraint sau.
  - Múi giờ cố định UTC+7 (không cấu hình đa cơ sở) — đủ cho một phòng khám.
  - Ràng buộc giờ chỉ ở `AppointmentService`, không ở `VisitService` — walk-in bỏ qua có chủ đích.
