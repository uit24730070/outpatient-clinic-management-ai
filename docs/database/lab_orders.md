# Bảng `lab_orders` — Từ điển dữ liệu

Phiếu chỉ định cận lâm sàng. **Hai nguồn** (ADR 0016): bác sĩ chỉ định trong lúc khám (gắn phiếu khám +
bác sĩ), hoặc **walk-in** do lễ tân đăng ký (không cần phiếu khám/bác sĩ). Entity
`ClinicManagement.Domain.Paraclinical.LabOrder` (aggregate root, owned collection `LabOrderItem`),
migration `AddParaclinical` + `RelaxLabOrderAndAppointmentService`. Xem
[ADR 0015](../adr/0015-mo-hinh-can-lam-sang.md), [ADR 0016](../adr/0016-dang-ky-dich-vu-walkin-cls-va-ky-thuat-vien.md).

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng. |
| `Code` | `varchar(20)` | Không | Mã phiếu `CLS-000001`, sinh tự động. **Unique**. |
| `EncounterId` | `uuid` | **Có** | FK → `encounters.Id` (Restrict). Phiếu khám nguồn; **null với walk-in** (ADR 0016). Có index. |
| `AppointmentId` | `uuid` | Có | FK → `appointments.Id` (Restrict). Lịch khám gắn kèm (walk-in). Có index. |
| `VisitId` | `uuid` | Có | FK → `visits.Id` (Restrict). Lượt tiếp nhận gắn kèm (walk-in, ADR 0017) — gom phiếu CLS & hoá đơn phí CLS theo lượt. Có index. |
| `PatientId` | `uuid` | Không | FK → `patients.Id` (Restrict). **Snapshot** từ phiếu khám hoặc do lễ tân chọn. Có index. |
| `DoctorId` | `uuid` | **Có** | FK → `doctors.Id` (Restrict). **Snapshot** bác sĩ chỉ định; **null với walk-in**. |
| `Note` | `varchar(1000)` | Có | Ghi chú chỉ định. |
| `Status` | `varchar(20)` | Không | `Ordered`/`InProgress`/`Completed`/`Cancelled` (chuỗi). |
| `InvoicedAt` | `timestamptz` | Có | Thời điểm đã lập HĐ phí CLS — cờ chống lập trùng (Mô hình A). |
| `CreatedAt` / `UpdatedAt` | `timestamptz` | — | Dấu thời gian kiểm toán. |
| `IsDeleted` / `DeletedAt` | — | — | Xoá mềm ([ADR 0003](../adr/0003-chien-luoc-soft-delete.md)). |

### Index & khoá
- `PK_lab_orders`; `IX_lab_orders_Code` — **UNIQUE**; `IX_lab_orders_EncounterId`; `IX_lab_orders_AppointmentId`; `IX_lab_orders_VisitId`; `IX_lab_orders_PatientId`; FK `DoctorId`/`AppointmentId`/`VisitId`.

## Quy tắc nghiệp vụ
- Mã `CLS-` đếm cả bản ghi đã xoá mềm (`IgnoreQueryFilters()`) để tránh trùng mã.
- **Đường bác sĩ** (`POST /api/lab-orders`, `RecordEncounter`): chỉ khi phiếu khám còn `Draft` → sai → 409
  `Paraclinical.EncounterNotDraft`. **Đường walk-in** (`POST /api/lab-orders/walk-in`, `ManageStaff`): chỉ cần
  bệnh nhân (`Patient.NotFound` 404) + lượt tiếp nhận nếu có (`Appointment.NotFound` 404), không encounter/bác sĩ.
  Cả hai: mỗi mục tham chiếu một `ServicePrice` loại **Paraclinical** (sai loại → 400 `Paraclinical.ServiceNotParaclinical`).
- **Vòng đời** (máy trạng thái ở Domain): nhập kết quả mục đầu → `InProgress`; đủ mọi mục có kết quả →
  `Completed`. Sửa/nhập/huỷ sai vòng đời → 409 `Paraclinical.InvalidTransition`.
- **`TotalAmount`** (Σ đơn giá các mục) là thuộc tính **tính toán**, không map cột (`builder.Ignore`).
- **Phí CLS** lập hoá đơn riêng loại `Paraclinical` qua `POST /api/invoices/from-lab-order/{id}`; idempotent
  qua cờ `InvoicedAt` (lập lần 2 → 409 `Billing.ParaclinicalAlreadyInvoiced`). **Lượt gắn hoá đơn** (`Invoice.VisitId`):
  ưu tiên `LabOrder.VisitId` (walk-in gắn lượt, ADR 0017), fallback suy từ `AppointmentId`/`Encounter.AppointmentId`.
  **Walk-in gắn lượt:** `POST /api/lab-orders/walk-in` nhận `visitId?` (kiểm tồn tại → `Visit.NotFound` 404) để gom phiếu CLS + hoá đơn phí CLS vào lượt.
- **RBAC:** chỉ định (đường bác sĩ) = `RecordEncounter`; đăng ký walk-in = `ManageStaff` (Lễ tân);
  **nhập kết quả/huỷ = `RecordLabResult`** (Admin + Bác sĩ + **Kỹ thuật viên**, ADR 0016); đọc mở mọi vai trò.
