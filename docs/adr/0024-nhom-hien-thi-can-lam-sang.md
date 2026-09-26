# ADR 0024 — Gom nhóm hiển thị dịch vụ Cận lâm sàng khi chỉ định

**Ngày:** 2026-09-26
**Trạng thái:** Chấp nhận
**Sprint:** 25 (ngoài kế hoạch — polish sau bảo vệ)
**Liên quan:** [ADR 0015](0015-mo-hinh-can-lam-sang.md) (mô hình CLS)

---

## Bối cảnh

Phản hồi giảng viên (tuần trước buổi bảo vệ): màn **chỉ định cận lâm sàng** liệt kê dịch vụ dạng một
dải chip phẳng, gây rối mắt khi danh mục có nhiều mục — thực tế đã lên tới **23 dịch vụ** (Sprint 14,
seed `DV-CLS001..023`: xét nghiệm máu/nước tiểu, siêu âm, X-quang, CT, điện tâm đồ, nội soi…) hiển thị
chung một khối không phân nhóm ở component dùng chung `ServiceMultiPicker` (nhúng ở `VisitForm`,
`VisitDetailPage`, `LabOrderPanel`).

`ServicePrice` (ADR 0015) chỉ có `Category` rộng (`Consultation`/`Paraclinical`/`Other`) — không đủ
để gom nhóm con trong Paraclinical.

---

## Quyết định

1. **Thêm `ParaclinicalGroup`** (enum chuỗi, nullable trên `ServicePrice.Group`, chỉ có ý nghĩa khi
   `Category == Paraclinical` — ép về `null` ở constructor/`UpdateDetails` khi khác Paraclinical):
   `LabTest` (Xét nghiệm), `Imaging` (Chẩn đoán hình ảnh), `Functional` (Thăm dò chức năng),
   `Endoscopy` (Nội soi), `Other` (chưa phân loại rõ).
2. **Backfill dữ liệu seed có sẵn** ngay trong migration `AddServicePriceGroup`: 6 dịch vụ từ
   `HasData` gán qua model, 20 dịch vụ seed thô (`DV-CLS004..023`, migration `SeedMoreServicePrices`)
   backfill bằng `UPDATE ... CASE "Code" WHEN ...` (không nằm trong `HasData` nên EF không tự sinh).
3. **`ServicePriceFormPage`** (Admin/`ManageBilling`) thêm select "Nhóm CLS", chỉ hiện khi
   `category === Paraclinical` — admin gán nhóm khi thêm dịch vụ mới, tự phân loại chứ không đoán
   theo tên lúc runtime (heuristic theo tên dễ vỡ, dữ liệu tương lai không đoán được).
4. **`ServiceMultiPicker` gom theo `group`** khi toàn bộ danh sách truyền vào là Paraclinical (mọi nơi
   gọi component này đều fetch riêng theo `category=Paraclinical` nên điều kiện luôn đúng thực tế) —
   mỗi nhóm là một khối gấp/mở (`<button>` tự quản `collapsed` state, không dùng thư viện ngoài), có
   icon + đếm số mục + badge "Đã chọn N" để theo dõi khi đã gấp. Khi đang gõ tìm kiếm, mọi nhóm tự mở
   để thấy ngay kết quả trùng. Danh mục không phải Paraclinical (hoặc rỗng) vẫn hiển thị phẳng như cũ
   — không đủ dữ liệu để gom, tránh gãy các màn khác dùng chung component.
5. **Không đổi hành vi `LabOrder`/`LabOrderItem`** (ADR 0015) — nhóm chỉ là trục hiển thị lúc chỉ
   định, không snapshot vào phiếu/hoá đơn (phiếu đã có `ServiceName` đủ để đọc lại).

---

## Hệ quả

| Khía cạnh | Tác động |
|-----------|---------|
| Migration | `AddServicePriceGroup`: thêm cột `Group` (nullable, varchar 20) + backfill 23 dòng CLS có sẵn bằng SQL thô. |
| Backward compat | Dịch vụ Paraclinical do người dùng tạo trước ADR này có `Group = null` → hiển thị dưới mục "Chưa phân nhóm" (không mất/ẩn dữ liệu). |
| UI | 3 màn dùng `ServiceMultiPicker` (`VisitForm`, `VisitDetailPage`, `LabOrderPanel`) tự động thừa hưởng — không sửa riêng từng màn. `ServicePricesListPage` hiện thêm nhãn nhóm cạnh badge phân loại. |
| Test | Không có test FE tự động (dự án chỉ `tsc -b && vite build`); đã kiểm qua Chrome thủ công (front-desk, mở lượt mới, gấp/mở nhóm, chọn dịch vụ, tìm kiếm). |
| Ngoài phạm vi | Không đổi cách hiển thị danh sách phiếu **đã chỉ định** (`LabOrderCard`) theo nhóm — số mục mỗi phiếu thường nhỏ, không phải điểm giảng viên chê; không thêm nhóm cho `ServiceCategory.Other`. |
