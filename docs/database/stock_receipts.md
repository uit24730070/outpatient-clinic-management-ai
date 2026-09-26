# Bảng `stock_receipts` & `stock_receipt_items` — Từ điển dữ liệu

Phiếu nhập kho (aggregate cha–con) làm tăng tồn theo lô. Entity `ClinicManagement.Domain.Pharmacy.StockReceipt` (aggregate root) + owned entity `StockReceiptItem`, migration `AddPharmacyInventory`. Cùng khuôn owned collection như `prescription_items` ([ADR 0006](../adr/0006-mo-hinh-benh-an-encounter-prescription.md)); xem [ADR 0011](../adr/0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md).

## Bảng `stock_receipts`

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. |
| `Code` | `varchar(20)` | Không | Mã phiếu `PN-000001`, sinh tự động. **Unique**. |
| `SupplierName` | `varchar(200)` | Không | Nhà cung cấp (văn bản tự do ở P1). |
| `ReceivedAt` | `timestamptz` | Không | Thời điểm nhận hàng. |
| `Note` | `varchar(1000)` | Có | Ghi chú. |
| `CreatedAt` / `UpdatedAt` | `timestamptz` | — | Dấu thời gian kiểm toán. |
| `IsDeleted` / `DeletedAt` | — | — | Xoá mềm (P1 không dùng — phiếu bất biến). |

- `PK_stock_receipts`; `IX_stock_receipts_Code` — **UNIQUE**.

## Bảng `stock_receipt_items` (owned)

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `integer` (identity) | Không | Khóa chính shadow do EF quản lý. |
| `StockReceiptId` | `uuid` | Không | **FK** → `stock_receipts.Id`, `ON DELETE CASCADE`. Có index. |
| `MedicationId` | `uuid` | Không | Thuốc được nhập. Có index. |
| `BatchNumber` | `varchar(100)` | Không | Số lô. |
| `ExpiryDate` | `date` | Không | Hạn dùng (`DateOnly`). |
| `Quantity` | `integer` | Không | Số lượng nhập (`> 0`). |
| `UnitCost` | `numeric(18,2)` | Có | Đơn giá nhập (tuỳ chọn). |

## Quy tắc nghiệp vụ
- **Tạo phiếu nhập** (`POST /api/stock-receipts`) trong **một `SaveChanges`**: với mỗi dòng → tìm/tạo lô ([`medication_batches`](medication_batches.md)) + cộng tồn + ghi một [`stock_transactions`](stock_transactions.md) loại `Import`. Dòng trùng khoá lô trong cùng phiếu **cộng dồn**.
- Thuốc không tồn tại/đã xoá → `Pharmacy.MedicationNotFound` (404); validator: ≥1 dòng, `Quantity > 0`.
- Phiếu **bất biến** ở P1: chỉ `GET` list/chi tiết, không sửa/xoá (tránh hoàn tác tồn).
- **RBAC:** ghi = `Roles.ManagePharmacy` (**Admin + Dược sĩ**, ADR 0013); đọc cùng nhóm.
