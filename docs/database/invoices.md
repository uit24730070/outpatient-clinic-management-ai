# Bảng `invoices` & `invoice_items` — Từ điển dữ liệu

Hoá đơn viện phí (aggregate cha–con). Entity `ClinicManagement.Domain.Billing.Invoice` (aggregate root) + owned entity `InvoiceItem`, migration `AddBillingAndServicePrices`. Cùng khuôn owned collection như `prescription_items` ([ADR 0006](../adr/0006-mo-hinh-benh-an-encounter-prescription.md))/`stock_receipt_items` ([ADR 0011](../adr/0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md)); mô hình: [ADR 0014](../adr/0014-mo-hinh-vien-phi-va-thu-ngan.md). Chi tiết dòng: [invoice_items.md](invoice_items.md).

## Bảng `invoices`

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. |
| `Code` | `varchar(20)` | Không | Mã hoá đơn `HD-000001`, sinh tự động. **Unique**. |
| `PatientId` | `uuid` | Không | Snapshot bệnh nhân. Có index. |
| `EncounterId` | `uuid` | Có | Phiếu khám nguồn (nếu lập HĐ thuốc từ phiếu). **Không còn unique** từ Sprint 14.5 (Mô hình A — nhiều HĐ/lượt). Index thường. |
| `AppointmentId` | `uuid` | Có | Lịch khám nguồn (Sprint 14.5) — gom hoá đơn theo lịch. Null với vãng lai/chỉ-CLS. Index thường. |
| `VisitId` | `uuid` | Có | Lượt tiếp nhận nguồn (Sprint 17, ADR 0017) — **suy từ lịch khám gắn HĐ** lúc lập; gom HĐ cả lượt + thu cả lượt. Null nếu không thuộc lượt. Index thường (không FK cứng). |
| `Status` | `varchar(20)` | Không | Enum chuỗi: `Draft`/`Paid`/`Cancelled`. |
| `TotalAmount` | `numeric(18,2)` | Không | Tổng tiền = Σ `invoice_items.LineTotal`, tính phía server. |
| `PaidAt` | `timestamptz` | Có | Thời điểm thu tiền (đặt khi `Paid`). |
| `PaymentMethod` | `varchar(20)` | Có | Enum chuỗi: `Cash`/`Card`/`Transfer` (đặt khi `Paid`). |
| `Note` | `varchar(1000)` | Có | Ghi chú. |
| `CreatedAt` / `UpdatedAt` | `timestamptz` | — | Dấu thời gian kiểm toán. |
| `IsDeleted` / `DeletedAt` | — | — | Xoá mềm (chỉ khi `Draft`). |

### Index & khoá
- `PK_invoices`; `IX_invoices_Code` — **UNIQUE**; `IX_invoices_EncounterId` (thường, từ S14.5 bỏ unique); `IX_invoices_AppointmentId`; `IX_invoices_VisitId`; `IX_invoices_PatientId`.

## Quy tắc nghiệp vụ
- **Máy trạng thái** ([ADR 0014](../adr/0014-mo-hinh-vien-phi-va-thu-ngan.md)): `Draft` → `Paid` / `Cancelled`. Sửa dòng (`ReplaceItems`), thu tiền (`Pay`), huỷ, xoá mềm **chỉ khi `Draft`**; hoá đơn `Paid` **bất biến**. Chuyển sai → `Billing.InvalidTransition` (409).
- **Lập HĐ thuốc từ phiếu khám** (`POST /api/invoices/from-encounter/{encounterId}`, **Mô hình A** từ S14.5): phiếu phải `Completed` (khác → `Billing.EncounterNotCompleted` 409); dựng **chỉ dòng thuốc đã cấp** (từ `prescription_items` có `MedicationId`, `Quantity × SalePrice`, snapshot) — **không** còn tự thêm công khám. Không có dòng thuốc gắn danh mục → `Billing.NoMedicationToInvoice` (400). Chống trùng bằng cờ `encounters.MedicationInvoicedAt`: lập lần 2 → `Billing.MedicationAlreadyInvoiced` (409). HĐ gắn `AppointmentId` của phiếu (nếu có).
- **Hoá đơn dịch vụ / lúc tiếp nhận** (`POST /api/invoices`): dòng tham chiếu `ServicePriceId` (snapshot giá); nhận `appointmentId?` để gắn lượt (kiểm tồn tại → `Appointment.NotFound` 404). Dùng cho khám/tái khám/**chỉ-CLS** (không cần encounter).
- **Gom theo lượt** (`GET /api/invoices/by-appointment/{appointmentId}`): trả danh sách + `TotalBilled`/`TotalPaid`/`TotalOutstanding` (server; HĐ `Cancelled` không tính billed). Cũng lọc `GET /api/invoices?appointmentId=`.
- Mã `HD-` đếm cả bản ghi đã xoá mềm (`IgnoreQueryFilters()`).
- **RBAC:** ghi/đọc = `Roles.ManageBilling` (**Admin + Lễ tân**, ADR 0014). Bác sĩ/Dược sĩ không thấy hoá đơn.
