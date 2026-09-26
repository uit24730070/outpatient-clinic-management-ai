# Bảng `encounters` & `prescription_items` — Từ điển dữ liệu

Phiếu khám (bệnh án của một buổi khám) và các dòng đơn thuốc. Sinh ra từ entity `ClinicManagement.Domain.Encounters.Encounter` (aggregate root) và owned entity `PrescriptionItem`, migration `AddEncounters`. Quan hệ **1–1** với `appointments` và **cha–con** `encounters → prescription_items` (xem [ADR 0006](../adr/0006-mo-hinh-benh-an-encounter-prescription.md)).

## Bảng `encounters`

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `AppointmentId` | `uuid` | Không | **FK** → `appointments.Id`. **Unique** (1–1: mỗi lịch một phiếu). |
| `PatientId` | `uuid` | Không | **FK** → `patients.Id`. **Snapshot** từ lịch khám lúc tạo. Có index. |
| `DoctorId` | `uuid` | Không | **FK** → `doctors.Id`. **Snapshot** từ lịch khám lúc tạo. |
| `Symptoms` | `varchar(1000)` | Có | Triệu chứng. |
| `Diagnosis` | `varchar(1000)` | Không | Chẩn đoán (bắt buộc). |
| `Notes` | `varchar(1000)` | Có | Chỉ định / ghi chú thêm. |
| `Status` | `varchar(20)` | Không | Trạng thái phiếu, lưu **chuỗi** (`Draft`/`Completed`). |
| `DispensedAt` | `timestamptz` | Có | Thời điểm đã cấp phát thuốc (trừ tồn FEFO). `null` = chưa cấp phát. Chống cấp phát trùng (P2 — ADR 0011). |
| `MedicationInvoicedAt` | `timestamptz` | Có | Thời điểm đã lập **hoá đơn thuốc** từ phiếu. `null` = chưa lập. Cờ chống lập HĐ thuốc trùng (Mô hình A — ADR 0014 P2, migration `RelaxInvoiceModelAndMedicationInvoiced`). |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo (dùng làm mốc "thời điểm khám" cho lịch sử). |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (mặc định `false`). |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

### Index & khoá ngoại
- `PK_encounters` — khóa chính trên `Id`.
- `IX_encounters_AppointmentId` — **UNIQUE**, đảm bảo quan hệ 1–1 và chặn tạo phiếu thứ hai cho cùng lịch (lối thoát chặt cho race ở tầng service).
- `IX_encounters_PatientId` — tra cứu lịch sử khám theo bệnh nhân.
- `FK_encounters_appointments_AppointmentId`, `FK_encounters_patients_PatientId`, `FK_encounters_doctors_DoctorId` — `ON DELETE RESTRICT`.

## Bảng `prescription_items` (owned)

Dòng đơn thuốc thuộc phiếu khám — **owned collection**, không có vòng đời độc lập; tạo/sửa/đọc theo cả cụm cùng phiếu.

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `integer` (identity) | Không | Khóa chính shadow do EF quản lý. |
| `EncounterId` | `uuid` | Không | **FK** → `encounters.Id`, `ON DELETE CASCADE` (xoá cứng theo phiếu; hệ thống chỉ xoá mềm phiếu nên thực tế không kích hoạt). Có index. |
| `MedicationId` | `uuid` | Có | **Tuỳ chọn** trỏ tới [`medications.Id`](medications.md) — **không FK cứng**. Có giá trị ⇒ cấp phát trừ tồn FEFO khi chốt phiếu; `null` ⇒ thuốc ngoài danh mục (không trừ tồn). Có index (P2 — ADR 0011). |
| `DrugName` | `varchar(200)` | Không | Tên thuốc (văn bản hiển thị; điền từ danh mục khi có `MedicationId`, vẫn sửa tay được). |
| `Dosage` | `varchar(100)` | Không | Liều dùng, ví dụ `500mg`. |
| `Quantity` | `integer` | Không | Số lượng, `> 0`. |
| `Instruction` | `varchar(300)` | Có | Cách dùng, ví dụ `Ngày 2 lần sau ăn`. |

## Quy tắc nghiệp vụ
- **Tạo phiếu** chỉ khi lịch khám đang `InProgress`; sai trạng thái → `Encounter.AppointmentNotInProgress` (409); lịch không tồn tại → `Encounter.AppointmentNotFound` (400).
- **1–1:** tạo phiếu thứ hai cho cùng lịch → `Encounter.AlreadyExists` (409) (kiểm ở service; unique index chặn ở DB).
- `PatientId`/`DoctorId` là **snapshot** do service điền từ lịch khám — client **không** gửi; bệnh án giữ nguyên dù lịch đổi/xoá về sau.
- **Sửa nội dung + thay cả cụm đơn thuốc** chỉ khi phiếu còn `Draft`; sau `Completed` → `Encounter.InvalidTransition` (409).
- **Chốt phiếu** (`POST /{id}/complete`): `Draft → Completed` **và** khép lịch khám `InProgress → Completed` (method Domain, cùng `SaveChanges`) — xem [ADR 0006](../adr/0006-mo-hinh-benh-an-encounter-prescription.md).
- **Cấp phát thuốc (P2 — ADR 0011):** chốt phiếu **tự động cấp phát** các dòng đơn có `MedicationId` — trừ tồn các lô **còn hạn** theo **FEFO** (hạn tăng dần), ghi [`stock_transactions`](stock_transactions.md) loại `Dispense` (âm, `ReferenceType="Encounter"`), đặt `DispensedAt`. Thiếu tồn còn hạn → `Pharmacy.InsufficientStock` (409, **rollback toàn bộ** — không chốt được phiếu). Tranh chấp lô đồng thời → `Pharmacy.ConcurrencyConflict` (409, concurrency token `xmin`).
- **RBAC:** tạo/sửa = **Bác sĩ và Admin** (`Roles.RecordEncounter`); **chốt phiếu (kèm cấp phát)** = **Admin/Bác sĩ/Dược sĩ** (`Roles.DispenseEncounter`, ADR 0013 — Lễ tân không đụng tồn kho); đọc (danh sách/chi tiết/lịch sử) = mọi vai trò đã đăng nhập.
- DTO trả về kèm `patientName`, `doctorName` (join) và mảng `prescriptionItems`.
- Chiến lược xoá mềm: xem [ADR 0003](../adr/0003-chien-luoc-soft-delete.md).
