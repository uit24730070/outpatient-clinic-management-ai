# Lịch sử sprint (S0–S21)

> File tham khảo, KHÔNG được nạp tự động mỗi session. Chỉ đọc khi cần tra lại
> quyết định/kiến trúc cũ (ví dụ: "vì sao cấp phát FEFO lại làm ở Sprint 12 kiểu này?").
> Sprint gần nhất (S22 trở đi) nằm trong ghi chú dự án mục 10.

- **S0** ✅ skeleton. **S1** ✅ Bệnh nhân CRUD đầu-cuối (7 test, migration `InitialCreate`).
- **S2** ✅ Chuyên khoa + Bác sĩ + soft delete toàn hệ thống + Serilog + `/health` (ADR 0003). *Sinh mã đếm `IgnoreQueryFilters()` tránh trùng sau soft delete.*
- **S3** ✅ JWT + RBAC, `User` (Admin/Receptionist/Doctor, `IsActive`), BCrypt, seed `admin` (ADR 0004).
- **S4** ✅ `Appointment` — máy trạng thái Domain, chống trùng giờ bác sĩ (ADR 0005).
- **S5** ✅ `Encounter` + owned `PrescriptionItem`, 1–1 Appointment, chốt phiếu→auto Complete lịch (ADR 0006).
- **S6** ✅ AI tóm tắt — `IChatCompletionService` (Application) + `OpenAiChatCompletionService` (Infrastructure), `Fake` tất định (ADR 0007).
- **S7** ✅ RAG — `IEmbeddingService` (Voyage AI) + pgvector `EncounterEmbedding` (1024 chiều), hỏi đáp có nguồn (ADR 0008).
- **S8** ✅ `User↔Doctor` (nullable unique FK), `doctorId` từ `/me` tra server-side, phân quyền FE `config/access.ts` (ADR 0009).
- **S9** ✅ Quản lý User (Admin CRUD + link Doctor) + Docker Compose (nginx reverse proxy, auto-migrate gated).
- **S10** ✅ Chatbot tool-calling — `IAssistantCompletionService`, orchestrator `MaxToolRounds=5`, 4 tool chỉ-đọc có RBAC (ADR 0010).
- **S11** ✅ Kho P1 — `Medication`/`MedicationBatch`/`StockReceipt`/`StockTransaction`, FEFO định hướng (ADR 0011).
- **S12** ✅ Kho P2 — cấp phát FEFO khi chốt phiếu (`xmin` concurrency), cảnh báo tồn thấp/hết hạn, `ManagePharmacy`=Admin+Dược sĩ (ADR 0011 P2 + ADR 0013).
- **S13** ✅ UI — TailwindCSS v4 + shadcn/ui + react-hook-form + zod toàn bộ 22 trang (ADR 0012).

### Sprint 14 ✅ Viện phí & Thu ngân (ADR 0014)
`ServicePrice` (mã `DV-`) + `Medication.SalePrice` + aggregate `Invoice`/owned `InvoiceItem` (snapshot giá, mã `HD-`). Máy trạng thái `Draft→Paid/Cancelled` ở Domain; `/pay` nhận `paymentMethod` chuỗi. Lập on-demand (`from-encounter` = dòng thuốc đã cấp + công khám). 133 test.

### Sprint 14.5 ✅ Viện phí P2 — nhiều HĐ/lượt (ADR 0014 P2)
Bỏ unique `EncounterId`, thêm `Invoice.AppointmentId?`. Chống trùng chuyển sang **cờ trên nguồn**: `Encounter.MedicationInvoicedAt` (đặt 1 lần). `from-encounter` chỉ còn dòng thuốc (công khám thu lúc tiếp đón). `GET /by-appointment/{id}` gom HĐ + tổng. 140 test.

### Sprint 15 ✅ Cận lâm sàng (ADR 0015)
`ServicePrice.Category` (Consultation/Paraclinical/Other). Aggregate `LabOrder` + owned `LabOrderItem` (Guid Id riêng để địa chỉ hoá). Chỉ tạo khi encounter `Draft`. Máy trạng thái `Ordered→InProgress→Completed/Cancelled`. `from-lab-order` chống trùng bằng `LabOrder.InvoicedAt`. `MyClinicPage` đa tab `forceMount`. *`InvoiceItemType.Other` đổi số 2→3 (FE const-map phải đồng bộ).* 155 test.

