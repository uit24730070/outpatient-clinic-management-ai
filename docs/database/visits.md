# Bảng `visits` — Từ điển dữ liệu

Lượt tiếp nhận (lượt khám): gom **một lần bệnh nhân đến phòng khám**, cho phép đăng ký **nhiều dịch vụ khám** trong một lượt (mỗi dịch vụ là một `appointments` con tham chiếu `VisitId`). Sinh từ entity `ClinicManagement.Domain.Visits.Visit`, migration `AddVisits` (xem [ADR 0017](../adr/0017-mo-hinh-luot-tiep-don-visit.md)).

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `Code` | `varchar(20)` | Không | Mã lượt duy nhất, ví dụ `LK-000001`. Sinh tuần tự (đếm `IgnoreQueryFilters`). |
| `PatientId` | `uuid` | Không | **FK** → `patients.Id` (`ON DELETE RESTRICT`). Snapshot bệnh nhân của lượt. Có index. |
| `Note` | `varchar(500)` | Có | Ghi chú tiếp nhận. |
| `Status` | `varchar(20)` | Không | Trạng thái, lưu **chuỗi** (`Open`/`Closed`/`Cancelled`). |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (mặc định `false`). Khác `Cancelled` (trạng thái nghiệp vụ). |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

## Index & khoá ngoại
- `PK_visits` — khóa chính trên `Id`.
- `IX_visits_Code` — **unique** trên `Code`.
- `IX_visits_PatientId` — tra cứu theo bệnh nhân.
- `FK_visits_patients_PatientId` — `ON DELETE RESTRICT`.

## Quy tắc nghiệp vụ
- **Tạo lượt** (`POST /api/visits`): nhận `patientId` + `services[]` (dịch vụ khám: `doctorId`, `startTime`, `endTime`, `reason?`, `servicePriceId?`) + `paraclinicalServiceIds[]?` (dịch vụ CLS). Tạo `Visit` + N `Appointment` (khám) **+ một `LabOrder` walk-in** (nếu có CLS, loại `Paraclinical`, gắn `VisitId`) trong **một `SaveChanges`**. Kiểm bệnh nhân/từng bác sĩ tồn tại, dịch vụ khám loại `Consultation` (sai → `Appointment.ServiceNotConsultation` 400), CLS loại `Paraclinical` (sai → `Paraclinical.ServiceNotParaclinical` 400), chống trùng giờ mỗi bác sĩ (→ `Appointment.Overlap` 409). Không có cả khám lẫn CLS → `Visit.NoServices` 400 (cho phép lượt **chỉ CLS**).
- **Thêm dịch vụ** (`POST /api/visits/{id}/services`): chỉ khi lượt còn `Open` (sai → `Visit.NotOpen` 409).
- **Máy trạng thái** (Domain): `Open → Closed` (`/close`) / `Open → Cancelled` (`/cancel`); chuyển sai → `Visit.InvalidTransition` 409.
- **Gom viện phí theo lượt:** hoá đơn mang `invoices.VisitId` **suy từ lịch khám gắn HĐ** lúc lập. `GET /api/visits/{id}` trả tổng **đã lập/đã thu/còn nợ** (gom theo `VisitId`, union `AppointmentId` để tương thích HĐ cũ; bỏ HĐ `Cancelled`). Xem thêm `GET /api/invoices/by-visit/{id}` và **thu cả lượt** `POST /api/invoices/pay-visit/{id}` (RBAC `ManageBilling`).
- RBAC: ghi = `Roles.ManageStaff` (Admin/Lễ tân); đọc mọi vai trò.
- Chiến lược xoá mềm: xem [ADR 0003](../adr/0003-chien-luoc-soft-delete.md).
