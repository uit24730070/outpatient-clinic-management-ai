# 0019. Điều dưỡng, sinh hiệu & hàng đợi khám

- Trạng thái: Accepted
- Ngày: 2026-09-12

## Bối cảnh
Sprint 19 mở **Epic 13 — Tiếp đón lâm sàng & Hàng đợi**, khép vòng đời khâu đầu của bệnh nhân: tiếp đón → **hàng đợi/số thứ tự** → **sinh hiệu** → vào khám. Cần thêm **vai trò Điều dưỡng (Nurse)** và chốt: (1) mô hình vai trò + RBAC; (2) mô hình sinh hiệu (gắn đâu, ai ghi/đọc); (3) mô hình hàng đợi (cấp số, máy trạng thái, vãng lai, phạm vi số); (4) nguồn sự thật "đang khám" khi có nhiều máy trạng thái; (5) phạm vi (phần nợ).

## Quyết định

### 1. Vai trò Điều dưỡng (`Nurse`) — theo mẫu ADR 0013
- `UserRole.Nurse = 5` (lưu **chuỗi**, không đổi schema). `Roles.Nurse`.
- `Roles.RecordVitals = Admin + Nurse` (ghi sinh hiệu); `Roles.ManageQueue = Admin + Receptionist + Nurse` (điều phối hàng đợi).
- Điều dưỡng **không** ghi bệnh án/đơn thuốc (`RecordEncounter` không đổi). Đọc thông tin lâm sàng cần thiết (lịch khám, bệnh nhân) mở cho mọi vai trò.
- Seed user demo `dieuduong`/`Nurse@123` (BCrypt workFactor 11 tất định), Guid `cccccccc-…` (tránh trùng `duocsi`=`eeee…`). FE đồng bộ: `types/auth.ts` (label "Điều dưỡng"), `config/access.ts` (`canRecordVitals`/`canManageQueue`, nav "Hàng đợi"/"Sinh hiệu", landing = `/queue`), `StatusBadge` (badge cyan), router (`MANAGE_QUEUE`/`RECORD_VITALS`, Nurse ∈ `ALL_ROLES`).

### 2. Sinh hiệu (`Vitals`) — gắn lượt khám (1–1)
- Entity `Domain/Clinical/Vitals`: FK **`AppointmentId` unique** (một bộ sinh hiệu mỗi lượt), snapshot `PatientId`; các chỉ số **nullable** (`HeightCm`/`WeightKg`/`TemperatureC`/`Pulse`/`BloodPressureSystolic`/`BloodPressureDiastolic`/`SpO2`/`RespiratoryRate`/`Notes`), `MeasuredAt`/`MeasuredBy` (Id tài khoản đo).
- **Gắn `Appointment`, KHÔNG gắn `Encounter`**: đo **sau check-in, trước** khi bác sĩ tạo phiếu khám (Encounter chưa tồn tại lúc đo). Bác sĩ đọc theo `AppointmentId` (Encounter 1–1 Appointment — Sprint 5).
- **BMI là thuộc tính tính toán** (`Bmi = WeightKg / (HeightCm/100)²`, làm tròn 1 chữ số), **không lưu cột** (`builder.Ignore`). Vì không map SQL được, `VitalsService` **ánh xạ DTO trong bộ nhớ** (không projection), tra tên người đo bằng truy vấn phụ.
- **Upsert theo lượt**: `POST /api/appointments/{id}/vitals` tạo mới nếu chưa có, ngược lại cập nhật (`Vitals.Update`). `GET` trả `data=null` nếu chưa đo. RBAC ghi `RecordVitals`, đọc mở cho lâm sàng.

### 3. Hàng đợi (`QueueTicket`) — số thứ tự theo ngày
- Entity `Domain/Queue/QueueTicket`: `TicketDate` (`DateOnly`), `Number` (tuần tự trong ngày), snapshot `PatientId`, `AppointmentId?` (**vãng lai = null**), `RoomId?`/`DoctorId?`, `Status`, `CalledAt?`.
- **Phạm vi cấp số = toàn phòng khám theo ngày** (không theo phòng/bác sĩ) — đơn giản, phù hợp phòng khám nhỏ, dễ mở rộng thành bảng gọi số. `Number = COUNT(vé trong ngày, kể cả đã xoá) + 1` (`IgnoreQueryFilters` — chống trùng như mã `BN-`).
- **Múi giờ "hôm nay"** quy đổi hằng số **UTC+7** (`ClinicOffset`, thống nhất ADR 0018) khi cấp/lọc số.
- **Máy trạng thái** ở Domain: `Waiting → Called → InProgress → Done`; `Waiting/Called → Skipped`. Chuyển sai → `Queue.InvalidTransition` (409). `Call()` đặt `CalledAt`. `Assign(roomId, doctorId)` để điều phối.
- Endpoints: `POST /api/queue` (lấy số, hỗ trợ vãng lai), `GET /api/queue?date=&roomId=&doctorId=&status=` (mặc định hôm nay, sắp theo số), `POST /{id}/assign`, `POST /{id}/{call|start|done|skip}`. RBAC `ManageQueue`.

### 4. Nguồn sự thật "đang khám" (3 máy trạng thái)
Một lượt có thể chạm **ba** trạng thái: `Appointment` (`CheckedIn/InProgress`, ADR 0005), `QueueTicket` (`Called/InProgress`, sprint này), `Encounter` (Sprint 5). **Chốt:** `Appointment.Status` là **nguồn sự thật lâm sàng**; `QueueTicket.Status` chỉ phản ánh trạng thái **hàng đợi** (best-effort), **không nhân đôi** logic lâm sàng. Hai bên độc lập — không tự đồng bộ chéo ở sprint này (tránh coupling & lệch trạng thái).

### 5. Phạm vi (phần nợ)
- **Bảng gọi số / màn hình chờ (kiosk/TV)**, loa gọi số tự động — cắt (màn thao tác hàng đợi đã đủ chứng minh nghiệp vụ).
- Cờ **ưu tiên** (cấp cứu/người già), đặt lại số.
- **Cờ bất thường sinh hiệu** theo tuổi–giới, biểu đồ xu hướng sinh hiệu.
- ~~Tự tạo vé khi check-in~~ — **đã làm** (Sprint 24): `VisitService.CreateAsync`/`AddServiceAsync` tự cấp `QueueTicket` ngay lúc tạo lượt/thêm dịch vụ (không đợi `Appointment.CheckIn`), `RoomId` suy tự động từ `DoctorWorkSchedule` khớp thứ/khung giờ của bác sĩ; null nếu bác sĩ chưa khai khung làm việc khớp giờ hẹn (lễ tân điều phối tay như cũ qua `POST /{id}/assign`).

## Hệ quả
- **Ưu:** khép vòng đời tiếp đón (số thứ tự + sinh hiệu); vai trò Điều dưỡng theo đúng mẫu ADR 0013; sinh hiệu tái dùng quan hệ 1–1 với `Appointment` nên bác sĩ đọc ngay trong bệnh án; hàng đợi hỗ trợ vãng lai không cần lịch trước.
- **Nhược/đánh đổi:**
  - Cấp số tuần tự kiểm ở service (có race như mã `BN-`) — chấp nhận ở quy mô đồ án.
  - Ba máy trạng thái không tự đồng bộ — điều dưỡng/lễ tân cập nhật hàng đợi thủ công; đổi lấy tính đơn giản & không lệch logic.
  - BMI tính trong bộ nhớ (không lọc/sắp theo BMI ở SQL) — không cần thiết ở quy mô này.
  - Guard FE (`RequireRole`) chỉ là UX — backend vẫn chốt 401/403.
