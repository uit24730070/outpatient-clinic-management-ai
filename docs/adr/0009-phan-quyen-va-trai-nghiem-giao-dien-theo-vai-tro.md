# 0009. Phân quyền & trải nghiệm giao diện theo vai trò (+ liên kết User↔Doctor)

- Trạng thái: Accepted
- Ngày: 2026-08-09

## Bối cảnh
Từ Sprint 3, hệ thống đã có JWT + RBAC ở backend ([ADR 0004](0004-chien-luoc-xac-thuc-va-phan-quyen.md)): mọi controller nghiệp vụ `[Authorize]`, ghi = `Roles.ManageStaff` (Admin/Lễ tân) hoặc `Roles.RecordEncounter` (Admin/Bác sĩ). Nhưng **frontend** mới chỉ ẩn/hiện nút theo hai cờ rải rác (`canManage`/`canRecordEncounter`): mọi vai trò vẫn thấy **chung một menu 4 mục**, cùng đáp về `/appointments`, và vào được **mọi route**. Trải nghiệm chưa cá nhân hoá theo phận sự.

Song song, [ADR 0005](0005-mo-hinh-lich-kham-va-may-trang-thai.md)/[0006](0006-mo-hinh-benh-an-encounter-prescription.md) còn **nợ liên kết `User↔Doctor`**: bác sĩ đăng nhập không biết mình ứng với hồ sơ `Doctor` nào, nên không thể lọc "lịch/phiếu của tôi" — phải thao tác trên **mọi** bệnh nhân.

Sprint 8 chốt: (1) ma trận quyền `vai trò × tài nguyên × hành động`; (2) mô hình quyền tập trung ở frontend; (3) guard route theo vai trò + trang mặc định; (4) mô hình `User↔Doctor` + cách lộ "doctorId của tôi". Giữ nguyên Clean Architecture, envelope `ApiResponse`, và **backend là chốt chặn bảo mật** — FE chỉ là lớp UX.

## Các quyết định

### 1. Ma trận quyền (đồng bộ RBAC backend hiện có)
Không đổi ma trận **ghi** đã chốt; chỉ *tailor hiển thị* và bổ sung lọc "của tôi".

| Tài nguyên / Hành động | Admin | Lễ tân | Bác sĩ |
|---|:--:|:--:|:--:|
| Bệnh nhân — đọc | ✅ | ✅ | ✅ |
| Bệnh nhân — ghi (thêm/sửa/xoá) | ✅ | ✅ | ❌ |
| Chuyên khoa / Bác sĩ — đọc | ✅ | ✅ | ✅ (BE) |
| Chuyên khoa / Bác sĩ — ghi | ✅ | ❌ ¹ | ❌ |
| Lịch khám — đọc | ✅ | ✅ | ✅ |
| Lịch khám — đặt/sửa/chuyển trạng thái | ✅ | ✅ | ❌ |
| Phiếu khám/đơn thuốc — ghi | ✅ | ❌ | ✅ |
| Tóm tắt/Hỏi đáp AI | ✅ | ❌ | ✅ |
| Phòng khám của tôi (lọc theo bác sĩ) | — | — | ✅ |

- Khớp `Roles.ManageStaff` (Admin/Lễ tân) và `Roles.RecordEncounter` (Admin/Bác sĩ).
- ¹ **Cập nhật Sprint 10:** danh mục master Bác sĩ/Chuyên khoa **ghi** siết về **chỉ Admin** (`Roles.ManageCatalog`) — Lễ tân không còn quản lý được (trước đây thuộc `ManageStaff`). **Đọc** vẫn mở cho Lễ tân/Bác sĩ (cần chọn bác sĩ khi đặt lịch + công cụ trợ lý `list_doctors`). FE ẩn nav + guard route `/doctors`,`/specialties` về Admin.
- **Tailor hiển thị:** dù backend cho Bác sĩ **đọc** danh mục Bác sĩ/Chuyên khoa, FE **ẩn** khỏi menu và **guard chặn** route quản lý danh mục cho Bác sĩ (không thuộc phận sự) — thu hẹp UX, không nới quyền.

