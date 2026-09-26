# ADR 0026 — Tự lập hoá đơn ngay lúc tiếp nhận (bỏ thao tác "Lập hoá đơn" rời)

**Ngày:** 2026-09-26
**Trạng thái:** Chấp nhận
**Sprint:** 25 (ngoài kế hoạch — polish sau bảo vệ)
**Liên quan:** [ADR 0017](0017-mo-hinh-luot-tiep-don-visit.md) (Lượt tiếp nhận), [ADR 0021](0021-thanh-toan-truoc-khi-thuc-hien.md) (thu trước khi thực hiện)

---

## Bối cảnh

Phản hồi: ở workspace Lễ tân (`/front-desk`, UX-03), sau khi chốt dịch vụ khám + CLS lúc "Tiếp nhận
mới", panel thu tiền mở ra ngay (đã có từ UX-03) nhưng **chưa có gì để thu** — `VisitService.CreateAsync`
chỉ tạo `Appointment`/`LabOrder`, không tự lập `Invoice`. Lễ tân phải rời panel, vào `VisitDetailPage`,
bấm "Lập hoá đơn" (điều hướng sang `InvoiceFormPage`, chọn lại đúng những dịch vụ vừa chọn), rồi mới
quay lại thu tiền được — dư một vòng thao tác cho đúng một việc "thu tiền ngay lúc tiếp nhận" vốn là
kịch bản chính của phòng khám nhỏ tự thu tiền (không BHYT).

## Quyết định

- **`VisitForm.submit()` (FE) tự gọi `createInvoice` ngay sau `createVisit` thành công** — gộp mọi
  dịch vụ khám có giá (`appointments[].servicePriceId != null`) + phiếu CLS walk-in vừa tạo (nếu có)
  vào **một hoá đơn Draft duy nhất**, tái dùng nguyên endpoint `POST /api/invoices` đã hỗ trợ sẵn
  `AppointmentIds[]` + `LabOrderId` gộp cấp Lượt (từ UX-05/ADR 0021 mở rộng) — **không đổi backend**.
- Lỗi lập hoá đơn (hiếm, vd trùng mã) **không chặn việc tạo lượt** — lượt vẫn tạo thành công, chỉ báo
  lỗi kèm hướng dẫn vào Chi tiết lượt lập tay (đường cũ vẫn còn nguyên, làm phương án dự phòng).
- **`VisitQuickPayPanel` hiện thêm danh sách "Dịch vụ đã chọn"** (tên dịch vụ + loại dòng + thành tiền,
  đọc từ `Invoice.Items` vừa lập) phía trên khối Đã lập/Đã thu/Còn nợ — lễ tân soát lại trước khi bấm
  thu tiền, đúng góp ý "panel nên hiển thị dịch vụ đã chọn".
- Sửa kèm một lỗi có sẵn: `onPayAll` trước đó chỉ cập nhật state hoá đơn, không nạp lại `visit` → khối
  Đã lập/Đã thu/Còn nợ (tính từ `visit.totalPaid/totalOutstanding`) hiện sai số dư trong vài giây sau
  khi thu tiền thành công (phải đợi vòng tự làm mới định kỳ). Nay `onPayAll` gọi lại `getVisit` luôn.

### Vì sao không tự lập hoá đơn ở Backend (`VisitService.CreateAsync`)

Cân nhắc nhưng chọn làm ở FE: `InvoiceService.CreateAsync` đã đúng là nơi duy nhất chứa toàn bộ logic
gating/snapshot/chống lập trùng (ADR 0014/0015/0021) — để `VisitService` gọi chéo sang `InvoiceService`
sẽ phá vỡ ranh giới hai aggregate hiện tại (thêm phụ thuộc service-to-service một chiều mới, trong khi
FE đã có đủ dữ liệu từ chính response `createVisit` để tự ráp request). Giữ ở FE cũng khớp cách
`VisitDetailPage` đang tự ráp `AppointmentIds`/`LabOrderId` cho nút "Lập hoá đơn" sẵn có — chỉ khác là
gọi tự động thay vì đợi người bấm.

---

## Hệ quả

| Khía cạnh | Tác động |
|-----------|---------|
| Backend | Không đổi — tái dùng nguyên `POST /api/invoices`. |
| UX | Lễ tân: chốt dịch vụ → "Tạo lượt tiếp nhận" → panel đã sẵn hoá đơn + danh sách dịch vụ → "Thu tiền cả lượt". Bớt 1 vòng điều hướng (Chi tiết lượt → Lập hoá đơn → quay lại) cho kịch bản phổ biến nhất. |
| Tương thích | Lượt không có dịch vụ tính phí (tất cả `servicePriceId == null`, không CLS) không tự lập hoá đơn — không có gì để thu, đúng hành vi cũ. Thêm dịch vụ vào lượt **đang mở** sau đó (`AddVisitService`, ngoài `VisitForm`) vẫn theo đường cũ (nút "Lập hoá đơn" thủ công) — ngoài phạm vi ADR này. |
| Ngoài phạm vi | Tự thu tiền luôn (không chỉ tự lập hoá đơn) — vẫn cần lễ tân xác nhận phương thức + bấm thu, vì đây là hành động tài chính không nên tự động hoá không có xác nhận người dùng. |
