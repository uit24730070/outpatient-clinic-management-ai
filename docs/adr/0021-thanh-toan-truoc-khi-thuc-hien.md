# 0021. Thanh toán trước khi thực hiện (payment-before-execution)

- Trạng thái: Accepted
- Ngày: 2026-09-12

## Bối cảnh
Đến hết Sprint 20, hai điểm còn **sai nghiệp vụ** phòng khám dịch vụ (bệnh nhân tự trả tiền, không BHYT):
1. **Cấp phát thuốc** trừ tồn kho **ngay khi chốt phiếu khám** (`EncounterService.CompleteAsync` → `DispenseAsync`, ADR 0011) — trước khi thu tiền.
2. **Kết quả cận lâm sàng** nhập được **bất kể đã thu phí CLS hay chưa** (`LabOrderService.SetItemResultAsync`).

Đây là **nợ có chủ đích** tách từ Sprint 16 (ADR 0016) và mục "Nợ đề cương" (GĐ6). Sprint 21 siết đúng thứ tự: **thu tiền trước, thực hiện sau**. Cần chốt: (1) mô hình trạng thái cấp phát; (2) mô hình tồn (giữ chỗ vs xuất thực); (3) liên kết hoá đơn↔nguồn để biết "đã thu"; (4) RBAC; (5) tương thích dữ liệu cũ.

## Quyết định

### 1. Vòng đời cấp phát thuốc ở cấp Encounter (PAY-02)
- Thêm enum `DispenseStatus { None, Reserved, Paid, Dispensed }` + `Encounter.DispenseStatus`/`ReservedAt`/`MedicationPaidAt` (giữ `DispensedAt`). **Đặt ở cấp phiếu khám** (không trên owned entity `PrescriptionItem`) — khớp thực tế: 1 hoá đơn thuốc/phiếu (`Encounter.MedicationInvoicedAt`, ADR 0014 P2), cấp phát all-or-nothing.
- Máy trạng thái Domain là nguồn sự thật:
  - **Chốt phiếu** (có ≥1 dòng gắn danh mục) → `MarkReserved` (None → Reserved). Phiếu không có thuốc gắn danh mục giữ `None`.
  - **Thu hoá đơn thuốc** → `MarkMedicationPaid` (Reserved → Paid; trạng thái khác bỏ qua yên lặng — idempotent).
  - **Cấp phát thực** (Dược sĩ) → `MarkDispensed` (Paid → Dispensed). Chưa thu (Reserved) → `Pharmacy.NotPaid`; None/Dispensed → 409.
- Endpoint mới `POST /api/encounters/{id}/dispense` chạy trừ tồn FEFO + ghi sổ cái `Dispense` (logic cũ chuyển nguyên từ `CompleteAsync` sang), giữ `xmin` concurrency.

### 2. Mô hình tồn: giữ chỗ theo "tồn khả dụng", xuất thực khi Dispense
- **Reserved/Paid không trừ `MedicationBatch.QuantityOnHand`** (tồn vật lý) — chỉ trừ **tồn khả dụng** khi kiểm lúc chốt phiếu:
  `khả dụng = Σ(tồn lô còn hạn) − Σ(số lượng đang Reserved/Paid chưa Dispensed)`.
  Thiếu khả dụng → `Pharmacy.InsufficientStock` (rollback, giữ mã cũ). Số "đang giữ chỗ" truy vấn qua owned collection (`Encounters … SelectMany(PrescriptionItems)`).
- **Chỉ Dispensed** mới trừ `QuantityOnHand` thật + ghi `StockTransaction`. FEFO **chuyển thời điểm chọn lô** từ lúc chốt phiếu sang lúc cấp phát (chọn lô còn hạn gần nhất tại thời điểm xuất).

### 3. Gating kết quả CLS bằng cờ trên nguồn (PAY-01)
- Thêm `LabOrder.PaidAt` (+ `MarkPaid`, idempotent) và `Invoice.LabOrderId` (không FK cứng, như `EncounterId`). Lập hoá đơn phí CLS gắn `LabOrderId = order.Id`.
- **Móc thu tiền → mở cổng** (`InvoiceService.MarkSourcesPaidAsync`, gọi trong `PayAsync`/`PayVisitAsync`): khi thu một hoá đơn, nếu có `LabOrderId` → `LabOrder.MarkPaid`; nếu có `EncounterId` **và** có dòng `Medication` → `Encounter.MarkMedicationPaid`. Cùng một `SaveChanges`.
- `SetItemResultAsync` chặn khi `LabOrder.PaidAt is null` → `Paraclinical.NotPaid` (409). Áp cho cả walk-in.

### 4. RBAC
- **Chốt phiếu** (`POST …/complete`) đổi từ `DispenseEncounter` → `RecordEncounter` (Admin/Bác sĩ) — chốt phiếu chỉ giữ tồn, là việc bác sĩ; không còn đụng kho.
- **Cấp phát thực** (`POST …/dispense`) = `ManagePharmacy` (Admin/Dược sĩ) — đúng người ở quầy phát thuốc (ADR 0013).
- Thu tiền vẫn `ManageBilling` (Admin/Lễ tân). Ba vai trò khép vòng: Bác sĩ chốt (Reserved) → Lễ tân thu (Paid) → Dược sĩ phát (Dispensed).

### 5. Tương thích dữ liệu cũ (migration `PaymentBeforeExecution`)
- Cột thêm là additive; `DispenseStatus` mặc định `'None'`. Backfill (quote PascalCase — §8 ghi chú dự án):
  - `encounters`: `DispensedAt IS NOT NULL` → `'Dispensed'` (đơn đã cấp phát cũ coi như đã xuất).
  - `lab_orders`: `PaidAt = InvoicedAt` khi `InvoicedAt IS NOT NULL` (phiếu đã lập HĐ cũ coi như đã thu — không hồi quy phiếu đang dở).
  - `invoices`: `LabOrderId` để null (HĐ cũ không map ngược được — chấp nhận, gating chỉ dựa `LabOrder.PaidAt`).

## Hệ quả
- **Ưu:** đúng nghiệp vụ thu-trước-làm-sau; tách rõ tồn khả dụng vs tồn vật lý; máy trạng thái Domain tường minh; RBAC khép vòng ba vai trò; không hồi quy dữ liệu/hoá đơn cũ.
- **Nhược/đánh đổi:**
  - Thêm một bước thao tác (Dược sĩ cấp phát) — đúng quy trình nhưng nhiều click hơn luồng gộp cũ.
  - Trạng thái ở cấp Encounter (không per-line) → chưa hỗ trợ thu/cấp phát **từng dòng** (ngoài phạm vi; đủ cho 1 HĐ thuốc/phiếu).
  - Tồn khả dụng tính bằng truy vấn tổng hợp mỗi lần chốt phiếu (không cache) — đủ nhanh ở quy mô đồ án.
- **Ngoài phạm vi (nợ):** hoàn tiền/huỷ sau `Paid`/`Dispensed` (reversal `StockTransaction`); đặt cọc/thu một phần; nhắc Dược sĩ real-time đơn `Paid` chờ phát.
