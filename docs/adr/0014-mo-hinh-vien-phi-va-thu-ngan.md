# 0014. Mô hình viện phí & thu ngân

- Trạng thái: Accepted
- Ngày: 2026-08-22
- Liên quan: [ADR 0005](0005-mo-hinh-lich-kham-va-may-trang-thai.md) (máy trạng thái + snapshot), [ADR 0006](0006-mo-hinh-benh-an-encounter-prescription.md) (owned collection), [ADR 0011](0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md) (kho thuốc / giá bán), [ADR 0013](0013-vai-tro-duoc-si-quan-ly-kho-thuoc.md) (RBAC vai trò)

## Bối cảnh

Vòng đời bệnh nhân khám ngoại trú thiếu mắt xích **thu ngân/viện phí**: sau khi khám xong
(phiếu `Completed` + đã cấp thuốc), phòng khám phải **lập hoá đơn** (công khám + tiền thuốc đã
cấp + dịch vụ lẻ), **thu tiền** và **in**. Đây cũng là **trường tiền tệ đầu tiên** của hệ thống,
và là dữ liệu nguồn cho **Dashboard doanh thu** (Sprint 15). Không xử lý BHYT/đồng chi trả ở
phạm vi này.

## Quyết định

1. **Tiền tệ = `decimal` (map `numeric(18,2)`), đơn vị VND.** Tuyệt đối **không** dùng
   `float`/`double`. Cộng tổng ở **server** (`Sum` trên `decimal`), không cộng ở client. FE chỉ
   hiển thị, định dạng locale `vi-VN` (`Intl.NumberFormat` currency VND, không phần thập phân).

2. **Bảng giá dịch vụ `ServicePrice`** (vertical slice CRUD như `Medication`/`Specialty`): mã
   `DV-` (đếm `IgnoreQueryFilters()`), `Name`, `UnitPrice`, `Description?`, soft delete. Dùng cho
   dòng "công khám" và dịch vụ lẻ (thủ thuật, tư vấn…).

3. **Giá bán thuốc `Medication.SalePrice`** (mặc định 0, tham số ctor mặc định như [ADR 0011] →
   không phá đơn/test cũ). **Không** đụng luồng tồn kho/FEFO — chỉ để tính tiền thuốc trên hoá đơn.

4. **Hoá đơn `Invoice`** là aggregate root có owned collection `InvoiceItem` (cùng khuôn
   `PrescriptionItem`/`StockReceiptItem`): mã `HD-`, snapshot `PatientId`, `EncounterId?`
   (nullable + **unique** → 1 phiếu khám ↔ tối đa 1 hoá đơn; nhiều NULL OK trên Postgres),
   `Status`, `TotalAmount` (tính lại khi `ReplaceItems`), `PaidAt?`, `PaymentMethod?`. Mỗi
   `InvoiceItem` có `ItemType` (`ServiceFee`/`Medication`/`Other`, lưu chuỗi), `Description`,
   `UnitPrice` **snapshot**, `Quantity`, `LineTotal`, `ReferenceId?` (trỏ `ServicePrice`/
   `Medication` để tra soát).

5. **Snapshot giá.** `InvoiceItem.UnitPrice` copy tại thời điểm lập/sửa hoá đơn — đổi bảng giá
   hoặc `SalePrice` sau **không** ảnh hưởng hoá đơn cũ (giống snapshot bệnh nhân/bác sĩ của phiếu
   khám). Không join giá "sống" khi hiển thị.

6. **Lập hoá đơn on-demand, KHÔNG tự động khi chốt phiếu.** Thu ngân bấm "Tạo hoá đơn từ phiếu
   khám" (`POST /api/invoices/from-encounter/{encounterId}`): tự dựng dòng **công khám** (từ dịch
   vụ mặc định — cấu hình `Billing:DefaultConsultationServiceCode`, mặc định `DV-000001`, POCO
   `BillingOptions` singleton) + dòng **thuốc đã cấp** (từ `PrescriptionItem` có `MedicationId`,
   `Quantity × SalePrice`, snapshot). Tránh móc vào `Encounter.Complete`/cấp phát FEFO nặng
   ([ADR 0011]) ⇒ rủi ro hồi quy thấp, đúng thực tế (thu ngân là bước riêng ở quầy). Cũng cho tạo
   **hoá đơn lẻ** không gắn phiếu (`POST /api/invoices`, dòng tham chiếu `ServicePriceId`).

7. **Máy trạng thái `InvoiceStatus`** ở Domain: `Draft` → `Paid` / `Cancelled`. Sửa dòng
   (`ReplaceItems`) + huỷ + xoá mềm **chỉ khi `Draft`**; `Pay(method)` đặt `PaidAt`/`PaymentMethod`
   rồi khoá **bất biến**. Chuyển sai vòng đời → `Billing.InvalidTransition` (409).

8. **RBAC `Roles.ManageBilling` = Admin + Lễ tân.** Cả **ghi lẫn đọc** dữ liệu tài chính chỉ
   Admin/Lễ tân; **Bác sĩ/Dược sĩ không thấy hoá đơn**. Guard FE (`canManageBilling`, nav, route
   `MANAGE_BILLING`) chỉ là UX — backend vẫn chốt 403.

9. **Đồng thời.** Tạo hoá đơn trùng cho 1 phiếu có race như sinh mã `BN-`/`HD-`; unique index
   `EncounterId` chặn ở DB (kiểm ở service + bắt `DbUpdateException` → `Billing.InvoiceAlreadyExists`
   409). Sinh mã `HD-`/`DV-` đếm `IgnoreQueryFilters()`.

## Hệ quả

