# 0006. Mô hình bệnh án (Encounter / Prescription)

- Trạng thái: Accepted
- Ngày: 2026-08-08

## Bối cảnh
Sprint 5 hiện thực **khám bệnh & hồ sơ bệnh án (EMR)**: bác sĩ ghi nhận buổi khám (triệu chứng, chẩn đoán, chỉ định), **kê đơn thuốc**, và xem **lịch sử khám** của bệnh nhân. Đây là:
- thực thể nối tiếp vòng đời **lịch khám** (`Appointment`, ADR 0005): buổi khám gắn với một lịch đang `InProgress` và khép vòng bằng `Completed`; và
- quan hệ **cha–con** đầu tiên trong hệ thống: một phiếu khám (`Encounter`) chứa nhiều **dòng đơn thuốc** (`PrescriptionItem`).

Cần chốt bốn điểm: (1) quan hệ `Encounter ↔ Appointment` và cách suy ra Patient/Doctor; (2) mô hình cha–con `Encounter → PrescriptionItem`; (3) liên kết `User ↔ Doctor` cho RBAC; (4) cách nối máy trạng thái khi hoàn tất phiếu. Giữ nguyên phong cách Clean Architecture, envelope `ApiResponse`, nền RBAC (Sprint 3) và máy trạng thái đặt ở Domain (Sprint 4).

## Các phương án & quyết định

### 1. Quan hệ Encounter ↔ Appointment + suy ra Patient/Doctor
- **Phương án A — `Encounter` gắn **1–1 bắt buộc** với một `Appointment`, và **lưu snapshot** `PatientId`/`DoctorId` trực tiếp trên `Encounter`.**
- Phương án B — Encounter tham chiếu Appointment tuỳ chọn, luôn **join qua Appointment** để lấy Patient/Doctor.

**Chọn A.** Mỗi buổi khám tương ứng đúng một lịch khám; ràng buộc **1–1** bằng **unique index trên `AppointmentId`**. Lưu `PatientId`/`DoctorId` **snapshot** (sao tại thời điểm tạo phiếu từ Appointment) để:
- truy vấn **lịch sử khám theo bệnh nhân** (`GET /api/encounters?patientId=`) gọn, không phải join chuỗi qua Appointment;
- giữ đúng dữ liệu ngay cả khi lịch bị đổi/ xoá mềm về sau (bệnh án là bản ghi lịch sử, không nên trôi theo Appointment).

Chấp nhận đánh đổi **dư thừa có kiểm soát**: `PatientId`/`DoctorId` được service điền từ Appointment, client **không** gửi (tránh lệch dữ liệu).

**Ràng buộc trạng thái khi tạo:** chỉ tạo phiếu cho Appointment đang `InProgress` (đã check-in và bắt đầu khám). Tạo phiếu khi Appointment sai trạng thái → `Error.Conflict` mã `Encounter.AppointmentNotInProgress` (409). Tạo phiếu thứ hai cho cùng Appointment → `Error.Conflict` mã `Encounter.AlreadyExists` (409) — kiểm ở tầng service (có race như chống trùng lịch/sinh mã, lối thoát là unique index DB).

### 2. Mô hình cha–con Encounter → PrescriptionItem
- **Phương án A — `PrescriptionItem` là **owned entity** (OwnsMany) của aggregate root `Encounter`.**
- Phương án B — `PrescriptionItem` là entity độc lập (kế thừa `Entity`, có `Id`/soft delete riêng, FK `EncounterId`), CRUD từng dòng.

**Chọn A (owned collection).** Dòng đơn thuốc **không có vòng đời độc lập** khỏi phiếu khám: luôn được tạo/sửa/đọc theo cả cụm cùng phiếu. Hệ quả:
- Lưu ở bảng riêng `prescription_items` (khoá ngoại `encounter_id` về `encounters`, khoá chính shadow do EF quản lý). Owned nhưng tách bảng để chuẩn hoá quan hệ 1–nhiều.
- `PrescriptionItem` là POCO **không kế thừa `Entity`** → **không** có `IsDeleted` riêng. Xoá mềm ở mức **phiếu khám**: xoá mềm `Encounter` ẩn luôn cả cụm dòng đơn (đọc luôn qua root nên nhất quán). Không cần cascade vật lý vì không xoá cứng.
- Cập nhật đơn = **thay cả cụm** (`Encounter.ReplaceItems(...)`): service xoá dòng cũ, thêm dòng mới trong một `SaveChanges`. Giữ bất biến ở aggregate root, service/controller không thao tác trực tiếp lên dòng đơn.
- Thuốc nhập **văn bản tự do** (`DrugName`) ở quy mô đồ án; hướng nâng cấp: danh mục thuốc chuẩn hoá (drug master) + FK ở sprint sau.

