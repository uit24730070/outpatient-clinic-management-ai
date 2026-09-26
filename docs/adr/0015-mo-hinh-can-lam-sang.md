# 0015. Mô hình cận lâm sàng (chỉ định & kết quả)

- Trạng thái: Accepted
- Ngày: 2026-08-23
- Liên quan: [ADR 0005](0005-mo-hinh-lich-kham-va-may-trang-thai.md) (máy trạng thái + snapshot), [ADR 0006](0006-mo-hinh-benh-an-encounter-prescription.md) (owned collection), [ADR 0014](0014-mo-hinh-vien-phi-va-thu-ngan.md) (viện phí — Mô hình A nhiều hoá đơn/lượt)

## Bối cảnh

Khám ngoại trú thiếu mắt xích **cận lâm sàng (CLS)**: trong lúc khám, bác sĩ cần **chỉ định**
xét nghiệm/chẩn đoán hình ảnh, kỹ thuật viên/bác sĩ **nhập kết quả**, bác sĩ **xem kết quả** để
hoàn tất chẩn đoán, và **phí CLS phải vào hoá đơn**. Đây là Epic 11. Không xử lý BHYT, không tích
hợp máy xét nghiệm/PACS ở phạm vi này.

## Quyết định

1. **Danh mục dịch vụ CLS = `ServicePrice` có phân loại `Category`** (enum chuỗi
   `Consultation`/`Paraclinical`/`Other`, mặc định `Other` cho dữ liệu Sprint 14 — tham số ctor/
   `UpdateDetails` mặc định để không phá CRUD cũ). **Không** tạo bảng giá thứ hai. Dịch vụ loại
   `Paraclinical` mới được chỉ định trong phiếu CLS.

2. **Phiếu chỉ định `LabOrder`** là aggregate root có owned collection `LabOrderItem` (cùng khuôn
   `InvoiceItem`/`PrescriptionItem`): mã `CLS-` (đếm `IgnoreQueryFilters()`), gắn phiếu khám nguồn
   (`EncounterId`, FK Restrict), snapshot `PatientId`/`DoctorId`. Mỗi mục tham chiếu một `ServicePrice`
   (Paraclinical) + **snapshot** `ServiceName`/`UnitPrice` (đổi bảng giá sau không ảnh hưởng phiếu cũ).
   Chỉ tạo khi phiếu khám còn `Draft` (`Paraclinical.EncounterNotDraft` 409); dịch vụ không phải
   Paraclinical → 400 `Paraclinical.ServiceNotParaclinical`.

3. **`LabOrderItem` có khoá riêng `Guid Id`** (thay shadow int) để **địa chỉ hoá khi nhập kết quả**
   từng mục qua API (`/items/{itemId}/result`). Trạng thái mục `Pending`/`Completed`.

4. **Máy trạng thái phiếu `LabOrderStatus`** (`Ordered`→`InProgress`→`Completed`/`Cancelled`) đặt ở
   Domain (`SetItemResult`/`Cancel` trả `Result`). Nhập kết quả mục đầu → `InProgress`; đủ mọi mục có
   kết quả → `Completed`. Sửa/nhập sau khi `Completed`/`Cancelled` → 409 `Paraclinical.InvalidTransition`.

5. **Phí CLS lập hoá đơn RIÊNG loại `Paraclinical`** (theo Mô hình A của [ADR 0014] P2 — không gộp
   vào HĐ thuốc/công khám). Thêm `InvoiceItemType.Paraclinical` (chèn **trước** `Other` → `Other`
   đổi giá trị số 2→3; enum lưu chuỗi nên DB an toàn, FE const-map cập nhật theo).
   `InvoiceService.CreateFromLabOrderAsync` dựng dòng từ snapshot của phiếu chỉ định, suy `AppointmentId`
   từ phiếu khám nguồn để gom theo lượt. **Chống lập trùng bằng cờ `LabOrder.InvoicedAt`**
   (`MarkInvoiced` đặt một lần → 409 `Billing.ParaclinicalAlreadyInvoiced`) — như `Encounter.MedicationInvoicedAt`,
   không dựa lá chắn unique. Dòng hoá đơn dịch vụ (`CreateAsync`) tự **map loại dòng theo `Category`**
   (Paraclinical→`Paraclinical`, còn lại→`ServiceFee`) nên **ca chỉ-CLS lúc tiếp đón** (không encounter)
   cũng ra dòng đúng loại.

6. **RBAC:** chỉ định/nhập kết quả/huỷ = `Roles.RecordEncounter` (**Admin + Bác sĩ**); đọc mở cho mọi
   vai trò lâm sàng (đã đăng nhập). Lập hoá đơn phí CLS = `Roles.ManageBilling` (**Admin + Lễ tân**) —
   tách bạch khâu khám và khâu thu ngân.

7. **Màn khám đa tab (CLS-06, thuần FE):** thân màn khám tách thành component `EncounterForm` nhận
   `appointmentId` qua prop (route cũ `/appointments/:id/encounter` giữ nguyên qua vỏ bọc). "Phòng khám
   của tôi" mở song song nhiều phiếu bằng shadcn `Tabs` **keep-mounted** (`forceMount` + ẩn CSS). Nháp
   phiếu khám đã persist server-side nên đóng/mở lại tab (kể cả F5) nạp đúng qua `getEncounterByAppointment`
   — **không cần global store/localStorage**. Backend ~0 (nhiều `InProgress` đồng thời không bị chặn).

## Hệ quả

- **Tích cực:** tái dùng bảng giá + luồng hoá đơn Mô hình A; snapshot giá nhất quán; chống tính phí
  trùng bằng cờ trên nguồn (đồng nhất S14.5); màn đa tab không thêm state phức tạp.
- **Đánh đổi / nợ:** chưa **đính kèm file kết quả** (PDF/ảnh DICOM) hay tích hợp máy XN/PACS; chưa
  **khoảng tham chiếu/cờ bất thường** tự động; chưa có **vai trò kỹ thuật viên** riêng (dùng
  `RecordEncounter` tạm). `LabOrderItem.ServicePriceId` không FK cứng ràng buộc — không chặn xoá mềm
  dịch vụ đã chỉ định (giữ snapshot). Chỉ tạo phiếu CLS khi encounter `Draft` (chỉ định trong lúc khám).
- **`InvoiceItemType.Other` đổi giá trị số** (2→3): mọi nơi đọc số phải đồng bộ (đã cập nhật FE const-map).
