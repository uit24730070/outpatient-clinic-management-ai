# Bảng `queue_tickets` — Từ điển dữ liệu

Vé hàng đợi khám / số thứ tự trong ngày. Sinh ra từ entity `ClinicManagement.Domain.Queue.QueueTicket`, migration `AddVitalsAndQueue` ([ADR 0019](../adr/0019-dieu-duong-sinh-hieu-va-hang-doi.md)).

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng. |
| `TicketDate` | `date` | Không | Ngày cấp số (giờ địa phương phòng khám). |
| `Number` | `integer` | Không | Số thứ tự trong ngày (bắt đầu từ 1, phạm vi toàn phòng khám). |
| `PatientId` | `uuid` | Không | Snapshot bệnh nhân. |
| `AppointmentId` | `uuid` | Có | FK → `appointments` (`SET NULL`). Null với **khách vãng lai**. |
| `RoomId` | `uuid` | Có | FK → `rooms` (`SET NULL`). Phòng gán cho vé. |
| `DoctorId` | `uuid` | Có | FK → `doctors` (`SET NULL`). Bác sĩ gán cho vé. |
| `Status` | `varchar(20)` | Không | Enum chuỗi: `Waiting`/`Called`/`InProgress`/`Done`/`Skipped`. |
| `CalledAt` | `timestamptz` | Có | Thời điểm gọi số (đặt khi → `Called`). |
| `CreatedAt` / `UpdatedAt` | `timestamptz` | Không / Có | Dấu thời gian kiểm toán. |
| `IsDeleted` / `DeletedAt` | `boolean` / `timestamptz` | Không / Có | Xoá mềm. |

## Index & khoá ngoại
- `PK_queue_tickets` — khóa chính trên `Id`.
- `IX_queue_tickets_TicketDate_Status` — lọc theo ngày + trạng thái (truy vấn chính).
- `IX_queue_tickets_TicketDate_RoomId`, `IX_queue_tickets_TicketDate_DoctorId` — lọc theo phòng/bác sĩ.
- FK tới `appointments`/`rooms`/`doctors` đều `ON DELETE SET NULL`.

## Quy tắc nghiệp vụ
- **Cấp số**: `Number = COUNT(vé cùng ngày, kể cả đã xoá — `IgnoreQueryFilters`) + 1` → chống trùng như mã `BN-`. Phạm vi = **toàn phòng khám theo ngày**.
- **Múi giờ "hôm nay"**: quy đổi hằng số UTC+7 (`ClinicOffset`).
- **Máy trạng thái** (Domain): `Waiting → Called → InProgress → Done`; `Waiting/Called → Skipped`. Chuyển sai → `Queue.InvalidTransition` (409).
- **Vãng lai**: `AppointmentId` null (lấy số không cần lịch trước).
- `QueueTicket.Status` chỉ phản ánh trạng thái **hàng đợi** — không phải nguồn sự thật lâm sàng (đó là `Appointment.Status`, [ADR 0019](../adr/0019-dieu-duong-sinh-hieu-va-hang-doi.md)).
- **Tự động cấp lúc tiếp nhận** (`VisitService.CreateAsync`/`AddServiceAsync`): mỗi dịch vụ khám (Appointment) trong lượt được cấp một vé ngay, `RoomId` suy tự động từ `DoctorWorkSchedule` khớp thứ/khung giờ của bác sĩ tại thời điểm hẹn (null nếu bác sĩ chưa có khung làm việc khớp).
- RBAC: `Roles.ManageQueue` (Admin/Lễ tân/Điều dưỡng) cho mọi thao tác ghi; đọc mở cho mọi vai trò đã đăng nhập.
