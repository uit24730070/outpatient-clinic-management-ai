# 0002. Sử dụng EF Core (Npgsql) và chiến lược migration

- Trạng thái: Accepted
- Ngày: 2026-08-08

## Bối cảnh
Sprint 1 cần lớp truy cập dữ liệu tới PostgreSQL cho vertical slice "Quản lý Bệnh nhân". Cần chọn ORM và cách quản lý thay đổi schema (migration) sao cho phù hợp Clean Architecture và giữ lớp Application độc lập với provider CSDL.

## Quyết định
- Dùng **Entity Framework Core 8** với provider **Npgsql.EntityFrameworkCore.PostgreSQL**.
- Lớp **Application** chỉ phụ thuộc trừu tượng `IAppDbContext` (khai báo `DbSet<>` + `SaveChangesAsync`); hiện thực `AppDbContext` nằm ở **Infrastructure**.
- Truy vấn trong Application **không dùng API riêng của provider** (ví dụ `ILike`) để giữ tính di động — dùng `ToLower().Contains(...)` cho tìm kiếm không phân biệt hoa thường.
- Mapping cấu hình qua `IEntityTypeConfiguration<T>` (fluent), không dùng data annotation trên entity.
- Migration là **code-first**, lưu tại `Infrastructure/Persistence/Migrations`, sinh/áp dụng qua `dotnet ef` với startup project là WebApi.
- Dấu thời gian kiểm toán (`CreatedAt`/`UpdatedAt`) được gán tập trung khi override `SaveChangesAsync`.

## Hệ quả
- **Ưu:** Application kiểm thử được bằng provider InMemory; đổi provider CSDL ít ảnh hưởng nghiệp vụ; schema có lịch sử rõ ràng qua migration.
- **Nhược/đánh đổi:** Application vẫn tham chiếu gói `Microsoft.EntityFrameworkCore` (chấp nhận, theo phong cách template Clean Architecture phổ biến); `ToLower().Contains` không tận dụng index tối ưu bằng full-text — chấp nhận ở quy mô đồ án.
- Cần một PostgreSQL đang chạy để áp dụng/kiểm thử migration (local hoặc container thủ công cho tới khi F-08 hoàn thiện Docker Compose).
