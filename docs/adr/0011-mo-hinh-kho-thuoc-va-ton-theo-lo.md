# 0011. Mô hình kho thuốc & tồn theo lô/hạn dùng + sổ cái giao dịch

- Trạng thái: Accepted
- Ngày: 2026-08-22

## Bối cảnh
Giảng viên yêu cầu bổ sung **nghiệp vụ kho dược** (Epic 8, ngoài phạm vi ban đầu): quản lý thuốc **đầy đủ** — lô + hạn dùng + nhập/xuất, và **trừ tồn khi kê đơn**. Hiện `PrescriptionItem` (Sprint 5, [ADR 0006](0006-mo-hinh-benh-an-encounter-prescription.md)) chỉ là **văn bản tự do**, không nối vào tồn kho.

Yêu cầu chạm cả **module kho mới** lẫn **hồi quy bệnh án Sprint 5**. Để giảm rủi ro, chia làm hai sprint: **P1 (Sprint 11)** dựng nền dữ liệu + nhập kho (không sửa code cũ); **P2 (Sprint 12)** nối xuất/cấp phát vào `PrescriptionItem`. ADR này chốt mô hình dữ liệu cho cả hai, hiện thực dần.

## Các quyết định

### 1. Tồn kho theo **lô + hạn dùng** (batch/expiry), không chỉ một con số tồn
- Thuốc (`Medication`) là **danh mục** (mã `TH-`, hoạt chất, đơn vị văn bản tự do, ngưỡng tồn tối thiểu `ReorderLevel`). Tồn **không** lưu trên thuốc.
- Tồn thực nằm ở **lô** (`MedicationBatch`: `MedicationId` + `BatchNumber` + `ExpiryDate` + `QuantityOnHand`). **Tồn tổng của thuốc = `SUM(QuantityOnHand)` các lô chưa xoá**, tính **phía server** (subquery `Sum`), không nạp hết về bộ nhớ.
- Lý do: dược phẩm phải truy vết **hạn dùng** để cảnh báo hết hạn và **cấp phát FEFO** (first-expired-first-out) ở P2. Một con số tồn duy nhất không đủ.

### 2. Nhập kho là **aggregate cha–con bất biến** (mirror Encounter/PrescriptionItem)
- `StockReceipt` (mã `PN-`) + owned collection `StockReceiptItem` — cùng khuôn owned collection như `PrescriptionItem` ([ADR 0006](0006-mo-hinh-benh-an-encounter-prescription.md)).
- **Tạo phiếu nhập ⇒** với mỗi dòng: tìm lô khớp (`MedicationId`+`BatchNumber`+`ExpiryDate`) → **cộng** `QuantityOnHand` qua method Domain `MedicationBatch.Increase`, hoặc **tạo lô mới**; đồng thời ghi một `StockTransaction`. Các dòng trùng khoá lô trong cùng phiếu **cộng dồn** vào một lô (gom qua cache theo khoá). Tất cả trong **một `SaveChanges`**.
- Phiếu nhập **bất biến sau khi tạo** ở P1 (không sửa/xoá) — tránh phải hoàn tác tồn. Sửa/huỷ có hoàn tác để sau.

### 3. **Sổ cái giao dịch tồn** (`StockTransaction`) — nguồn sự thật truy vết
- Mọi thay đổi tồn để lại một dòng bất biến: `MedicationBatchId`, `Type` (`Import`/`Dispense`/`Adjust`, lưu **chuỗi** `HasConversion<string>()`), `QuantityDelta` (dương=nhập, âm=xuất), `ReferenceType`/`ReferenceId` (trỏ chứng từ nguồn, ví dụ `StockReceipt`), `OccurredAt`.
- P1 chỉ ghi `Import`. Đây là **nền cho báo cáo xuất–nhập–tồn** và cho cấp phát `Dispense` ở P2. Không dùng SQL thô — ghi qua EF trong luồng service.

### 4. RBAC: gộp `ManagePharmacy` = **Admin + Lễ tân**
> ⚠️ **Đã thay bởi [ADR 0013](0013-vai-tro-duoc-si-quan-ly-kho-thuoc.md):** thêm vai trò **Dược sĩ** →
> `ManagePharmacy = Admin + Dược sĩ` và `DispenseEncounter = Admin + Bác sĩ + Dược sĩ` (Lễ tân không còn quản lý kho). Phần dưới ghi nguyên trạng lúc Sprint 11–12.

- Phòng khám nhỏ **chưa có vai trò "Dược sĩ" riêng** → `Roles.ManagePharmacy` mirror `ManageStaff` (Admin + Lễ tân) cho **ghi** (danh mục, nhập kho, xem sổ cái). **Đọc danh mục thuốc mở cho mọi vai trò** (bác sĩ cần tra khi kê đơn ở P2).
- Guard FE chỉ là UX; backend vẫn chốt 403 (mirror [ADR 0009](0009-phan-quyen-va-trai-nghiem-giao-dien-theo-vai-tro.md)).

### 5. `DateOnly` cho hạn dùng; `PrescriptionItem` **giữ nguyên** ở P1
- Hạn dùng chỉ cần **ngày** → `DateOnly` ↔ Postgres `date` (Npgsql hỗ trợ; EF InMemory cũng chạy). Không dùng `timestamptz`.
- `PrescriptionItem` **không đổi** ở P1 (tránh migration/hồi quy bệnh án). P2 mới thêm `MedicationId?` **nullable** (tương thích ngược) + luồng cấp phát.