### 3. Liên kết User ↔ Doctor (RBAC)
Là **nợ kỹ thuật** ghi nhận ở ADR 0005 (chưa lọc "phiếu của tôi" cho bác sĩ).

- **Phương án A — hoãn liên kết; RBAC chỉ theo vai trò.**
- Phương án B — thêm `Doctor.UserId?` + migration + cập nhật seed để RBAC "bác sĩ chỉ thao tác phiếu của mình".

**Chọn A (hoãn).** Ở sprint này **bác sĩ thao tác được mọi phiếu khám** (RBAC chỉ theo vai trò), tránh chi phí migration + đồng bộ seed đăng nhập. Ghi rõ là **hạn chế**: chưa ràng buộc "phiếu của chính bác sĩ đăng nhập". Để mở cho sprint có liên kết `User ↔ Doctor`.

**Phân quyền:**
- **Ghi** (tạo/sửa/hoàn tất phiếu + đơn thuốc): **Bác sĩ và Admin** (`Roles.RecordEncounter = Admin,Doctor`). Khác các slice danh mục (ghi = Admin/Lễ tân) vì ghi bệnh án là hành vi **lâm sàng**.
- **Đọc** (danh sách/chi tiết/lịch sử): mọi vai trò đã đăng nhập (Lễ tân cần xem để tra cứu).

### 4. Nối máy trạng thái khi hoàn tất phiếu
`Encounter` có trạng thái riêng **`EncounterStatus`** (lưu chuỗi, đồng nhất các enum khác):

```
Draft      (khởi tạo — đang khám, có thể lưu nhiều lần)
Completed  (đã chốt phiếu)
```

- Tạo phiếu → `Draft`. Sửa nội dung/đơn thuốc chỉ khi còn `Draft`.
- **Hoàn tất phiếu** (`POST /api/encounters/{id}/complete`): `Encounter.Complete()` (`Draft → Completed`) **và** service gọi `Appointment.Complete()` (`InProgress → Completed`, method Domain Sprint 4) trong cùng `SaveChanges`. **Không** sửa trạng thái Appointment trực tiếp — giữ nguồn sự thật ở Domain.
- Nếu `Appointment.Complete()` thất bại (không ở `InProgress`) → trả lỗi, **không** lưu (cả hai chuyển tiếp cùng thành/bại). Hoàn tất phiếu đã `Completed` → `Error.Conflict` mã `Encounter.InvalidTransition` (409).

## Hệ quả
- **Ưu:** bệnh án là bản ghi lịch sử ổn định (snapshot Patient/Doctor); lịch sử theo bệnh nhân truy vấn gọn; cha–con đơn giản qua owned collection, xoá mềm nhất quán ở mức phiếu; khép vòng đời lịch khám tự động, dùng lại máy trạng thái Domain; RBAC lâm sàng tách khỏi RBAC danh mục.
- **Nhược/đánh đổi:**
  - Dư thừa `PatientId`/`DoctorId` trên `Encounter` — chấp nhận, service là nơi duy nhất điền để tránh lệch.
  - Ràng buộc 1–1 và "chưa có phiếu" kiểm ở service có **race** (như ADR 0005) — lối thoát là **unique index** `AppointmentId` (đã thêm) chặn ở DB.
  - Bác sĩ thao tác **mọi** phiếu (chưa `User ↔ Doctor`) — hạn chế đã biết, mở ở sprint sau.
  - Sửa đơn = thay cả cụm — đơn giản, chấp nhận ghi đè toàn bộ dòng mỗi lần lưu.
