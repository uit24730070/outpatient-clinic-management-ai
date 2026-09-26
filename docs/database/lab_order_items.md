# Bảng `lab_order_items` — Từ điển dữ liệu

Mục của một phiếu chỉ định cận lâm sàng (một dịch vụ CLS). **Owned collection** của `LabOrder`
(cùng khuôn `invoice_items`/`prescription_items`), migration `AddParaclinical`.
Xem [ADR 0015](../adr/0015-mo-hinh-can-lam-sang.md).

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính **riêng** (không phải shadow int) — để địa chỉ hoá mục khi nhập kết quả. Sinh phía ứng dụng. |
| `LabOrderId` | `uuid` | Không | FK → `lab_orders.Id` (Cascade). Owner. Có index. |
| `ServicePriceId` | `uuid` | Không | Tham chiếu bảng giá (chỉ tra soát, **không** FK cứng). Có index. |
| `ServiceName` | `varchar(200)` | Không | Tên dịch vụ — **snapshot** lúc chỉ định. |
| `UnitPrice` | `numeric(18,2)` | Không | Đơn giá (VND) — **snapshot** lúc chỉ định. |
| `ResultText` | `varchar(4000)` | Có | Kết quả (văn bản tự do; null khi chưa có). |
| `Conclusion` | `varchar(1000)` | Có | Kết luận/nhận định (tuỳ chọn). |
| `Status` | `varchar(20)` | Không | `Pending`/`Completed` (chuỗi). |
| `ResultedAt` | `timestamptz` | Có | Thời điểm nhập kết quả (null khi chưa có). |

### Index & khoá
- `PK_lab_order_items` (`Id`); `IX_lab_order_items_LabOrderId`; `IX_lab_order_items_ServicePriceId`.

## Quy tắc nghiệp vụ
- Không có soft delete riêng — vòng đời gắn chặt phiếu chỉ định (`LabOrder`).
- Nhập kết quả qua `POST /api/lab-orders/{id}/items/{itemId}/result` (đặt `ResultedAt`, chuyển `Status`→`Completed`);
  method Domain `LabOrder.SetItemResult` đồng thời cập nhật trạng thái phiếu.
- `UnitPrice`/`ServiceName` là snapshot — đổi bảng giá sau không ảnh hưởng phiếu cũ.
- `ServicePriceId` **không** FK cứng → không chặn xoá mềm dịch vụ đã chỉ định (giữ snapshot, như `MedicationId` ở đơn thuốc).
