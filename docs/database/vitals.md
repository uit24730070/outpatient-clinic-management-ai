# Bảng `vitals` — Từ điển dữ liệu

Lịch sử các lần đo sinh hiệu. Sinh ra từ entity `ClinicManagement.Domain.Clinical.Vitals`, migration
`AddVitalsAndQueue` ([ADR 0019](../adr/0019-dieu-duong-sinh-hieu-va-hang-doi.md)); `VisitId` +
gom theo Lượt tiếp nhận thêm ở migration `AddVitalsVisitId`; bỏ ràng buộc duy nhất (cho phép đo lại
nhiều lần, giữ lịch sử) ở migration `AllowMultipleVitalsPerVisit`.

## Cột

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng. |
| `AppointmentId` | `uuid` | Không | FK → `appointments` (`ON DELETE RESTRICT`). Lịch khám tại thời điểm đo lần này. |
| `VisitId` | `uuid` | Có | FK → `visits` (`ON DELETE RESTRICT`). Khi lịch thuộc một lượt, dùng khoá này để gom **mọi lần đo của cả lượt** (dùng chung giữa các dịch vụ khám/chuyên khoa); null với lịch lẻ (gom theo `AppointmentId`). |
| `PatientId` | `uuid` | Không | Snapshot bệnh nhân (từ lịch khám). |
| `HeightCm` | `numeric(5,2)` | Có | Chiều cao (cm). |
| `WeightKg` | `numeric(5,2)` | Có | Cân nặng (kg). |
| `TemperatureC` | `numeric(4,1)` | Có | Nhiệt độ (°C). |
| `Pulse` | `integer` | Có | Mạch (lần/phút). |
| `BloodPressureSystolic` | `integer` | Có | Huyết áp tâm thu (mmHg). |
| `BloodPressureDiastolic` | `integer` | Có | Huyết áp tâm trương (mmHg). |
| `SpO2` | `integer` | Có | Độ bão hoà oxy máu (%). |
| `RespiratoryRate` | `integer` | Có | Nhịp thở (lần/phút). |
| `Notes` | `varchar(1000)` | Có | Ghi chú của điều dưỡng. |
| `MeasuredAt` | `timestamptz` | Không | Thời điểm đo — cố định lúc tạo (bản ghi bất biến, không có "cập nhật"). |
| `MeasuredBy` | `uuid` | Không | Id tài khoản đo (điều dưỡng/Admin). |
| `CreatedAt` / `UpdatedAt` | `timestamptz` | Không / Có | Dấu thời gian kiểm toán. |
| `IsDeleted` / `DeletedAt` | `boolean` / `timestamptz` | Không / Có | Xoá mềm. |

> **BMI** không có cột — là **thuộc tính tính toán** ở Domain (`WeightKg / (HeightCm/100)²`, làm tròn 1 chữ số), tính lúc trả DTO.

## Index & khoá ngoại
- `PK_vitals` — khóa chính trên `Id`.
- `IX_vitals_AppointmentId` — thường (không unique) — nhiều lần đo cho cùng một lịch.
- `IX_vitals_VisitId` — thường (không unique) — nhiều lần đo cho cùng một lượt.
- `FK_vitals_appointments_AppointmentId` — `ON DELETE RESTRICT`.
- `FK_vitals_visits_VisitId` — `ON DELETE RESTRICT`.

## Quy tắc nghiệp vụ
- **Bản ghi bất biến, có lịch sử**: `POST /api/appointments/{id}/vitals` LUÔN tạo bản ghi mới (không ghi
  đè) — cho phép đo lại nhiều lần (bệnh nhân yêu cầu đo lại, chỉ số bất thường cần đo kiểm tra…).
  `GET .../vitals` trả lần đo **gần nhất**; `GET .../vitals/history` trả toàn bộ lịch sử (mới nhất trước).
- **Gom theo Lượt tiếp nhận**: khi lịch khám thuộc một lượt (nhiều dịch vụ khám/chuyên khoa cùng một lần
  đến), lịch sử đo dùng CHUNG cho cả lượt (khoá `VisitId`) — đo/xem từ dịch vụ khám nào trong lượt cũng
  ra cùng một lịch sử, tránh đo lặp cho mỗi dịch vụ. Lịch lẻ (không thuộc lượt) gom theo `AppointmentId`.
- Gắn **lịch khám** (đo sau check-in, trước khi có phiếu khám). Bác sĩ đọc theo `AppointmentId` (Encounter 1–1 Appointment).
- RBAC: ghi `Roles.RecordVitals` (Admin/Điều dưỡng); đọc mở cho lâm sàng.
- Biên hợp lý các chỉ số kiểm ở `RecordVitalsRequestValidator` (chỉ khi có giá trị).