- **Khép kín vòng đời** đến khâu ra về: khám → cấp thuốc → lập hoá đơn → thu tiền → in. Dashboard
  doanh thu (Sprint 15) đọc trực tiếp `invoices`/`invoice_items` (phân loại theo `ItemType`).
- **Đánh đổi / phần còn nợ:** chưa có **hoàn tiền/huỷ hoá đơn đã thu** (refund), **thanh toán một
  phần**/công nợ, **BHYT/đồng chi trả/miễn giảm**, **hoá đơn điện tử (HĐĐT)** chuẩn thuế, tích hợp
  cổng thanh toán. Sửa hoá đơn = **thay cả cụm dòng** (`ReplaceItems`) — sửa hoá đơn từ phiếu khám
  sẽ mất dòng tiền thuốc (chỉ nhận lại dòng dịch vụ), nên chủ yếu dùng cho hoá đơn lẻ.
- "Công khám mặc định" hiện chốt **một mã dịch vụ cấu hình** (`DV-000001`); mở rộng theo chuyên
  khoa/bác sĩ sau nếu cần.
- **Đã kiểm chứng đầu-cuối** trên pgvector: migration `AddBillingAndServicePrices` áp sạch (seed 3
  dịch vụ + `sale_price`); lập hoá đơn từ phiếu (công khám + thuốc, tổng đúng, snapshot); thu tiền
  `Draft→Paid`; hoá đơn `Paid` bất biến; Bác sĩ/Dược sĩ 403. 133 unit test xanh.

## Bổ sung P2 — Nhiều hoá đơn / lượt & thu ngân lúc tiếp đón (Sprint 14.5, BILL-06..10)

- Trạng thái: Accepted · Ngày: 2026-08-22

**Bối cảnh.** Mô hình P1 chốt **1 phiếu khám ↔ 1 hoá đơn** (gộp công khám + thuốc, lập on-demand
sau khi khám). Thực tế phòng khám cần **nhiều hoá đơn cho một lượt** theo từng giai đoạn: thu
**công khám lúc tiếp đón**, thu **cận lâm sàng** (kể cả bệnh nhân **chỉ đến làm CLS**, không khám),
thu **thuốc** sau khi khám. Ngoài ra cần **thu trước hoặc thu sau** linh hoạt.

**Quyết định (thay đổi so với P1):**

1. **Mô hình A — nhiều hoá đơn / lượt.** **Bỏ ràng buộc unique `Invoice.EncounterId`** (đổi thành
   index thường): một phiếu khám có thể có nhiều hoá đơn (vd HĐ công khám + HĐ thuốc). Bỏ lối kiểm
   `AnyAsync(EncounterId)` + bắt `DbUpdateException` của điểm (9) P1.
2. **Gắn lượt tiếp đón.** Thêm `Invoice.AppointmentId?` (nullable, ctor tham số mặc định — HĐ cũ
   không gãy) để lập hoá đơn **trước khi có `Encounter`** (lúc mới check-in) và **gom** các hoá đơn
   cùng lượt. Bệnh nhân vãng lai/chỉ-CLS: HĐ gắn `PatientId`, `AppointmentId` null.
3. **Lập hoá đơn lúc tiếp đón từ dịch vụ.** `POST /api/invoices` (hoá đơn dịch vụ, đã có) nhận thêm
   `appointmentId?`; lễ tân chọn ≥1 dịch vụ từ **bảng giá** (khám/tái khám/CLS) → dựng dòng snapshot
   giá. Không cần encounter. Lượt truyền vào được kiểm tồn tại (`Appointment.NotFound` 404).
4. **`from-encounter` giờ chỉ dựng dòng thuốc.** Bỏ tự thêm dòng công khám (công khám đã thu lúc
   tiếp đón). Phiếu không có dòng thuốc gắn danh mục → `Billing.NoMedicationToInvoice` (400).
   ⇒ `BillingOptions`/`DefaultConsultationServiceCode` **không còn dùng** ở luồng lập hoá đơn.
5. **Chống tính phí trùng bằng cờ trên nguồn** (thay lá chắn unique đã bỏ): `Encounter.MedicationInvoicedAt?`
   + `Encounter.MarkMedicationInvoiced()` (Domain, chỉ đặt 1 lần). Lập HĐ thuốc lần 2 →
   `Billing.MedicationAlreadyInvoiced` (409). Đây là **phần dễ sai nhất** — thiếu sẽ thu tiền 2 lần.
6. **Thu tiền linh hoạt = hệ quả của `Draft`+`Pay`** (không thêm trạng thái): trả trước = tạo HĐ rồi
   `Pay` ngay; trả sau = để `Draft`, `Pay` khi ra về.
7. **Gom theo lượt.** `GET /api/invoices/by-appointment/{appointmentId}` → `AppointmentInvoicesDto`
   (danh sách + `TotalBilled`/`TotalPaid`/`TotalOutstanding` tính phía server; HĐ `Cancelled` không
   tính vào billed). Lọc `GET /api/invoices?appointmentId=` cũng khả dụng.

**Hệ quả.** RBAC `ManageBilling` giữ nguyên (Admin + Lễ tân). Sprint 15 (CLS-04): phí cận lâm sàng
lập **hoá đơn riêng** loại `Paraclinical` theo mô hình này (không gộp), chống trùng bằng cờ trên
`LabOrder`/`LabOrderItem`. **Còn nợ** (như P1): thanh toán một phần/refund/phiếu thu tổng của lượt,
BHYT/HĐĐT. **Đã kiểm chứng:** migration `RelaxInvoiceModelAndMedicationInvoiced` áp sạch trên
pgvector (bỏ unique `EncounterId`, thêm `AppointmentId` + `MedicationInvoicedAt`); 140 unit test xanh
(+7); FE build + oxlint sạch.
