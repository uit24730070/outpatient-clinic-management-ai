# Bảng `specialties` — Từ điển dữ liệu

Chuyên khoa khám chữa bệnh (bảng tra cứu). Sinh ra từ entity `ClinicManagement.Domain.Specialties.Specialty`, migration `AddSpecialtiesDoctorsAndSoftDelete`.

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `Name` | `varchar(150)` | Không | **Unique**. Tên chuyên khoa. |
| `Description` | `varchar(500)` | Có | Mô tả. |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật. |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (mặc định `false`). Bản ghi `true` bị ẩn khỏi truy vấn mặc định. |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

## Index
- `PK_specialties` — khóa chính trên `Id`.
- `IX_specialties_Name` — **unique**, trên `Name`.

## Ghi chú
- Kiểm tra trùng tên ở lớp Application (không phân biệt hoa/thường) trả lỗi `Specialty.NameConflict` (409) trước khi chạm unique index của DB.
- Chiến lược xoá mềm: xem [ADR 0003](../adr/0003-chien-luoc-soft-delete.md). Tên của bản ghi đã xoá vẫn chiếm giá trị unique.
- Seed sẵn 5 chuyên khoa mẫu (`HasData`): Nội tổng quát, Tim mạch, Nhi khoa, Tai mũi họng, Da liễu.
