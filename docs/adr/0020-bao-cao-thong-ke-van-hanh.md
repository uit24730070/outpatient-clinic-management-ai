# 0020. Báo cáo & thống kê vận hành (Dashboard)

- Trạng thái: Accepted
- Ngày: 2026-09-12

## Bối cảnh
Sprint 20 mở **Epic 7 — Reporting**, đặt **cuối lộ trình** vì cần đủ dữ liệu từ các epic trước: viện phí/doanh thu (Sprint 14, Epic 10), cận lâm sàng (Sprint 15), lịch/phòng (Sprint 18), hàng đợi/sinh hiệu (Sprint 19). Đến đây cả hai workflow đã khép vòng đời; Dashboard là **lớp quan sát** cho quản lý. Cần chốt: (1) tổng hợp ở đâu (có thêm bảng/materialized view không); (2) mô hình DTO chỉ số; (3) xử lý múi giờ khi gom theo "ngày"; (4) RBAC (ai xem gì, bác sĩ xem "của mình"); (5) phạm vi (phần nợ).

## Quyết định

### 1. Tổng hợp phía server, không thêm bảng nghiệp vụ
- Thêm `Application/Reports/` (`IReportService` + `ReportService`) + `WebApi/Controllers/ReportsController` (`/api/reports/*`). **Không** entity/migration mới — tổng hợp trực tiếp từ bảng hiện có bằng `Count`/`Sum`/`GroupBy`, `AsNoTracking`, đi qua global query filter (**tôn trọng soft-delete**, không `IgnoreQueryFilters`).
- **Không** cache/materialized view ở sprint này — chưa cần ở quy mô đồ án; giữ số liệu luôn tươi.

### 2. Bốn báo cáo + DTO chỉ số tường minh
- **R-01 overview** (`OverviewDto`): tổng bệnh nhân/bác sĩ đang hoạt động, lịch hôm nay (tổng + phân rã trạng thái), phiếu khám hôm nay, doanh thu hôm nay (HĐ `Paid`), số đang chờ hàng đợi.
- **R-02 lịch theo khoảng** (`AppointmentReportDto`): gom theo ngày × trạng thái, **đủ mọi ngày** trong khoảng (kể cả ngày 0), lọc tuỳ chọn `doctorId`.
- **R-03 năng suất bác sĩ** (`DoctorProductivityDto[]`): mỗi bác sĩ số lịch / hoàn tất / phiếu khám.
- **R-04 doanh thu** (`RevenueReportDto`): theo ngày × loại khoản mục (công khám/thuốc/CLS/khác) từ `Invoice`/`InvoiceItem` `Paid`.
- **Phân rã trạng thái dùng field tường minh** (`AppointmentStatusBreakdown`) thay cho dictionary theo enum — vì hệ thống **serialize enum thành số** (không có `JsonStringEnumConverter`), tránh key số khó đọc cho FE.

### 3. Múi giờ "ngày" — hằng số UTC+7
- Cột thời gian là `timestamptz` (UTC). "Hôm nay" và gom theo ngày quy đổi **UTC+7** (`ClinicOffset`, thống nhất ADR 0018–0019). Mỗi ngày địa phương → khoảng `[00:00, 24:00)` giờ phòng khám, đổi sang mốc `DateTimeOffset` để so cột UTC trong SQL (dịch được).
- **Lọc khoảng** (so `DateTimeOffset`) chạy ở SQL; **gom theo ngày địa phương** thực hiện **trong bộ nhớ** trên tập nhỏ đã lọc (quy đổi múi giờ không dịch được sang SQL). Đánh đổi chấp nhận ở quy mô đồ án; chặn `from>to` (`Report.InvalidRange`) và khoảng > 366 ngày (`Report.RangeTooWide`). Thiếu `from/to` → mặc định 7 ngày gần nhất.

### 4. RBAC
- Overview/lịch/doanh thu = `Roles.ManageStaff` (Admin/Lễ tân) — dữ liệu tài chính & vận hành cho quản lý.
- Năng suất bác sĩ mở thêm cho **Bác sĩ** nhưng **ép chỉ xem của mình**: controller truyền `CurrentUserId` khi vai trò là Bác sĩ; service phân giải `Doctor.UserId → doctorId` (tra server-side, mẫu ADR 0009) và ghi đè tham số `doctorId`; tài khoản bác sĩ chưa gắn hồ sơ → trả rỗng.
- FE: nav "Tổng quan" + landing Admin/Lễ tân đổi `/appointments → /dashboard`; guard route `MANAGE_STAFF`. Guard chỉ là UX — backend vẫn chốt 401/403.

### 5. Phạm vi (phần nợ)
- **Xuất báo cáo** (PDF/Excel), lịch gửi báo cáo định kỳ.
- **Cache/materialized view** cho tổng hợp (khi dữ liệu lớn).
- Báo cáo **xuất–nhập–tồn kho** chi tiết, thống kê **sinh hiệu** (xu hướng).
- Biểu đồ FE dùng **CSS/flex thuần** (không thêm thư viện chart) — giữ bundle gọn, đúng tsconfig nghiêm ngặt.

## Hệ quả
- **Ưu:** dashboard trực quan cho quản lý mà không tăng bề mặt dữ liệu (0 bảng/migration mới); rủi ro thấp (chỉ-đọc); tái dùng RBAC & quy ước múi giờ đã chốt; số liệu luôn tươi.
- **Nhược/đánh đổi:**
  - Gom theo ngày trong bộ nhớ (sau khi lọc SQL theo khoảng) — không tối ưu bằng gom hẳn ở SQL, nhưng đúng múi giờ và đủ nhanh ở quy mô này.
  - Không cache — mỗi lần mở dashboard chạy vài truy vấn tổng hợp; chấp nhận được.
  - Biểu đồ CSS thuần đơn giản (không tooltip/zoom nâng cao) — đủ minh hoạ, tránh phụ thuộc.
