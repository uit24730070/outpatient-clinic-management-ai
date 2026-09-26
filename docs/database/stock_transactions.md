# Bảng `stock_transactions` — Từ điển dữ liệu

Sổ cái giao dịch tồn: bản ghi **bất biến** truy vết mọi thay đổi tồn của một lô. Entity `ClinicManagement.Domain.Pharmacy.StockTransaction`, migration `AddPharmacyInventory`. Xem [ADR 0011](../adr/0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md).

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. |
| `MedicationBatchId` | `uuid` | Không | **FK** → `medication_batches.Id`, `ON DELETE RESTRICT`. |
| `Type` | `varchar(20)` | Không | Loại giao dịch, lưu **chuỗi**: `Import` (nhập, P1), `Dispense` (cấp phát, P2); `Adjust` (điều chỉnh — dành sau). |
| `QuantityDelta` | `integer` | Không | Chênh lệch tồn: **dương = nhập** (`Import`), **âm = xuất** (`Dispense`). |
| `ReferenceType` | `varchar(50)` | Không | Loại chứng từ nguồn: `StockReceipt` (nhập) hoặc `Encounter` (cấp phát theo phiếu khám). |
| `ReferenceId` | `uuid` | Không | Id chứng từ nguồn (phiếu nhập hoặc phiếu khám). |
| `OccurredAt` | `timestamptz` | Không | Thời điểm phát sinh giao dịch. |
| `CreatedAt` / `UpdatedAt` | `timestamptz` | — | Dấu thời gian kiểm toán. |
| `IsDeleted` / `DeletedAt` | — | — | Xoá mềm (không dùng — sổ cái bất biến). |

### Index & khoá ngoại
- `PK_stock_transactions`.
- `IX_stock_transactions_MedicationBatchId_OccurredAt` — sổ cái theo lô, thứ tự thời gian.
- `IX_stock_transactions_ReferenceType_ReferenceId` — tra theo chứng từ nguồn.
- `FK_stock_transactions_medication_batches_MedicationBatchId` — `ON DELETE RESTRICT`.

## Quy tắc nghiệp vụ
- **Nhập** ([`stock_receipts`](stock_receipts.md)) — mỗi dòng nhập tạo một giao dịch `Import` (dương).
- **Cấp phát** ([`encounters`](encounters.md)) — chốt phiếu khám trừ tồn FEFO, mỗi lô đụng tới ghi một giao dịch `Dispense` (âm), `ReferenceType="Encounter"`, cùng `SaveChanges` với việc trừ tồn lô (P2 — [ADR 0011](../adr/0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md)).
- Đọc: `GET /api/stock-transactions?medicationId=&type=` (**Admin + Dược sĩ**, ADR 0013) — phục vụ kiểm tra & báo cáo xuất–nhập–tồn.
- Sổ cái **bất biến**, ghi qua EF trong luồng service (không SQL thô).
