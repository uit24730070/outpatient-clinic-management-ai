# ADR 0025 — Kết quả có cấu trúc cho nhóm Xét nghiệm (giao diện Kỹ thuật viên theo loại CLS)

**Ngày:** 2026-09-26
**Trạng thái:** Chấp nhận
**Sprint:** 25 (ngoài kế hoạch — polish sau bảo vệ)
**Liên quan:** [ADR 0015](0015-mo-hinh-can-lam-sang.md) (mô hình CLS), [ADR 0024](0024-nhom-hien-thi-can-lam-sang.md) (nhóm CLS)

---

## Bối cảnh

Sau ADR 0024 (gom nhóm khi **chỉ định**), phản hồi tiếp theo: màn "Thực hiện CLS" của Kỹ thuật viên
vẫn sơ sài — mọi mục chỉ định, bất kể Xét nghiệm/Chẩn đoán hình ảnh/Thăm dò chức năng/Nội soi, đều
dùng chung đúng 2 ô văn bản tự do (`ResultText`/`Conclusion`, `LabItemRow` trong `LabOrderPanel.tsx`).
Với Xét nghiệm — bản chất là các chỉ số so sánh được với khoảng tham chiếu (VD "Bạch cầu 12.5,
tham chiếu 4.0-10.0") — văn bản tự do không cho thấy ngay chỉ số nào bất thường, khác hẳn Chẩn đoán
hình ảnh/Nội soi vốn là diễn giải mô tả, không có "khoảng tham chiếu" để so.

## Quyết định

1. **Thêm `LabResultParameter`** (owned entity, lồng thêm một tầng owned bên trong
   `LabOrderItem.Parameters` — EF Core 8 hỗ trợ owned collection lồng nhau): `Name`, `Value` (văn bản,
   chứa được cả kết quả định tính như "Âm tính"), `Unit`, `ReferenceRange`, `IsAbnormal`. `IsAbnormal`
   **tự tính lúc tạo** (`LabResultParameter.Create`) khi cả `Value` và `ReferenceRange` đọc được dạng
   số "thấp-cao" (`decimal.TryParse` theo `-`); đọc không được (định tính) thì giữ `false` — hệ thống
   không đoán, kỹ thuật viên tự đọc bằng mắt qua cột Khoảng tham chiếu.
2. **Không tạo danh mục "chỉ số chuẩn theo từng dịch vụ xét nghiệm"** (cân nhắc nhưng bỏ — cần một
   entity + CRUD admin riêng để khai "Xét nghiệm công thức máu gồm WBC/RBC/Hb/..." cho từng
   `ServicePrice`, việc mô hình hoá dữ liệu y khoa thật sự, rủi ro/khối lượng không tương xứng ở giai
   đoạn polish sau bảo vệ). Kỹ thuật viên **tự gõ tên thông số** mỗi lần nhập (đã biết nghiệp vụ) —
   hệ thống chỉ cho **cấu trúc hoá** việc nhập (từng dòng riêng, so sánh được) thay vì cho **gợi ý sẵn**.
3. **`SetLabResultRequest` mở rộng thêm `Parameters`** (không đổi route) — `ResultText`/`Conclusion`
   và `Parameters` **không loại trừ nhau**, cùng tồn tại trên một `LabOrderItem`; FE quyết định gửi
   cái nào theo nhóm dịch vụ. Gọi lại `SetItemResult` **thay toàn bộ** `Parameters` (như
   `LabOrder.ReplaceItems`) — không cộng dồn qua các lần sửa.
4. **`LabOrderItemDto` thêm `Group`** — tra theo `ServicePriceId` **tại thời điểm đọc** (KHÔNG
   snapshot, khác `ServiceName`/`UnitPrice` vốn phải đóng băng vì ảnh hưởng tiền) — đổi nhóm dịch vụ
   sau vẫn phản ánh đúng ở các phiếu cũ; chỉ để FE chọn giao diện nhập, không phải dữ liệu tài chính.
5. **`LabItemRow` (FE) tách nhánh theo `item.group`:** `LabTest` → bảng thông số động (thêm/xoá dòng,
   4 cột: Thông số/Giá trị/Đơn vị/Khoảng tham chiếu) thay `ResultText`; các nhóm còn lại → giữ ô
   `Textarea` mô tả tự do (đổi từ `Input` một dòng — mô tả hình ảnh/nội soi thường dài) với placeholder
   gợi ý theo nhóm. Xem read-only bảng thông số **tô đỏ + icon cảnh báo** dòng `IsAbnormal`.
6. **Phiếu in (`LabOrderPrintPage`)** hiển thị bảng thông số lồng trong ô "Kết quả" khi có, đánh dấu
   giá trị bất thường bằng dấu `*` in đậm + chú thích cuối trang (bản in đen trắng, không dùng màu).

---

## Hệ quả

| Khía cạnh | Tác động |
|-----------|---------|
| Migration | `AddLabResultParameters`: bảng mới `lab_result_parameters` (khoá ẩn `Id` identity, FK `LabOrderItemId` cascade) — không đổi bảng cũ, không breaking. |
| Test | `TestDbContext` (EF InMemory riêng cho unit test, không phụ thuộc Infrastructure) phải khai owned collection lồng y hệt `LabOrderConfiguration` — quên khai gây lỗi model-build ở **mọi** test dùng `TestDbContext.Encounters`/DbSet khác (không riêng Paraclinical), đã gặp và sửa khi làm ADR này. 2 test mới: cờ bất thường tính đúng khi so sánh được số; thay toàn bộ thông số qua các lần gọi `SetItemResultAsync`, không cộng dồn. |
| Backward compat | Mục CLS cũ (trước ADR này) không có `Parameters` → `Parameters` rỗng, vẫn đọc được qua `ResultText` như trước — không mất dữ liệu. |
| UI | Chỉ `LabItemRow`/`LabOrderPrintPage` đổi; `LabOrderExecutePage`/`TechnicianLabPage` (danh sách hàng chờ) không đổi vì dùng lại `LabOrderCard`. |
| Ngoài phạm vi | Danh mục chỉ số chuẩn theo dịch vụ (gợi ý sẵn tên thông số); khoảng tham chiếu theo giới tính/độ tuổi; đơn vị chuẩn hoá (không kiểm tra "mmol/L" vs "mg/dL" cùng thông số); đính kèm ảnh/PACS (đã ngoài phạm vi từ ADR 0015). |
