# ADR 0022 — Hoàn tiền & Hoàn kho (Refund & Return)

**Ngày:** 2026-09-12  
**Trạng thái:** Chấp nhận  
**Sprint:** 22  

---

## Bối cảnh

Sprint 21 (ADR 0021) đã siết **thu-trước-làm-sau** một chiều: bệnh nhân phải thanh toán trước khi kỹ
thuật viên nhập kết quả CLS / Dược sĩ xuất kho thuốc. Còn thiếu chiều ngược: **huỷ/hoàn** sau khi đã
thu tiền hoặc đã cấp phát.

Hiện trạng trước Sprint 22:
- `Invoice.Cancel()` chỉ cho phép huỷ khi còn `Draft`; hoá đơn `Paid` không có đường hoàn.
- Đơn thuốc `Dispensed` không có cơ chế nhập lại tồn.
- Phiếu CLS `Paid` (chưa có kết quả) không thể huỷ được nếu đã lập hoá đơn.

---

## Quyết định

### REF-01 · Hoàn tiền hoá đơn `Paid`

- Thêm `InvoiceStatus.Refunded` (giá trị 3, lưu chuỗi).
- Thêm cột `RefundedAt` (timestamp) và `RefundReason` (varchar 500) vào bảng `invoices`.
- Phương thức `Invoice.Refund(reason, when)`: chỉ hợp lệ từ `Paid`; mọi trạng thái khác → 409.
- Endpoint `POST /api/invoices/{id}/refund` body `{ reason }` (role `ManageBilling`).
- **Sổ cái bất biến**: không xoá/sửa hoá đơn cũ — chỉ chuyển trạng thái + ghi lý do/thời điểm.
- Báo cáo doanh thu (ADR 0020) lọc `Status == Paid` → tự nhiên loại trừ `Refunded`.

### REF-02 · Hoàn kho đơn thuốc đã `Dispensed`

- Thêm `DispenseStatus.Returned` (giá trị 4, lưu chuỗi).
- Thêm `StockTransactionType.Return` (lưu chuỗi).
- Phương thức `Encounter.MarkReturned()`: chỉ hợp lệ từ `Dispensed`; chặn hoàn hai lần.
- Endpoint `POST /api/encounters/{id}/return-stock` (role `ManagePharmacy`).
- **Hoàn đúng lô**: service truy `StockTransaction` loại `Dispense` theo `EncounterId` → với mỗi giao
  dịch, gọi `batch.Increase(abs(quantityDelta))` và ghi giao dịch bù `Return` dương. Sổ cái bất biến —
  không xoá `Dispense` cũ, chỉ thêm `Return` mới.
- **Chính sách hoàn lô**: hoàn về đúng lô đã xuất (không FEFO ngược). Lô có thể đã hết hạn giữa chừng
  — tồn vẫn được cộng lại; hệ thống sẽ không cấp phát lô hết hạn ở lần tiếp theo (FEFO lọc
  `ExpiryDate >= today`).

### REF-03 · Huỷ phiếu CLS đã thu (chưa có kết quả)

- `LabOrderService.CancelAsync` mở rộng:
  - Nếu `order.IsPaid && order.Status == InProgress` → 409 `Paraclinical.HasPartialResults`.
  - Nếu `order.IsPaid && order.Status == Ordered` → cho phép: huỷ phiếu + tìm hoá đơn CLS
    (`Invoice.LabOrderId == order.Id && Status == Paid`) → hoàn tiền tự động (nối REF-01).
- Không thêm endpoint mới — dùng lại `DELETE`/`Cancel` hiện có (route `POST /api/lab-orders/{id}/cancel`).

---

## Hệ quả

| Khía cạnh | Tác động |
|-----------|---------|
| Migration | Thêm `RefundedAt`, `RefundReason` vào `invoices` (nullable, không breaking). |
| Enum mới | `InvoiceStatus.Refunded`, `DispenseStatus.Returned`, `StockTransactionType.Return` — lưu chuỗi, tự động tương thích. |
| Báo cáo | Doanh thu chỉ tính `Status == Paid`; `Refunded` không bị đếm (hành vi cũ đúng sẵn). |
| Sổ cái | Bất biến hoàn toàn: hoàn tiền = thêm trường trên HĐ; hoàn kho = thêm giao dịch `Return`. |
| RBAC | Hoàn tiền: `ManageBilling`; Hoàn kho: `ManagePharmacy`. |
| Ngoài phạm vi | Hoàn một phần; stock-take; phê duyệt nhiều cấp. |
