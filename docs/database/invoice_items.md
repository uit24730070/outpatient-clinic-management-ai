# Bảng `invoice_items` (owned) — Từ điển dữ liệu

Dòng chi tiết của một hoá đơn. Owned entity `ClinicManagement.Domain.Billing.InvoiceItem` thuộc aggregate `Invoice` ([invoices.md](invoices.md)), migration `AddBillingAndServicePrices`. Vòng đời gắn chặt hoá đơn (như `prescription_items`, [ADR 0006](../adr/0006-mo-hinh-benh-an-encounter-prescription.md)); mô hình: [ADR 0014](../adr/0014-mo-hinh-vien-phi-va-thu-ngan.md).

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `integer` (identity) | Không | Khóa chính shadow do EF quản lý. |
| `InvoiceId` | `uuid` | Không | **FK** → `invoices.Id`, `ON DELETE CASCADE`. Có index. |
| `ItemType` | `varchar(20)` | Không | Enum chuỗi: `ServiceFee` (công khám/dịch vụ) · `Medication` (tiền thuốc) · `Other` (khoản khác). |
| `Description` | `varchar(300)` | Không | Nội dung dòng (tên dịch vụ/thuốc lúc lập). |
| `UnitPrice` | `numeric(18,2)` | Không | Đơn giá **snapshot** tại thời điểm lập/sửa hoá đơn (không join giá sống). |
| `Quantity` | `integer` | Không | Số lượng (`> 0`). |
| `LineTotal` | `numeric(18,2)` | Không | Thành tiền = `UnitPrice × Quantity`. |
| `ReferenceId` | `uuid` | Có | Trỏ `service_prices.Id` (dòng `ServiceFee`) hoặc `medications.Id` (dòng `Medication`) để tra soát. |

### Index & khoá
- `PK_invoice_items` trên `Id`; `IX_invoice_items_InvoiceId` — theo hoá đơn cha.

## Quy tắc nghiệp vụ
- **Không** có soft delete riêng — ẩn theo hoá đơn (sửa = thay cả cụm `ReplaceItems`, chỉ khi `Draft`).
- `TotalAmount` của hoá đơn = Σ `LineTotal`, tính lại phía server mỗi khi `ReplaceItems`/`Recalculate`.
- `ReferenceId` chỉ để tra soát/báo cáo — **không** FK cứng (giá đã snapshot, không phụ thuộc dữ liệu nguồn còn tồn tại hay không).
- `TestDbContext` phải tự cấu hình `OwnsMany(InvoiceItem)` trong unit test (Infrastructure không nạp).
