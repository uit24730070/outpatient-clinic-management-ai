# Bảng `medication_batches` — Từ điển dữ liệu

Lô thuốc: đơn vị tồn kho theo **lô + hạn dùng** của một thuốc. Entity `ClinicManagement.Domain.Pharmacy.MedicationBatch`, migration `AddPharmacyInventory`. Xem [ADR 0011](../adr/0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md).

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng. |
| `MedicationId` | `uuid` | Không | **FK** → `medications.Id`, `ON DELETE RESTRICT`. |
| `BatchNumber` | `varchar(100)` | Không | Số lô của nhà sản xuất. |
| `ExpiryDate` | `date` | Không | Hạn dùng (`DateOnly`, chỉ ngày). |
| `QuantityOnHand` | `integer` | Không | Tồn hiện tại của lô (`≥ 0`). |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật (mỗi lần cộng tồn). |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (mặc định `false`). |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

### Index & khoá ngoại
- `PK_medication_batches` — khóa chính trên `Id`.
- `IX_medication_batches_MedicationId_ExpiryDate` — truy hồi lô theo thuốc, **hạn tăng dần** (định hướng FEFO ở P2).
- `FK_medication_batches_medications_MedicationId` — `ON DELETE RESTRICT`.

## Quy tắc nghiệp vụ
- **Không CRUD trực tiếp ở P1.** Lô **chỉ sinh/tăng qua phiếu nhập** ([`stock_receipts`](stock_receipts.md)): dòng nhập tìm lô khớp (`MedicationId`+`BatchNumber`+`ExpiryDate`) → cộng `QuantityOnHand` (method Domain `Increase`, kiểm `> 0`), hoặc tạo lô mới.
- Đọc: `GET /api/medications/{id}/batches` (hạn gần nhất trước) và tồn tổng nhúng trong `MedicationDto.StockOnHand`.
- Nền cho **cấp phát FEFO** (P2): trừ `QuantityOnHand` theo `ExpiryDate` tăng dần; dự kiến thêm concurrency token khi trừ tồn (xem [ADR 0011](../adr/0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md)).
