# 0003. Chiến lược xoá mềm (soft delete)

- Trạng thái: Accepted
- Ngày: 2026-08-08

## Bối cảnh
Sprint 2 yêu cầu "ngừng sử dụng" hồ sơ (Bệnh nhân, Bác sĩ, Chuyên khoa) thay vì xoá vật lý, để bảo toàn dữ liệu lịch sử và tham chiếu (khoá ngoại). Cần chọn cách đánh dấu bản ghi đã xoá và cách ẩn chúng khỏi truy vấn mặc định.

## Các phương án
1. **Cột cờ `IsDeleted` + global query filter** (EF Core `HasQueryFilter`): bản ghi được đánh dấu, mọi truy vấn LINQ mặc định tự loại bỏ; muốn thấy bản ghi đã xoá phải chủ động `IgnoreQueryFilters()`.
2. **Cột trạng thái vòng đời** (`Status` enum: Active/Inactive/…): linh hoạt hơn nhưng phải thêm điều kiện lọc thủ công ở mọi truy vấn — dễ sót, và hiện chưa có nhu cầu nhiều trạng thái.
3. **Bảng lưu trữ (archive) riêng**: chuyển bản ghi xoá sang bảng khác — phức tạp, thừa ở quy mô đồ án.

## Quyết định
Chọn **phương án 1**: cột cờ `IsDeleted` (`bool`) + `DeletedAt` (`timestamptz?`), áp **global query filter** `e => !e.IsDeleted`.

- `IsDeleted`/`DeletedAt` đặt ở lớp cơ sở `Domain/Common/Entity`; đánh dấu qua phương thức nghiệp vụ `MarkAsDeleted()` (setter `private`). Interface `ISoftDeletable` dùng để nhận diện thực thể áp filter.
- Query filter được áp **tự động cho mọi thực thể `ISoftDeletable`** bằng vòng lặp trong `AppDbContext.OnModelCreating` (dựng biểu thức `e => !e.IsDeleted` qua Reflection) — không phải khai báo lặp ở từng cấu hình.
- Endpoint `DELETE /api/{tài nguyên}/{id}` thực hiện xoá mềm, trả **204 No Content**.

## Hệ quả
- **Ưu:** ẩn bản ghi xoá là mặc định và nhất quán toàn hệ thống; bảo toàn ràng buộc khoá ngoại; kiểm thử được với provider InMemory (đã có test).
- **Nhược/đánh đổi & lưu ý:**
  - **Unique index vẫn tính cả bản ghi đã xoá.** Mã (`BN-`/`BS-`) hay tên chuyên khoa đã xoá vẫn chiếm giá trị unique. Vì vậy sinh mã dùng `IgnoreQueryFilters().Count()` để **không tái sử dụng** số thứ tự của bản ghi đã xoá (tránh vi phạm unique). Chấp nhận ở quy mô đồ án; nếu cần cho phép trùng tên sau khi xoá, dùng *filtered unique index* (`WHERE "IsDeleted" = false`).
  - Truy vấn thống kê/toàn bộ phải nhớ `IgnoreQueryFilters()` khi muốn gồm cả bản ghi đã xoá.
  - Chưa làm chức năng khôi phục (restore) — sẽ bổ sung nếu nghiệp vụ cần.
