# Bảng `service_prices` — Từ điển dữ liệu

Bảng giá dịch vụ (công khám, thủ thuật, tư vấn…). Entity `ClinicManagement.Domain.Billing.ServicePrice` (vertical slice CRUD như `Medication`/`Specialty`), migration `AddBillingAndServicePrices`. Xem [ADR 0014](../adr/0014-mo-hinh-vien-phi-va-thu-ngan.md).

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng. |
| `Code` | `varchar(20)` | Không | Mã dịch vụ `DV-000001`, sinh tự động. **Unique**. |
| `Name` | `varchar(200)` | Không | Tên dịch vụ. Có index. |
| `UnitPrice` | `numeric(18,2)` | Không | Đơn giá (VND). `≥ 0`. |
| `Description` | `varchar(1000)` | Có | Mô tả thêm. |
| `Category` | `varchar(20)` | Không | Phân loại (chuỗi) `Consultation`/`Paraclinical`/`Other`. Mặc định `Other` cho dữ liệu cũ (Sprint 15, [ADR 0015](../adr/0015-mo-hinh-can-lam-sang.md)). |
| `CreatedAt` / `UpdatedAt` | `timestamptz` | — | Dấu thời gian kiểm toán. |
| `IsDeleted` / `DeletedAt` | — | — | Xoá mềm ([ADR 0003](../adr/0003-chien-luoc-soft-delete.md)). |

### Index & khoá
- `PK_service_prices`; `IX_service_prices_Code` — **UNIQUE**; `IX_service_prices_Name` — tìm kiếm.

## Quy tắc nghiệp vụ
- Mã `DV-` đếm cả bản ghi đã xoá mềm (`IgnoreQueryFilters()`) để tránh trùng mã.
- Tìm kiếm theo tên/mã (`ToLower().Contains()`), phân trang clamp.
- Đơn giá ở đây là giá "sống"; khi lập hoá đơn, `invoice_items.UnitPrice` **snapshot** giá tại thời điểm lập — đổi giá sau không ảnh hưởng hoá đơn cũ.
- **RBAC:** ghi/đọc = `Roles.ManageBilling` (**Admin + Lễ tân**, ADR 0014). Bác sĩ/Dược sĩ không truy cập.
- **Phân loại `Category`** (Sprint 15): lọc danh mục `?category=`; dịch vụ loại `Paraclinical` mới **chỉ định được** trong phiếu cận lâm sàng ([ADR 0015](../adr/0015-mo-hinh-can-lam-sang.md)). Khi lập hoá đơn dịch vụ, loại dòng suy từ `Category` (Paraclinical→`Paraclinical`, còn lại→`ServiceFee`).
- Seed sẵn 6 dịch vụ: công khám `DV-000001` Khám tổng quát 150.000₫, `DV-000002` Tái khám 100.000₫, `DV-000003` Khám chuyên khoa 200.000₫ (đều `Consultation`); cận lâm sàng `DV-CLS001` Xét nghiệm công thức máu 80.000₫, `DV-CLS002` Chụp X-quang ngực thẳng 120.000₫, `DV-CLS003` Siêu âm ổ bụng tổng quát 150.000₫ (đều `Paraclinical`). *(Cấu hình `Billing:DefaultConsultationServiceCode` không còn dùng từ Sprint 14.5 — dead config.)*