### 2. Mô hình quyền frontend tập trung + nav khai báo
- Gom về **một nguồn sự thật** `src/config/access.ts`: helper `canManageStaff(role)`, `canRecordEncounter(role)` (khớp tên hằng RBAC backend), cấu hình `navItems` (mỗi mục gắn `roles` được thấy), `roleLandingPath`, và `navItemsFor/landingPathFor`.
- `auth store` chỉ *dẫn xuất* các cờ từ module này (giữ tương thích `canManage`/`canRecordEncounter` cho các trang cũ) và expose thêm `doctorId`.
- `MainLayout` render menu **theo cấu hình** (`navItemsFor(role)`), không liệt kê cứng. Thêm/đổi mục chỉ sửa `access.ts`.

### 3. Guard route + landing theo vai trò
- `RequireRole roles={[...]}` (đặt bên trong `RequireAuth`): vai trò ngoài danh sách → chuyển `/forbidden` (trang 403 thân thiện, có link về trang chính theo vai trò). Nhóm route bọc guard theo đúng ma trận ghi/đọc ở mục 1.
- `/` điều hướng động: `RoleLanding` → `landingPathFor(role)` (Admin/Lễ tân → `/appointments`; Bác sĩ → `/my-clinic`).
- **Phòng thủ nhiều lớp:** guard FE chỉ cải thiện UX; API vẫn trả **403** khi ghi sai quyền. Guard FE **không được nới lỏng** so với RBAC backend.

### 4. Mô hình `User↔Doctor`
- Thêm `Doctor.UserId?` (**nullable, unique**). Một tài khoản ↔ tối đa một hồ sơ bác sĩ; nhiều hồ sơ **chưa gắn** tài khoản cùng tồn tại (Postgres cho phép nhiều `NULL` trong unique index). FK `OnDelete: SetNull` — xoá tài khoản chỉ gỡ liên kết, không xoá hồ sơ.
- Method Domain `AssignUser(userId)`/`UnassignUser()`. Migration `AddDoctorUserLink`. **Seed** thêm một tài khoản Bác sĩ demo (`bacsi` / `Doctor@123`, hash BCrypt tất định) gắn hồ sơ `BS-000001` (chuyên khoa Nội tổng quát).

### 5. Cách lộ "doctorId của tôi" — tra cứu server-side qua `/api/auth/me` (KHÔNG nhét claim JWT)
- **Chọn tra cứu server-side:** `AuthService` (login + `/me`) truy `Doctors.Where(d => d.UserId == userId)` và trả `UserDto.DoctorId`. Tránh hoàn toàn **vấn đề token cũ** (nếu nhét claim thì token phát trước khi gắn hồ sơ sẽ thiếu `doctorId`, buộc đăng nhập lại). Chi phí thêm một truy vấn nhẹ, chấp nhận được.
- **User Bác sĩ chưa gắn hồ sơ** → `doctorId = null`: FE hiển thị thông báo "nhờ Admin liên kết", **không crash**, không lọc được "của tôi".
- **"Của tôi" tái dùng bộ lọc sẵn có:** không thêm endpoint mới — màn "Phòng khám của tôi" gọi `GET /api/appointments?doctorId={doctorId}` rồi lọc trạng thái `CheckedIn`/`InProgress` phía client.

## Hệ quả
- **Tích cực:** mỗi vai trò có menu gọn + trang mặc định + guard đúng phận sự; quyền FE tập trung (dễ soát, khớp BE); trả nốt nợ `User↔Doctor` — bác sĩ thấy đúng bệnh nhân của mình; không phát sinh endpoint/bảng vector mới.
- **Đánh đổi:** guard FE và RBAC BE là **hai nơi** phải giữ đồng bộ (đã tập trung FE về `access.ts` để giảm rủi ro lệch). Lọc "của tôi" phía client giới hạn ở trang đầu (pageSize 100) — đủ cho quy mô một ngày khám; nếu cần chính xác tuyệt đối, sau này thêm tham số/endpoint chuyên biệt.
- **Còn nợ:** màn **quản lý người dùng** (CRUD tài khoản + gắn/gỡ `User↔Doctor` qua UI) chưa làm — hiện chỉ gắn qua seed/migration; để Sprint sau.
