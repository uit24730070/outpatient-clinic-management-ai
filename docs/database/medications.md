# Bảng `medications` — Từ điển dữ liệu

Danh mục thuốc. Sinh ra từ entity `ClinicManagement.Domain.Pharmacy.Medication` (aggregate root của các lô), migration `AddPharmacyInventory`. Xem [ADR 0011](../adr/0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md).

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `Code` | `varchar(20)` | Không | Mã thuốc `TH-000001`, sinh tự động. **Unique**. |
| `Name` | `varchar(200)` | Không | Tên thương mại. Có index. |
| `ActiveIngredient` | `varchar(200)` | Không | Hoạt chất. |
| `Unit` | `varchar(50)` | Không | Đơn vị tính (viên/vỉ/chai/ống…), **văn bản tự do**. |
| `ReorderLevel` | `integer` | Không | Ngưỡng tồn tối thiểu để cảnh báo (dùng ở P2). Mặc định 0, `≥ 0`. |
| `SalePrice` | `numeric(18,2)` | Không | Giá bán một đơn vị (VND) để tính tiền thuốc trên hoá đơn (BILL-02, [ADR 0014](../adr/0014-mo-hinh-vien-phi-va-thu-ngan.md)). Mặc định 0, `≥ 0`. Không đụng tồn/FEFO. |
| `Description` | `varchar(1000)` | Có | Mô tả thêm. |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (mặc định `false`). |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

### Index & khoá
- `PK_medications` — khóa chính trên `Id`.
- `IX_medications_Code` — **UNIQUE**.
- `IX_medications_Name` — tìm kiếm theo tên.

## Quy tắc nghiệp vụ
- **Tồn tổng không lưu ở đây.** DTO trả về `StockOnHand` = `SUM(QuantityOnHand)` các lô chưa xoá của thuốc (subquery, tính phía server). Xem [`medication_batches`](medication_batches.md).
- Mã `TH-` đếm cả bản ghi đã xoá mềm (`IgnoreQueryFilters()`) để tránh trùng mã.
- Tìm kiếm theo tên/mã/hoạt chất (`ToLower().Contains()`), phân trang.
- **RBAC:** đọc = **mọi vai trò** (bác sĩ tra khi kê đơn); ghi (tạo/sửa/xoá) = `Roles.ManagePharmacy` (**Admin + Dược sĩ**, ADR 0013).
- Seed sẵn 3 thuốc mẫu (`TH-000001..3`) để demo (migration `AddBillingAndServicePrices` đặt `SalePrice` = 2.000/3.000/5.000₫). Chiến lược xoá mềm: [ADR 0003](../adr/0003-chien-luoc-soft-delete.md).
