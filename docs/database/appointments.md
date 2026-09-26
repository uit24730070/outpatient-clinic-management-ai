# Bảng `appointments` — Từ điển dữ liệu

Lịch khám. Sinh ra từ entity `ClinicManagement.Domain.Appointments.Appointment`, migration `AddAppointments`. Có **khoá ngoại kép** tới `patients` và `doctors` (quan hệ many-to-one), và **máy trạng thái** vòng đời (xem [ADR 0005](../adr/0005-mo-hinh-lich-kham-va-may-trang-thai.md)).

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `PatientId` | `uuid` | Không | **FK** → `patients.Id`. Có index. |
| `DoctorId` | `uuid` | Không | **FK** → `doctors.Id`. Có index (kèm `StartTime`). |
| `StartTime` | `timestamptz` | Không | Thời điểm bắt đầu (UTC). |
| `EndTime` | `timestamptz` | Không | Thời điểm kết thúc (UTC), `> StartTime`. |
| `Reason` | `varchar(500)` | Có | Lý do khám. |
| `Status` | `varchar(20)` | Không | Trạng thái vòng đời, lưu **chuỗi** (`Scheduled`/`CheckedIn`/`InProgress`/`Completed`/`Cancelled`/`NoShow`). |
| `CheckedInAt` | `timestamptz` | Có | Thời điểm check-in (đặt khi chuyển sang `CheckedIn`). |
| `ServicePriceId` | `uuid` | Có | Dịch vụ khám lễ tân đăng ký khi đặt lịch (loại `Consultation`); không FK cứng (ADR 0016). |
| `ServiceName` | `varchar(200)` | Có | **Snapshot** tên dịch vụ khám lúc gắn. |
| `ServicePrice` | `numeric(18,2)` | Có | **Snapshot** đơn giá dịch vụ khám lúc gắn. |
| `VisitId` | `uuid` | Có | **FK** → `visits.Id` (`ON DELETE RESTRICT`). Lượt tiếp nhận gom lịch (ADR 0017); `null` với lịch lẻ. Có index. |
| `RoomId` | `uuid` | Có | **FK** → `rooms.Id` (`ON DELETE SET NULL`). Phòng khám gán cho lịch (ADR 0018); `null` nếu chưa gán. Có index. |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (mặc định `false`). Khác `Cancelled` (trạng thái nghiệp vụ). |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

## Index & khoá ngoại
- `PK_appointments` — khóa chính trên `Id`.
- `IX_appointments_DoctorId_StartTime` — hỗ trợ lọc hàng đợi theo bác sĩ + thời gian và kiểm tra chồng lịch.
- `IX_appointments_PatientId` — tra cứu theo bệnh nhân.
- `IX_appointments_VisitId` — gom lịch theo lượt tiếp nhận (ADR 0017).
- `IX_appointments_RoomId` — join/lọc theo phòng (ADR 0018).
- `FK_appointments_doctors_DoctorId`, `FK_appointments_patients_PatientId`, `FK_appointments_visits_VisitId` — `ON DELETE RESTRICT`: không cho xoá (vật lý) bác sĩ/bệnh nhân/lượt khi còn lịch tham chiếu.
- `FK_appointments_rooms_RoomId` — `ON DELETE SET NULL`: xoá phòng chỉ gỡ gán, giữ lịch.

## Quy tắc nghiệp vụ
- Khi tạo, lớp Application **kiểm tra `PatientId` và `DoctorId` tồn tại**; nếu không → `Appointment.PatientNotFound` / `Appointment.DoctorNotFound` (400).
- **Chống trùng lịch:** một bác sĩ không có hai lịch **chồng khung giờ** (`StartTime < b.EndTime && EndTime > b.StartTime`), chỉ xét trạng thái còn chiếm chỗ (`Scheduled`/`CheckedIn`/`InProgress`/`Completed`). Vi phạm → `Appointment.Overlap` (409). Kiểm tra ở tầng service — có cùng hạn chế race như sinh mã ở các bảng khác.
- **Máy trạng thái** (nguồn sự thật ở Domain): `Scheduled → {CheckedIn, Cancelled, NoShow}`; `CheckedIn → {InProgress, Cancelled, NoShow}`; `InProgress → {Completed, Cancelled}`; `Completed`/`Cancelled`/`NoShow` là trạng thái kết thúc. Chuyển sai → `Appointment.InvalidTransition` (409).
- **Dịch vụ khám (ADR 0016):** create/update nhận `servicePriceId?`; nếu có, dịch vụ phải tồn tại và
  thuộc loại `Consultation` (sai → 400 `Appointment.ServiceNotConsultation`) rồi **snapshot** tên/giá.
  `GET /api/appointments/last?patientId=` trả lượt gần nhất để FE prefill dịch vụ khi **tái khám**.
- DTO trả về kèm `patientName`, `doctorName` (join; `null` nếu bản ghi liên quan đã bị xoá mềm).
- Chiến lược xoá mềm: xem [ADR 0003](../adr/0003-chien-luoc-soft-delete.md).
