# ADR 0023 — Khép kín vai trò Điều dưỡng (bắt buộc sinh hiệu trước khi khám)

**Ngày:** 2026-09-26
**Trạng thái:** Chấp nhận
**Sprint:** 25 (ngoài kế hoạch — polish sau bảo vệ)

---

## Bối cảnh

Từ Sprint 17 (ADR 0019), điều dưỡng có workspace riêng (`/nurse`, UX-04) để gọi số/đo sinh hiệu,
nhưng đây **chỉ là một lối tắt tuỳ chọn**: `MyClinicPage` (`/my-clinic`) cho phép bác sĩ tự chuyển
`CheckedIn → InProgress` (nút "Bắt đầu khám") mà không cần bất kỳ ai đo sinh hiệu trước — comment gốc
trong code còn ghi rõ "tự phục vụ... không cần chờ Lễ tân/Điều dưỡng thao tác ở màn khác trước". Vé
hàng đợi (`QueueTicket`) cũng có trạng thái `InProgress` riêng nhưng không liên kết gì với việc bắt
đầu khám thật (`Appointment.Start()`) — hai máy trạng thái độc lập.

Hệ quả: vai trò Điều dưỡng gần như optional trên thực tế — nếu phòng khám nhỏ không có người trực
riêng, quy trình vẫn chạy trơn tru mà bỏ qua hẳn bước đo sinh hiệu, không đúng với quy trình khám
ngoại trú thực tế (đo sinh hiệu là bước sàng lọc bắt buộc trước khi bác sĩ khám).

---

## Quyết định

**Chặn ở đúng một điểm nghẽn**: `AppointmentService.StartAsync` (CheckedIn → InProgress) — nơi duy
nhất mở khoá màn khám (`EncounterForm`) cho bác sĩ.

- Thêm bước kiểm tiên quyết `EnsureVitalsRecordedAsync` chạy **sau** khi máy trạng thái Domain xác
  nhận chuyển tiếp hợp lệ nhưng **trước** khi `SaveChangesAsync` — giữ đúng thứ tự lỗi (chuyển tiếp
  sai trạng thái vẫn báo `Appointment.InvalidTransition`, không bị át bởi lỗi thiếu sinh hiệu).
- Điều kiện: đã có ít nhất một bản ghi `Vitals` cho lịch khám — gom theo `VisitId` nếu lịch thuộc một
  Lượt tiếp nhận (đúng cách gom đã dùng ở `VitalsService`, nhiều dịch vụ khám cùng lượt chỉ cần đo
  chung một lần), ngược lại gom theo `AppointmentId`.
- Thiếu sinh hiệu → `409 Appointment.VitalsRequired`, thông báo hướng dẫn quay lại màn "Sinh hiệu &
  Hàng đợi".
- **Không đổi RBAC**: chỉ `Admin`/`Nurse` được gọi `POST /api/appointments/{id}/vitals` (đã có từ
  ADR 0019) — nghĩa là bác sĩ **không thể tự bỏ qua** bằng cách tự đo hộ.
- `QueueTicketDto` có thêm `HasVitals` (so trực tiếp theo `AppointmentId`, chỉ để hiển thị gợi ý UI —
  không phải điều kiện tiên quyết thật, tránh subquery lồng theo Visit phức tạp cho một chỉ báo phụ).
  `MyClinicPage` dùng để vô hiệu hoá nút "Bắt đầu khám" trước khi bác sĩ bấm nhầm; `NurseWorkspacePage`
  dùng để đánh dấu "Đã đo" trên hàng đã xử lý.

### Vì sao không chặn ở `QueueTicket.Start()` (Called → InProgress)

Vé hàng đợi và lịch khám là hai máy trạng thái tách biệt — nhiều luồng (vd bác sĩ tự "Bắt đầu khám"
ở `/my-clinic` không qua gọi số) không tạo/đi qua vé. Chặn ở `Appointment.StartAsync` đảm bảo mọi
đường vào phòng khám (kể cả tự phục vụ) đều đi qua cùng một cửa.

---

## Hệ quả

| Khía cạnh | Tác động |
|-----------|---------|
| Migration | Không — dùng lại bảng `vitals` sẵn có, chỉ thêm điều kiện đọc. |
| Backward compat | Lịch khám cũ đã `InProgress`/`Completed` từ trước không bị hồi tố — chỉ áp cho lần `Start()` mới. |
| RBAC | Không đổi — vẫn `Admin`/`Nurse` cho `RecordVitals`, `StartExam` giữ nguyên vai trò được phép gọi `start`. |
| UI | `MyClinicPage` vô hiệu nút khi chưa đo; `NurseWorkspacePage` đánh dấu đã đo. |
| Test | `AppointmentServiceTests`: 2 test mới (`Start_ShouldFail_WhenVitalsNotRecorded`,
  `Start_ShouldSucceed_WhenVitalsRecorded`), 1 test cũ cập nhật để seed `Vitals` trước khi `Start`. |
| Ngoài phạm vi | Không bắt buộc đo sinh hiệu cho lịch không qua `/my-clinic` self-service (không có,
  vì đây là cửa duy nhất); không thêm ngưỡng cảnh báo sinh hiệu bất thường (đã có ở VIS-04, độc lập). |