### Sprint 16 ✅ Walk-in CLS & Kỹ thuật viên (ADR 0016)
`UserRole.Technician`, `Roles.RecordLabResult` (Admin+Bác sĩ+Kỹ thuật viên). `LabOrder.EncounterId`/`DoctorId` → **nullable**, factory `CreateWalkIn`. `Appointment.ServicePriceId?` snapshot dịch vụ Consultation. RBAC nhập kết quả → `RecordLabResult`. *`EncounterId`/`DoctorId` nullable ⇒ mọi join phải null-an-toàn.* 165 test.

### Sprint 17 ✅ Lượt tiếp đón Visit (ADR 0017)
Aggregate `Visit` (mã `LK-`, `Open→Closed/Cancelled`) nằm trên `Appointment` (`Appointment.VisitId?` nullable). `VisitService.CreateAsync` tạo Visit + N Appointment + LabOrder walk-in trong **một `SaveChanges`**. Billing gắn lượt qua `Invoice.VisitId?` (suy từ lịch/LabOrder); `pay-visit` thu cả lượt một lượt. `LabOrder.VisitId?` gắn walk-in CLS vào lượt. 187 test.

### Sprint 18 ✅ Phòng khám & Lịch làm việc bác sĩ (ADR 0018)
`Room` (mã `PK-`) + `DoctorWorkSchedule` (mẫu tuần, `TimeOnly`, chống chồng khung). `AppointmentService` ràng buộc giờ khám ∈ khung làm việc hôm đó (UTC+7 hằng số `ClinicOffset`); bác sĩ chưa khai lịch → không ràng buộc. *Ràng buộc giờ **chưa áp** `VisitService` (walk-in bỏ qua có chủ đích).* 202 test.

### Sprint 19 ✅ Điều dưỡng, Sinh hiệu & Hàng đợi (ADR 0019)
`UserRole.Nurse=5`, `Roles.RecordVitals`=Admin+Nurse, `Roles.ManageQueue`=Admin+Lễ tân+Nurse. `Vitals` (1–1 `AppointmentId`; BMI tính trong bộ nhớ — xem ghi chú dự án mục 8); upsert `POST /api/appointments/{id}/vitals`. `QueueTicket` (số tuần tự theo ngày UTC+7, `AppointmentId?` nullable cho vãng lai, máy trạng thái `Waiting→Called→InProgress→Done/Skipped`). *Guid seed Nurse = `cccc…` (né `eeee…`=duocsi đã dùng).* 216 test.

### Sprint 20 ✅ Dashboard báo cáo (ADR 0020)
4 endpoint chỉ-đọc (`/reports/overview`, `/by-date`, `/by-doctor`, `/revenue-by-date`). UTC+7 qua hằng số `ClinicOffset` (Asia/Ho_Chi_Minh). Doanh thu lọc `InvoiceStatus.Paid`; năng suất bác sĩ tôn trọng RBAC "của tôi". Frontend dashboard: KPI card + `recharts` line chart + bảng + bộ lọc ngày. 224 test.

### Sprint 21 ✅ Thanh toán trước khi thực hiện (ADR 0021)
`DispenseStatus` (Reserved/Paid/Dispensed) đặt ở cấp **Encounter** (không tách entity). Gating CLS: kỹ thuật viên nhập kết quả bị chặn đến khi HĐ CLS `Paid` (`Paraclinical.NotPaid`). Cấp phát tách khỏi chốt phiếu: `POST /encounters/{id}/complete` giữ tồn (`Reserved`); `POST /encounters/{id}/dispense` xuất FEFO thật (`Paid→Dispensed`). Trang Dược sĩ (`PharmacyDispensePage`) hàng chờ `dispenseStatus=Paid`. *`SelectMany` owned collection tính tồn giữ chỗ.* 231 test.