## Hệ quả
- **Tích cực:** nền dữ liệu đủ để cấp phát FEFO ở P2 (chỉ việc trừ `QuantityOnHand` theo `ExpiryDate` tăng dần + ghi `Dispense`); sổ cái cho báo cáo & đối soát; P1 **không đụng code cũ** nên rủi ro hồi quy thấp; bám sát pattern vertical slice + owned collection sẵn có.
- **Đánh đổi / rủi ro đồng thời:** cộng tồn qua method Domain trong một `SaveChanges` là đủ cho nhập (ít tranh chấp). **Cấp phát ở P2 mới là chỗ cần cẩn trọng đồng thời** — dự kiến đặt **concurrency token** (`xmin`/rowversion) trên `MedicationBatch` để chặn hai lần trừ tồn chồng nhau; hiện thực khi làm trừ tồn. Chống trùng lô hiện kiểm ở service (có race như sinh mã `BN-`/`BS-`); có thể siết bằng unique index `(MedicationId, BatchNumber, ExpiryDate)` sau.
- **Còn nợ (P2 — Sprint 12):** `PrescriptionItem.MedicationId?`, cấp phát trừ tồn FEFO + sổ cái `Dispense`, cảnh báo tồn thấp/sắp hết hạn (dùng `ReorderLevel` + `ExpiryDate` đã có), điều chỉnh tồn thủ công (`Adjust`), nhà cung cấp thành entity riêng.

## Bổ sung P2 (Sprint 12) — cấp phát FEFO, concurrency & cảnh báo — *Accepted 2026-08-22*

### 6. Đơn thuốc **tuỳ chọn** gắn danh mục (`PrescriptionItem.MedicationId?`)
- Thêm cột `MedicationId` **nullable** trên owned collection `prescription_items` — **không FK cứng** (giữ dòng đơn cũ nguyên vẹn, tránh ràng buộc chặn xoá mềm thuốc). Có index để truy vấn.
- `DrugName` **luôn** là văn bản hiển thị (điền từ danh mục khi chọn, vẫn sửa tay được). `MedicationId = null` ⇒ thuốc ngoài danh mục, **không** trừ tồn. Kiểm thuốc tồn tại ở service khi có `MedicationId` (`Pharmacy.MedicationNotFound` 404). Tương thích ngược hoàn toàn — luồng `ReplaceItems` (thay cả cụm) không đổi.

### 7. **Cấp phát gộp vào chốt phiếu** (`Encounter.Complete`), FEFO, bỏ qua lô hết hạn
- **Thời điểm:** cấp phát chạy **tự động trong `EncounterService.CompleteAsync`** (không endpoint riêng). Chốt phiếu ⇒ khép lịch + cấp phát trong **cùng một `SaveChanges`** (nguyên tử). Đánh dấu `Encounter.DispensedAt` chống cấp phát trùng.
- **FEFO:** với mỗi thuốc có `MedicationId`, gom tổng số lượng, lấy các lô **còn hạn** (`ExpiryDate ≥ hôm nay UTC`), **hạn tăng dần**, trừ dần qua `MedicationBatch.Decrease` + ghi `StockTransaction` `Dispense` (`QuantityDelta` **âm**, `ReferenceType="Encounter"`). **Bỏ qua lô đã hết hạn** (an toàn lâm sàng — không phát thuốc quá hạn).
- **Không cấp phát một phần:** thiếu tồn còn hạn ⇒ `Pharmacy.InsufficientStock` (409), **rollback toàn bộ** (chưa `SaveChanges` nên không ghi gì). Hệ quả: **không chốt được phiếu khi thiếu tồn** — chấp nhận đánh đổi này vì cấp phát gộp vào chốt (phòng khám nhỏ, lễ tân/quầy dược xử lý cùng lúc).

### 8. Concurrency token `xmin` chống double-dispense
- `MedicationBatch` map **cột hệ thống `xmin` của Postgres** làm row-version (`UseXminAsConcurrencyToken`). Hai lượt cấp phát chồng nhau lên cùng lô ⇒ `DbUpdateConcurrencyException` ⇒ service trả `Pharmacy.ConcurrencyConflict` (409), client thử lại.
- **Lưu ý migration:** `xmin` là cột hệ thống — migration sinh `AddColumn xmin` phải **xoá thủ công** (đã làm trong `AddPrescriptionMedicationLink`), nếu không `dotnet ef database update` sẽ lỗi. `UseXminAsConcurrencyToken` bị đánh *obsolete* ở Npgsql 8 nhưng vẫn dùng (ức chế cảnh báo cục bộ) vì các cách manual có rủi ro sinh cột. InMemory không mô phỏng được xmin → cấp phát/FEFO test bằng unit test, double-dispense cần integration test.

### 9. RBAC chốt phiếu (kèm cấp phát) mở cho **cả ba vai trò**
- Vì cấp phát gộp vào bước chốt, endpoint `POST /api/encounters/{id}/complete` dùng nhóm mới `Roles.DispenseEncounter` = **Admin + Lễ tân + Bác sĩ** (nới so với `RecordEncounter` chỉ chi phối tạo/sửa phiếu). Cho phép quầy dược/lễ tân chốt+phát thuốc. **Đọc danh mục** mở mọi vai trò (bác sĩ tra khi kê đơn).

### 10. Cảnh báo kho (`GET /api/pharmacy/alerts?expiringInDays=30`)
- Tổng hợp **phía server**: tồn thấp (`SUM lô ≤ ReorderLevel`) + lô còn tồn có `ExpiryDate ≤ hôm nay + N` (bao gồm đã hết hạn, cờ `IsExpired`). RBAC `ManagePharmacy`. Không thay đổi tồn — chỉ đọc.
