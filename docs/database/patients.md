# Bảng `patients` — Từ điển dữ liệu

Hồ sơ bệnh nhân. Sinh ra từ entity `ClinicManagement.Domain.Patients.Patient`, migration `InitialCreate`.

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `Code` | `varchar(20)` | Không | **Unique**. Mã bệnh nhân dạng `BN-000001`, sinh tự động tăng dần. |
| `FullName` | `varchar(200)` | Không | Họ tên. Có index. |
| `DateOfBirth` | `date` | Có | Ngày sinh; không được ở tương lai. |
| `Gender` | `varchar(20)` | Không | Lưu dạng chuỗi enum: `Unknown`/`Male`/`Female`/`Other`. |
| `PhoneNumber` | `varchar(20)` | Có | Số điện thoại. Có index. |
| `Address` | `varchar(500)` | Có | Địa chỉ. |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |

## Index
- `PK_patients` — khóa chính trên `Id`.
- `IX_patients_Code` — **unique**, trên `Code`.
- `IX_patients_FullName` — hỗ trợ tìm kiếm theo tên.
- `IX_patients_PhoneNumber` — hỗ trợ tìm kiếm theo số điện thoại.

## Quy tắc sinh mã
`Code = "BN-" + (số bệnh nhân hiện có + 1)` định dạng 6 chữ số. Lưu ý: chiến lược này chưa chống được đua tranh (race) khi tạo đồng thời — sẽ củng cố ở sprint sau (sequence CSDL hoặc retry khi vi phạm unique).
