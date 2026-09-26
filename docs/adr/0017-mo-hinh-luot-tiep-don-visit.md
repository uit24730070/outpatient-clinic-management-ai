# ADR 0017 — Mô hình Lượt tiếp đón (Visit) gom nhiều dịch vụ khám/lượt

- **Trạng thái:** Đã chấp nhận
- **Ngày:** 2026-08-23
- **Sprint:** 17
- **Liên quan:** [ADR 0005](0005-mo-hinh-lich-kham-va-may-trang-thai.md) (Appointment/máy trạng thái), [ADR 0006](0006-mo-hinh-benh-an-encounter-prescription.md) (Encounter 1–1 Appointment), [ADR 0014](0014-mo-hinh-vien-phi-va-thu-ngan.md) (Invoice gắn `AppointmentId`), [ADR 0016](0016-dang-ky-dich-vu-walkin-cls-va-ky-thuat-vien.md) (dịch vụ khám trên Appointment)

## Bối cảnh

Một `Appointment` gắn **một bác sĩ** (`DoctorId`) và (từ Sprint 16) **một dịch vụ khám** (`ServicePriceId` loại `Consultation`). Khi bệnh nhân đến phòng khám muốn đăng ký **cùng lúc nhiều dịch vụ khám** (ví dụ khám 2 chuyên khoa khác nhau trong một lần đến), mô hình không biểu diễn được: buộc phải tạo nhiều lịch rời rạc, không có thực thể gom "một lần đến khám".

Thực tế các HIS/PMS dùng khái niệm **lượt khám / lượt tiếp đón (Visit/Encounter-group)**: một lần bệnh nhân đến, lễ tân mở một lượt rồi đăng ký 1..n dịch vụ khám (nhiều phòng/chuyên khoa) + cận lâm sàng, thu viện phí gộp cả lượt.

Từ Sprint 14.5 hệ thống đã dùng `Invoice.AppointmentId` như **khoá gom tạm** ("lượt tiếp đón nguồn") — tức "lượt" đang được biểu diễn bằng chính một `Appointment`, kế thừa đúng giới hạn 1 bác sĩ/1 dịch vụ.

## Quyết định

Nâng "lượt" thành **aggregate `Visit` riêng, nằm trên `Appointment`**.

1. **`Domain/Visits/Visit`** (mã `LK-`): `PatientId` (snapshot), `Status` (`VisitStatus` `Open → Closed`/`Cancelled`, lưu chuỗi), `Note?`; máy trạng thái nhẹ ở Domain (`Close`/`Cancel` trả `Result`). Soft delete qua `Entity`. **Không sở hữu** các appointment (chúng có vòng đời/máy trạng thái riêng) — chỉ gom nhóm.
2. **Mỗi dịch vụ khám vẫn là một `Appointment`**, thêm `Appointment.VisitId?` **nullable** (ctor tham số mặc định → lịch tạo trước Sprint 17 không gãy; lịch lẻ = "lượt một dịch vụ"). Nhờ vậy **tái dùng nguyên vẹn** máy trạng thái check-in/start/complete (ADR 0005), chống trùng giờ bác sĩ, và quan hệ 1–1 với `Encounter` (ADR 0006) — hai chuyên khoa ⇒ hai appointment ⇒ hai phiếu khám riêng, đúng thực tế.
3. **`VisitService.CreateAsync`** tạo `Visit` + N `Appointment` trong **một `SaveChanges`** (nguyên tử): validate bệnh nhân, từng bác sĩ tồn tại, từng dịch vụ thuộc loại `Consultation`, chống trùng giờ mỗi bác sĩ (xét cả CSDL lẫn các lịch đang dựng trong cùng lượt). `AddServiceAsync` thêm dịch vụ vào lượt còn `Open`.
4. **Gom viện phí theo lượt = suy dẫn**, **không** thêm cột `Invoice.VisitId`/`LabOrder.VisitId`: hoá đơn của lượt = các HĐ có `AppointmentId` thuộc lượt; tổng đã lập/đã thu/còn nợ tính phía server (bỏ HĐ `Cancelled`). Giảm bề mặt migration và tránh hồi quy Billing (ADR 0014/14.5).
5. **RBAC:** tạo/thao tác lượt = `Roles.ManageStaff` (Admin/Lễ tân); đọc mọi vai trò. **Walk-in**: tạo lượt không cần lịch hẹn trước.

Migration `AddVisits` (bảng `visits` + cột `appointments.visit_id`, FK `Restrict`, index `VisitId`).

## Hệ quả

**Tích cực:**
- Biểu diễn đúng "một lần đến khám nhiều dịch vụ"; điểm vào quy trình tiếp đón rõ ràng cho lễ tân.
- Rủi ro hồi quy thấp: không đụng máy trạng thái Appointment/Encounter, không đổi schema Invoice/LabOrder.
- `VisitId` sẵn sàng làm khoá gom cho hàng đợi (Sprint 19) và các báo cáo theo lượt (Sprint 20).

**Đánh đổi / nợ:**
- Gom billing **suy dẫn** qua `AppointmentId`: nếu phát sinh HĐ của lượt không gắn lịch nào (hiện chưa có đường đó) sẽ không gom được → khi cần mới thêm `Invoice.VisitId`.
- `VisitService` **lặp lại** phần validate/tạo `Appointment` của `AppointmentService` (bác sĩ/dịch vụ/chống trùng) thay vì gọi lại — chấp nhận trùng nhỏ để giữ một `SaveChanges` nguyên tử; nếu phình có thể tách helper dùng chung.
- Chưa có nút "thu tiền cả lượt" (lập/thu từng HĐ; tổng đã hiển thị) và chưa gắn walk-in CLS trực tiếp vào `Visit`.
- Chống trùng giờ trong lượt kiểm ở service (có race như các slice khác) — lối thoát chặt là exclusion constraint PG (ghi ở ADR 0005).
