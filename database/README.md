# Database

Chứa các tài nguyên cơ sở dữ liệu PostgreSQL thực thi được của dự án.

| Thư mục | Mục đích |
|---------|----------|
| `schema/` | Định nghĩa schema (DDL): bảng, khóa, index, ràng buộc |
| `migrations/` | Các script migration thay đổi cấu trúc CSDL theo thời gian |
| `seeds/` | Dữ liệu khởi tạo (seed data) cho môi trường phát triển/kiểm thử |
| `scripts/` | Script SQL tiện ích (backup, maintenance, truy vấn hỗ trợ...) |

> Sprint 0 chưa tạo schema/migration/seed. Các thư mục hiện để trống làm khung sẵn sàng.
>
> Tài liệu thiết kế (ERD, Database Dictionary, Migration Guide) đặt tại [`/docs/database`](../docs/database).
