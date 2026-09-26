# 0016. Đăng ký dịch vụ tại lễ tân, Walk-in CLS & vai trò Kỹ thuật viên

- Trạng thái: Accepted
- Ngày: 2026-08-23
- Liên quan: [ADR 0005](0005-mo-hinh-lich-kham-va-may-trang-thai.md) (lịch khám + snapshot), [ADR 0013](0013-vai-tro-duoc-si-quan-ly-kho-thuoc.md) (mẫu thêm vai trò), [ADR 0014](0014-mo-hinh-vien-phi-va-thu-ngan.md) (viện phí — Mô hình A, lập HĐ lúc tiếp đón), [ADR 0015](0015-mo-hinh-can-lam-sang.md) (mô hình CLS: `LabOrder`/`from-lab-order`/cờ `InvoicedAt`)

## Bối cảnh

Theo đề cương đã chốt, quy trình khám ngoại trú **mở đầu bằng lễ tân đăng ký dịch vụ**:
*"Đăng ký dịch vụ → tạo hoá đơn → thanh toán → thực hiện dịch vụ → phát sinh chỉ định nếu có → … →
cấp phát thuốc"*. Hệ thống trước Sprint 16 thiếu khâu đầu: (1) lễ tân **chưa đăng ký được dịch vụ khám**
khi đặt lịch; (2) cận lâm sàng **chỉ do bác sĩ chỉ định trong lúc khám** — không có luồng **walk-in CLS**
(bệnh nhân yêu cầu CLS không qua bác sĩ); (3) **thiếu vai trò Kỹ thuật viên** (đề cương yêu cầu tường minh).
Đây là Epic 11 mở rộng + Epic 14.

## Quyết định

1. **Vai trò Kỹ thuật viên** — thêm `UserRole.Technician = 4` (lưu chuỗi, **không đổi schema**) + `Roles.Technician`
   + nhóm `Roles.RecordLabResult = Admin + Doctor + Technician`. Seed user demo `kythuatvien`/`Technician@123`
   (BCrypt workFactor 11 tất định, mẫu ADR 0013). Đồng bộ FE: `types/auth.ts` (label "Kỹ thuật viên"),
   `config/access.ts` (nav "Thực hiện CLS", landing `/lab/technician`, cờ `canRecordLabResult`),
   `StatusBadge` (badge indigo), router (`RECORD_LAB_RESULT`, Technician ∈ `ALL_ROLES`).

2. **Walk-in CLS = NỚI `LabOrder`, không dựng aggregate mới.** `LabOrder.EncounterId` và `DoctorId` →
   **nullable**; thêm `LabOrder.AppointmentId?` (gắn lượt tiếp đón nếu có, FK Restrict). Giữ **nguyên**
   ctor đường bác sĩ (encounter/doctor bắt buộc — Sprint 15) + thêm **factory `LabOrder.CreateWalkIn(code,
   patientId, appointmentId?, note, items)`** (encounter/doctor = null). `LabOrderService.CreateWalkInAsync`
   (RBAC lễ tân) kiểm bệnh nhân tồn tại (`Patient.NotFound` 404), lượt tồn tại nếu có (`Appointment.NotFound`
   404), dịch vụ phải loại `Paraclinical` (dùng chung `BuildItemsAsync` với đường encounter). Tái dùng
   nguyên cụm: in phiếu, nhập kết quả, `from-lab-order` → hoá đơn.

3. **`from-lab-order` đổi nguồn `AppointmentId`:** ưu tiên `LabOrder.AppointmentId` (walk-in), **fallback**
   suy từ phiếu khám nguồn (`Encounter.AppointmentId`) khi có encounter — giữ nguyên hành vi & kết quả
   test Sprint 15 (đường bác sĩ).

4. **Đăng ký dịch vụ khám gắn vào `Appointment`.** Thêm `Appointment.ServicePriceId?` + **snapshot**
   `ServiceName?`/`ServicePrice?` (dịch vụ loại `Consultation`), nullable + ctor tham số mặc định (tương
   thích Sprint 4). `AppointmentService` create/update nhận `servicePriceId?` → kiểm tồn tại + loại
   Consultation (`Appointment.ServiceNotConsultation` 400) rồi snapshot. Endpoint `GET /api/appointments/last?patientId=`
   trả lượt gần nhất để FE **prefill dịch vụ khi tái khám**. Lập HĐ tiếp đón **tái dùng** `POST /api/invoices`
   (dòng dịch vụ + `appointmentId`, BILL-07) — không thêm cơ chế mới.

5. **RBAC nhập kết quả CLS chuyển sang `RecordLabResult`** (`PUT /api/lab-orders/{id}/items/{itemId}/result`):
   Admin + Bác sĩ + Kỹ thuật viên. Chỉ định trong lúc khám vẫn `RecordEncounter` (Bác sĩ); đăng ký walk-in
   `ManageStaff` (Lễ tân); đọc mở cho mọi vai trò. Hàng chờ kỹ thuật viên = tái dùng lọc `?status=Ordered|InProgress`.

## Hệ quả

- **Tích cực:** khép khâu đầu quy trình (đăng ký dịch vụ + walk-in CLS) mà không dựng aggregate/bảng mới;
  tái dùng toàn bộ luồng CLS + hoá đơn Mô hình A; thêm vai trò theo đúng checklist ADR 0013 (chạm nhiều
  nơi nhưng cơ học); snapshot giá nhất quán.
- **Nợ có chủ đích (→ Epic 15 "Điều kiện thanh toán trước khi thực hiện"):**
  - **Gating thanh toán chưa siết** — sprint này lễ tân/kỹ thuật viên **có thể nhập kết quả CLS trước khi
    HĐ phí CLS `Paid`**; chưa có trạng thái "đủ điều kiện thực hiện". *Không phải bug* — Epic 15 sẽ thêm
    `Paraclinical.NotPaid`.
  - **Cấp phát thuốc theo trạng thái** vẫn "trừ tồn ngay khi chốt phiếu" (Sprint 12) — Epic 15 sẽ tách
    Reserved→Paid→Dispensed.
  - **Provider AI** vẫn Claude — nợ đổi sang OpenAI GPT-5 mini theo đề cương (nợ riêng, ngoài sprint).
- **Đánh đổi kỹ thuật:** `EncounterId`/`DoctorId` nay nullable ⇒ mọi nơi join tên bác sĩ / đọc encounter
  phải null-an-toàn (DTO `Project` dùng subquery đã an toàn; FE type nullable + bỏ qua walk-in khi gom
  theo bệnh án). Đăng ký (Appointment) và lập HĐ (Invoice) là **2 aggregate/2 lời gọi** — không giao dịch
  chung; lỗi tạo HĐ thì lịch vẫn còn (bấm "Lập HĐ" lại). Chống trùng lập HĐ walk-in vẫn dựa cờ `InvoicedAt`
  (không có lá chắn unique).
