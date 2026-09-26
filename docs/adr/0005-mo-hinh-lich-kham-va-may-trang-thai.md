# 0005. Mô hình lịch khám & máy trạng thái (Appointment)

- Trạng thái: Accepted
- Ngày: 2026-08-08

## Bối cảnh
Sprint 4 đưa **đặt lịch khám** và **tiếp đón (check-in)** vào hệ thống. `Appointment` là thực thể đầu tiên có:
- **khoá ngoại kép** (tới `Patient` và `Doctor`), và
- **máy trạng thái (state machine)** — vòng đời trạng thái có ràng buộc chuyển tiếp.

Cần chốt ba điểm: (1) cách biểu diễn thời gian, (2) tập trạng thái + chuyển tiếp hợp lệ, (3) quy tắc chống trùng lịch và mức độ kiểm tra ở sprint này. Giữ nguyên phong cách Clean Architecture, envelope `ApiResponse`, và nền RBAC đã có ở Sprint 3.

## Các phương án & quyết định

### 1. Biểu diễn thời gian
- **Phương án A — `StartTime` + `EndTime` (`timestamptz`, UTC).** Linh hoạt về thời lượng, khớp với các mốc thời gian sẵn có (`CreatedAt`… đều `DateTimeOffset`), dễ kiểm tra chồng khung giờ bằng so sánh khoảng.
- Phương án B — khung giờ cố định (slot rời rạc). Đơn giản hơn nhưng cứng nhắc, khó mở rộng thời lượng khám khác nhau.

**Chọn A.** `StartTime`/`EndTime` kiểu `DateTimeOffset` (map `timestamptz`), lưu UTC; quy đổi hiển thị ở frontend. Ràng buộc `EndTime > StartTime` (validator + guard domain). Tránh lẫn `DateOnly`/`DateTimeOffset` (xem Rủi ro).

### 2. Tập trạng thái & chuyển tiếp
Enum `AppointmentStatus` **lưu chuỗi** (`HasConversion<string>()`, đồng nhất với `Gender`/`UserRole`):

```
Scheduled  (khởi tạo)
CheckedIn
InProgress
Completed  (kết thúc)
Cancelled  (kết thúc)
NoShow     (kết thúc)
```

**Chuyển tiếp hợp lệ** (nguồn sự thật duy nhất đặt ở Domain):

| Từ | Sang |
|----|------|
| `Scheduled` | `CheckedIn`, `Cancelled`, `NoShow` |
| `CheckedIn` | `InProgress`, `Cancelled`, `NoShow` |
| `InProgress` | `Completed`, `Cancelled` |
| `Completed` / `Cancelled` / `NoShow` | *(kết thúc — không chuyển tiếp)* |

- Chuyển tiếp **không hợp lệ** → `Error.Conflict` mã `Appointment.InvalidTransition` (409). Chọn `Conflict` (không phải `Validation`) vì lỗi thuộc **xung đột trạng thái hiện tại của tài nguyên**, không phải dữ liệu đầu vào sai.
- Mốc thời gian: ghi `CheckedInAt` khi vào `CheckedIn`. Các mốc khác dựa vào `UpdatedAt` (đủ ở quy mô đồ án; không audit từng bước để tránh phình schema).
- `Cancelled` là **trạng thái nghiệp vụ** (vẫn hiển thị trong lịch sử), **khác** `IsDeleted` (ẩn hẳn) — phân biệt như `IsActive` vs `IsDeleted` của `User`.

Máy trạng thái hiện thực bằng các method Domain trả `Result` (`CheckIn`, `Start`, `Complete`, `Cancel`, `MarkNoShow`), **không rải `if` ở service/controller** — để test được độc lập.

### 3. Chống trùng lịch (double-booking)
Một **bác sĩ** không được có hai lịch **chồng khung giờ** (`StartTime < b.EndTime && EndTime > b.StartTime`), chỉ xét các lịch còn **chiếm chỗ** (trạng thái khác `Cancelled`/`NoShow`). Vi phạm → `Error.Conflict` mã `Appointment.Overlap` (409).

Kiểm tra ở **tầng service** (như sinh mã `BN-`/`BS-` ở Sprint 1/2): có **điều kiện đua (race)** về lý thuyết — chấp nhận ở quy mô đồ án. Ghi chú giới hạn: nếu cần chặt chẽ có thể thêm **exclusion constraint** của PostgreSQL (`tstzrange` + `btree_gist`) ở sprint sau.

### 4. RBAC
- **Ghi** (tạo/sửa/huỷ/check-in/chuyển trạng thái): **Admin/Lễ tân** (`Roles.ManageStaff`), như các slice danh mục.
- **Đọc** (danh sách/chi tiết/hàng đợi): mọi vai trò đã đăng nhập.
- **Bác sĩ ở sprint này chỉ đọc.** Chưa cho bác sĩ tự cập nhật trạng thái khám của mình vì **chưa có liên kết `User ↔ Doctor`** (không thể lọc "lịch của tôi" một cách tin cậy). Ghi nhận là hạn chế, để mở khi bổ sung liên kết đó (Sprint 5+).

## Hệ quả
- **Ưu:** mô hình thời gian linh hoạt; máy trạng thái tập trung ở Domain, test độc lập; chống trùng đơn giản, đủ dùng; RBAC nhất quán với Sprint 3.
- **Nhược/đánh đổi:**
  - Chống trùng ở service có race — chấp nhận; lối thoát là exclusion constraint DB.
  - Bác sĩ chỉ đọc — thiếu luồng "bác sĩ cập nhật khám của mình" cho tới khi có liên kết `User ↔ Doctor`.
  - Không audit từng bước chuyển trạng thái (chỉ `CheckedInAt` + `UpdatedAt`) — đủ cho quy mô hiện tại.
