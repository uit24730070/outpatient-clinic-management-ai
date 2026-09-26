# Quy ước API & Endpoint

## 1. Envelope phản hồi thống nhất

Mọi phản hồi dùng cấu trúc `ApiResponse<T>`:

```jsonc
// Thành công
{ "success": true, "data": { /* ... */ }, "error": null, "meta": null }

// Thất bại
{ "success": false, "data": null,
  "error": { "code": "Patient.NotFound", "message": "…", "details": null },
  "meta": null }
```

- `details` (tùy chọn) là bản đồ `{ "TênTrường": ["thông báo lỗi", …] }` cho lỗi validation.
- JSON dùng camelCase.

## 2. Ánh xạ lỗi → HTTP status

| `error.type` (nội bộ) | HTTP |
|-----------------------|------|
| Validation | 400 |
| Unauthorized | 401 |
| Forbidden | 403 |
| NotFound | 404 |
| Conflict | 409 |
| Failure | 500 |

Lỗi model-binding và FluentValidation đều trả 400 với `code = "Validation.Failed"` kèm `details`.

## 2b. Xác thực & phân quyền (JWT + RBAC)

- **Gửi token:** sau khi đăng nhập, đính kèm header `Authorization: Bearer <accessToken>` cho mọi request tới endpoint được bảo vệ.
- **Chưa đăng nhập / token sai/hết hạn → 401** với envelope `code = "Auth.Unauthorized"`.
- **Sai vai trò (đăng nhập nhưng không đủ quyền) → 403** với envelope `code = "Auth.Forbidden"`.
- **Vai trò:** `Admin`, `Receptionist` (Lễ tân), `Doctor` (Bác sĩ), `Pharmacist` (Dược sĩ, [ADR 0013](../adr/0013-vai-tro-duoc-si-quan-ly-kho-thuoc.md)), `Technician` (Kỹ thuật viên, [ADR 0016](../adr/0016-dang-ky-dich-vu-walkin-cls-va-ky-thuat-vien.md) — thực hiện & nhập kết quả CLS: `Roles.RecordLabResult` = Admin + Bác sĩ + Kỹ thuật viên), `Nurse` (Điều dưỡng, [ADR 0019](../adr/0019-dieu-duong-sinh-hieu-va-hang-doi.md) — nhập sinh hiệu `Roles.RecordVitals` = Admin + Điều dưỡng; điều phối hàng đợi `Roles.ManageQueue` = Admin + Lễ tân + Điều dưỡng). Quy ước hiện tại:
  - Mọi endpoint nghiệp vụ (Bệnh nhân/Bác sĩ/Chuyên khoa) **yêu cầu đăng nhập**.
  - **Đọc** (GET) — mọi vai trò đã đăng nhập.
  - **Ghi** (POST/PUT/DELETE) danh mục & lịch khám — chỉ **Admin** và **Lễ tân**.
  - **Ghi bệnh án** (phiếu khám/đơn thuốc, `/api/encounters`) — chỉ **Bác sĩ** và **Admin** (hành vi lâm sàng; xem [ADR 0006](../adr/0006-mo-hinh-benh-an-encounter-prescription.md)).
- `POST /api/auth/login` và `GET /health` là `AllowAnonymous`.
- **Trải nghiệm & nav theo vai trò (frontend, [ADR 0009](../adr/0009-phan-quyen-va-trai-nghiem-giao-dien-theo-vai-tro.md)):** mỗi vai trò có menu gọn + trang mặc định + guard route riêng (Lễ tân/Admin → `/appointments`; Bác sĩ → `/my-clinic`). Guard FE chỉ là **UX** — backend vẫn là chốt chặn (403); guard FE không nới lỏng RBAC BE.
- **Liên kết `User↔Doctor`:** `GET /api/auth/me` (và phản hồi login) trả thêm `doctorId` — hồ sơ bác sĩ gắn với tài khoản (tra cứu server-side), `null` nếu chưa gắn/không phải bác sĩ. Bác sĩ dùng `doctorId` để lọc "của tôi" bằng tham số `doctorId` sẵn có ở `/api/appointments` và `/api/encounters`.

## 3. Endpoint — Bệnh nhân

Base path: `/api/patients`

| Method | Path | Mô tả | Thành công |
|--------|------|-------|-----------|
| POST | `/api/patients` | Tạo bệnh nhân | 201 Created |
| GET | `/api/patients?page=&pageSize=&search=` | Danh sách phân trang + tìm kiếm | 200 |
| GET | `/api/patients/{id}` | Chi tiết theo Id | 200 / 404 |
| PUT | `/api/patients/{id}` | Cập nhật | 200 / 404 |
| DELETE | `/api/patients/{id}` | Xoá mềm (ngừng sử dụng) | 204 / 404 |

### Body tạo/cập nhật
```json
{
  "fullName": "Nguyễn Văn A",
  "dateOfBirth": "1990-05-20",
  "gender": 1,
  "phoneNumber": "0912345678",
  "address": "Hà Nội"
}
```
- `gender`: `0` Unknown, `1` Male, `2` Female, `3` Other.
- `dateOfBirth`, `phoneNumber`, `address`: tùy chọn.

### Phân trang (GET danh sách)
- `page` (mặc định 1), `pageSize` (mặc định 20, tối đa 100).
- `search`: khớp không phân biệt hoa thường trên `fullName`, `code`, `phoneNumber`.
- `data` là `PagedResult`: `items`, `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`.

## 4. Endpoint — Chuyên khoa

Base path: `/api/specialties`. **Ghi (POST/PUT/DELETE) chỉ Admin** (`Roles.ManageCatalog` — danh mục master); **đọc (GET) cho mọi vai trò đã đăng nhập**.

| Method | Path | Mô tả | Thành công |
|--------|------|-------|-----------|
| POST | `/api/specialties` | Tạo chuyên khoa | 201 Created |
| GET | `/api/specialties?page=&pageSize=&search=` | Danh sách phân trang + tìm kiếm (theo `name`) | 200 |
| GET | `/api/specialties/{id}` | Chi tiết | 200 / 404 |
| PUT | `/api/specialties/{id}` | Cập nhật | 200 / 404 / 409 |
| DELETE | `/api/specialties/{id}` | Xoá mềm | 204 / 404 |

### Body tạo/cập nhật
```json
{ "name": "Tim mạch", "description": "Chẩn đoán và điều trị bệnh lý tim mạch." }
```
- `name` **bắt buộc, duy nhất** (không phân biệt hoa/thường). Trùng tên → 409 `code="Specialty.NameConflict"`.
- `description` tùy chọn.

## 5. Endpoint — Bác sĩ

Base path: `/api/doctors`. **Ghi (POST/PUT/DELETE) + gắn/gỡ tài khoản chỉ Admin** (`Roles.ManageCatalog`/`Admin`); **đọc (GET) cho mọi vai trò** (Lễ tân cần chọn bác sĩ khi đặt lịch).

| Method | Path | Mô tả | Thành công |
|--------|------|-------|-----------|
| POST | `/api/doctors` | Tạo bác sĩ | 201 Created |
| GET | `/api/doctors?page=&pageSize=&search=` | Danh sách phân trang + tìm kiếm | 200 |
| GET | `/api/doctors/{id}` | Chi tiết | 200 / 404 |
| PUT | `/api/doctors/{id}` | Cập nhật | 200 / 404 |
| DELETE | `/api/doctors/{id}` | Xoá mềm | 204 / 404 |
| POST | `/api/doctors/{id}/link-user` | Gắn tài khoản (role Doctor) — **Admin** | 200 / 400 / 404 / 409 |
| POST | `/api/doctors/{id}/unlink-user` | Gỡ liên kết tài khoản — **Admin** | 200 / 404 |

### Body tạo/cập nhật
```json
{
  "fullName": "Nguyễn Văn A",
  "specialtyId": "22222222-2222-2222-2222-222222222222",
  "phoneNumber": "0912345678",
  "email": "a@clinic.vn"
}
```
- `specialtyId` **bắt buộc** và phải trỏ tới chuyên khoa tồn tại; nếu không → 400 `code="Doctor.SpecialtyNotFound"`.
- `phoneNumber`, `email` tùy chọn (email phải hợp lệ).
- `search`: khớp không phân biệt hoa thường trên `fullName`, `code`, `phoneNumber`.
- DTO trả kèm `specialtyName` (join; `null` nếu chuyên khoa đã bị xoá).

## 6. Endpoint — Lịch khám (Appointment)

Base path: `/api/appointments`. Yêu cầu đăng nhập; **ghi** (POST/PUT/DELETE + hành động trạng thái) chỉ **Admin/Lễ tân**, **đọc** cho mọi vai trò.

| Method | Path | Mô tả | Thành công |
|--------|------|-------|-----------|
| POST | `/api/appointments` | Đặt lịch mới (`Scheduled`; nhận `servicePriceId?` — dịch vụ khám Consultation) | 201 / 400 / 409 |
| GET | `/api/appointments?page=&pageSize=&date=&doctorId=&patientId=&status=` | Danh sách/hàng đợi, lọc | 200 |
| GET | `/api/appointments/last?patientId=` | Lượt gần nhất của bệnh nhân (prefill dịch vụ khi tái khám); `data=null` nếu chưa có | 200 |
| GET | `/api/appointments/{id}` | Chi tiết | 200 / 404 |
| PUT | `/api/appointments/{id}` | Đổi khung giờ/lý do (reschedule) | 200 / 404 / 409 |
| DELETE | `/api/appointments/{id}` | Xoá mềm (khác huỷ) | 204 / 404 |
| POST | `/api/appointments/{id}/check-in` | `Scheduled → CheckedIn` | 200 / 404 / 409 |
| POST | `/api/appointments/{id}/start` | `CheckedIn → InProgress` | 200 / 404 / 409 |
| POST | `/api/appointments/{id}/complete` | `InProgress → Completed` | 200 / 404 / 409 |
| POST | `/api/appointments/{id}/cancel` | `Scheduled/CheckedIn/InProgress → Cancelled` | 200 / 404 / 409 |
| POST | `/api/appointments/{id}/no-show` | `Scheduled/CheckedIn → NoShow` | 200 / 404 / 409 |

### Body tạo lịch
```json
{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "doctorId": "22222222-2222-2222-2222-222222222222",
  "startTime": "2026-08-10T08:00:00Z",
  "endTime": "2026-08-10T08:30:00Z",
  "reason": "Khám tổng quát",
  "servicePriceId": null
}
```
- `startTime`/`endTime`: `timestamptz` (UTC), `endTime > startTime`. Body cập nhật (PUT) gồm `startTime`, `endTime`, `reason`, `servicePriceId` (không đổi bệnh nhân/bác sĩ).
- `patientId`/`doctorId` phải tồn tại; nếu không → 400 `code="Appointment.PatientNotFound"` / `"Appointment.DoctorNotFound"`.
- `servicePriceId` (tuỳ chọn, [ADR 0016](../adr/0016-dang-ky-dich-vu-walkin-cls-va-ky-thuat-vien.md)): dịch vụ khám phải tồn tại + loại `Consultation` → sai loại 400 `Appointment.ServiceNotConsultation`; khi gắn, snapshot `serviceName`/`servicePrice` vào lịch.

### Trạng thái & mã lỗi
- `status` (số, giống cách map `gender`): `0` Scheduled, `1` CheckedIn, `2` InProgress, `3` Completed, `4` Cancelled, `5` NoShow.
- **Trùng khung giờ bác sĩ** (chồng lịch còn hiệu lực) → 409 `code="Appointment.Overlap"`.
- **Chuyển trạng thái không hợp lệ** (sai máy trạng thái) → 409 `code="Appointment.InvalidTransition"`.
- Máy trạng thái & quy tắc chống trùng: xem [ADR 0005](../adr/0005-mo-hinh-lich-kham-va-may-trang-thai.md).

### Bộ lọc (GET danh sách)
- `date` (`YYYY-MM-DD`, lọc theo ngày UTC của `startTime`), `doctorId`, `patientId`, `status` — đều tuỳ chọn, kết hợp AND. Sắp xếp theo `startTime` tăng dần.

## 6b. Endpoint — Khám bệnh & Bệnh án (Encounter)

Base path: `/api/encounters`. Yêu cầu đăng nhập; **tạo/sửa/chốt phiếu** chỉ **Bác sĩ/Admin** (`Roles.RecordEncounter`); **cấp phát thực** cho **Admin/Dược sĩ** (`Roles.ManagePharmacy`); **đọc** cho mọi vai trò. Từ Sprint 21 ([ADR 0021](../adr/0021-thanh-toan-truoc-khi-thuc-hien.md)) chốt phiếu chỉ **giữ tồn** (Reserved), cấp phát thực tách riêng sau khi thu tiền. Xem [ADR 0006](../adr/0006-mo-hinh-benh-an-encounter-prescription.md) và [ADR 0011](../adr/0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md) (kho/FEFO).

| Method | Path | Mô tả | Thành công |
|--------|------|-------|-----------|
| POST | `/api/encounters` | Tạo phiếu khám cho lịch đang `InProgress` (kèm đơn thuốc; trạng thái `Draft`) | 201 / 400 / 409 |
| GET | `/api/encounters?page=&pageSize=&patientId=&doctorId=&status=&dispenseStatus=` | Lịch sử khám (mới nhất trước), lọc | 200 |
| GET | `/api/encounters/{id}` | Chi tiết phiếu kèm đơn thuốc | 200 / 404 |
| GET | `/api/encounters/by-appointment/{appointmentId}` | Lấy phiếu theo lịch (1–1) | 200 / 404 |
| PUT | `/api/encounters/{id}` | Sửa nội dung + thay toàn bộ đơn thuốc (chỉ khi `Draft`) | 200 / 404 / 409 |
| POST | `/api/encounters/{id}/complete` | Chốt phiếu + khép lịch **+ giữ tồn** (Reserved; kiểm tồn khả dụng) — Bác sĩ/Admin | 200 / 404 / 409 |
| POST | `/api/encounters/{id}/dispense` | **Cấp phát thực** (Paid → Dispensed): trừ tồn FEFO, ghi `Dispense` — Dược sĩ/Admin | 200 / 400 / 404 / 409 |
| POST | `/api/encounters/{id}/return-stock` | **Hoàn kho** (Dispensed → Returned): nhập lại tồn đúng lô, ghi `Return` bù — Dược sĩ/Admin (ADR 0022, REF-02) | 200 / 404 / 409 |

### Body tạo/sửa phiếu
```json
{
  "appointmentId": "33333333-3333-3333-3333-333333333333",
  "symptoms": "Sốt, ho",
  "diagnosis": "Viêm họng cấp",
  "notes": "Nghỉ ngơi",
  "prescriptionItems": [
    { "medicationId": "aaaa...", "drugName": "Paracetamol", "dosage": "500mg", "quantity": 10, "instruction": "Ngày 2 lần" }
  ]
}
```
- `appointmentId` chỉ ở body **tạo**; body sửa (PUT) gồm `symptoms`, `diagnosis`, `notes`, `prescriptionItems`.
- `diagnosis` **bắt buộc**; `symptoms`/`notes`/`instruction` tuỳ chọn; mỗi dòng đơn cần `drugName`, `dosage`, `quantity > 0`.
- `medicationId` **tuỳ chọn** (P2 — ADR 0011): có ⇒ dòng đơn gắn danh mục, đi qua **vòng đời cấp phát**; phải tồn tại (`Pharmacy.MedicationNotFound` 404). `null`/thiếu ⇒ thuốc ngoài danh mục (không trừ tồn).
- **Chốt phiếu (ADR 0021)** chỉ **giữ tồn**: kiểm **tồn khả dụng** = Σ(tồn lô còn hạn) − Σ(đang Reserved/Paid). Thiếu → `Pharmacy.InsufficientStock` (409, **không chốt được**). Không trừ tồn vật lý, không ghi sổ cái ở bước này.
- **Cấp phát thực** (`/dispense`) yêu cầu `dispenseStatus = Paid` (đã thu tiền thuốc): trừ tồn các lô **còn hạn** theo hạn tăng dần, ghi `Dispense`. Chưa thu → `Pharmacy.NotPaid` (409); không có thuốc gắn danh mục → `Pharmacy.NothingToDispense` (400); thiếu tồn → `Pharmacy.InsufficientStock` (409); tranh chấp lô → `Pharmacy.ConcurrencyConflict` (409).
- `patientId`/`doctorId` **không** gửi — service suy ra (snapshot) từ lịch khám.

### Trạng thái & mã lỗi
- `status` (số): `0` Draft, `1` Completed.
- `dispenseStatus` (số, ADR 0021): `0` None (không có thuốc gắn danh mục), `1` Reserved (giữ tồn, chờ thu tiền), `2` Paid (đã thu, chờ cấp phát), `3` Dispensed (đã cấp phát). DTO trả thêm `dispenseStatus`/`reservedAt`/`medicationPaidAt`/`dispensedAt`.
- Lịch không tồn tại → 400 `code="Encounter.AppointmentNotFound"`.
- Lịch không ở `InProgress` khi tạo → 409 `code="Encounter.AppointmentNotInProgress"`.
- Lịch đã có phiếu → 409 `code="Encounter.AlreadyExists"`.
- Sửa/chốt sai vòng đời phiếu → 409 `code="Encounter.InvalidTransition"`.

### Bộ lọc (GET danh sách)
- `patientId` (lịch sử khám của bệnh nhân), `doctorId`, `status`, `dispenseStatus` (hàng chờ cấp phát Dược sĩ = `dispenseStatus=2`) — tuỳ chọn, kết hợp AND. Sắp theo `createdAt` **giảm dần**.

## 6c. Endpoint — Trợ lý AI

Yêu cầu đăng nhập; chỉ **Bác sĩ/Admin** (`Roles.RecordEncounter`, đọc bệnh án). Reindex là bảo trì → **Admin**. Xem [ADR 0007](../adr/0007-tich-hop-llm.md) (LLM) và [ADR 0008](../adr/0008-rag-va-vector-store.md) (RAG).

| Method | Path | Mô tả | Quyền | Thành công |
|--------|------|-------|-------|-----------|
| POST | `/api/patients/{id}/ai-summary` | Tóm tắt lịch sử khám (context stuffing) | Bác sĩ/Admin | 200 / 404 / 500 |
| POST | `/api/patients/{id}/ai-ask` | Hỏi đáp có ngữ cảnh (RAG) trên bệnh án | Bác sĩ/Admin | 200 / 400 / 404 / 500 |
| POST | `/api/ai/reindex` | Backfill embedding toàn bộ phiếu khám | Admin | 200 / 500 |

### Phản hồi (`data`)
```json
{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "patientName": "Nguyễn Văn A",
  "summary": "…bản tóm tắt tiếng Việt…",
  "encounterCount": 3,
  "model": "gpt-5-mini",
  "generatedAt": "2026-08-08T10:42:54Z"
}
```
- Nạp tối đa **10 phiếu khám gần nhất** làm ngữ cảnh (context stuffing — chưa RAG). Bệnh nhân **chưa có phiếu** → vẫn 200, `encounterCount = 0`, `summary` là thông điệp phù hợp (không gọi LLM).
- Bệnh nhân không tồn tại → 404 `code="Patient.NotFound"`.
- **Mã lỗi `Ai.*`** (đều `Failure` → 500, giữ envelope `ApiResponse`, không crash):
  - `Ai.Unavailable` — thiếu khoá API / không kết nối được / provider trả mã lỗi.
  - `Ai.Timeout` — provider phản hồi quá thời gian chờ (`Ai:TimeoutSeconds`).
  - `Ai.BadResponse` — phản hồi rỗng/không đọc được.
- **Cấu hình & fake:** section `Ai` (`ApiKey` trống ở `appsettings.json`, đặt qua `Ai__ApiKey`; `Model` mặc định `gpt-5-mini`). Bật `Ai:UseFake = true` (mặc định ở Development) → trả tóm tắt mô phỏng tất định, **không gọi mạng, không tốn phí**.

### Hỏi đáp RAG — `POST /api/patients/{id}/ai-ask`
Body: `{ "question": "…" }`. Phản hồi (`data`):
```json
{
  "patientId": "11111111-1111-1111-1111-111111111111",
  "patientName": "Nguyễn Văn A",
  "question": "Bệnh nhân từng dùng kháng sinh gì?",
  "answer": "…câu trả lời tiếng Việt, dẫn Nguồn N…",
  "model": "gpt-5-mini",
  "sources": [
    { "encounterId": "…", "createdAt": "2026-08-01T09:00:00Z", "diagnosis": "Viêm họng cấp", "similarity": 0.94 }
  ],
  "generatedAt": "2026-08-09T10:42:54Z"
}
```
- Luồng RAG: embed câu hỏi → truy hồi **top-5** phiếu gần nhất theo cosine (pgvector) → dựng prompt kèm nguồn → gọi LLM. `sources` để **truy vết** phiếu đã dùng.
- **Chưa truy hồi được phiếu nào** (chưa lập chỉ mục / bệnh nhân chưa có phiếu) → vẫn 200, `sources` rỗng, `answer` là thông điệp phù hợp (**không gọi LLM**).
- Câu hỏi rỗng → 400 `code="Ai.QuestionRequired"`. Bệnh nhân không tồn tại → 404 `Patient.NotFound`.
- **Mã lỗi `Embedding.*`** (đều `Failure` → 500): `Embedding.Unavailable` (thiếu khoá/không kết nối/provider lỗi), `Embedding.Timeout`, `Embedding.BadResponse`. Lỗi tầng chat vẫn dùng `Ai.*`.

### Backfill — `POST /api/ai/reindex`
Lập chỉ mục embedding cho toàn bộ phiếu khám. Phản hồi `data`: `{ "indexed": <số phiếu> }`. Dùng khi bật khoá embedding lần đầu hoặc đổi model.

### Cấu hình embedding (section `Ai`)
`EmbeddingApiKey` (khoá Voyage AI, **trống** ở repo, đặt qua `Ai__EmbeddingApiKey`), `EmbeddingModel` (mặc định `voyage-3`, 1024 chiều), `EmbeddingBaseUrl` (`https://api.voyageai.com`), `UseFakeEmbedding` (mặc định `true` ở Development → vector tất định, không gọi mạng). Vector store: **pgvector** (bảng `encounter_embeddings`) — cần image `pgvector/pgvector:pg16`.

## 6d. Endpoint — Trợ lý hội thoại (Chatbot, AI-03)

Yêu cầu đăng nhập; **mọi vai trò**. Phạm vi dữ liệu **không** chốt bằng `[Authorize(Roles=...)]` mà do **lớp công cụ** kiểm soát theo vai trò người gọi (bác sĩ chỉ dữ liệu của mình). Xem [ADR 0010](../adr/0010-chatbot-tool-calling.md).

| Method | Path | Mô tả | Quyền | Thành công |
|--------|------|-------|-------|-----------|
| POST | `/api/assistant/chat` | Hỏi đáp nghiệp vụ nhiều lượt (tool-calling) | Mọi vai trò | 200 / 400 / 500 |

Body — gửi **toàn bộ lịch sử hội thoại** (lượt cuối phải là `user`):
```json
{
  "messages": [
    { "role": "user", "content": "Hôm nay bác sĩ Trần B có mấy lịch?" }
  ]
}
```
Phản hồi (`data`):
```json
{
  "answer": "Bác sĩ Trần B hôm nay có 3 lịch: 2 đã check-in, 1 đang chờ.",
  "model": "gpt-5-mini",
  "toolCalls": [
    { "name": "list_appointments", "arguments": "{\"date\":\"2026-08-10\"}", "result": "{\"total\":3,\"items\":[…]}" }
  ],
  "generatedAt": "2026-08-10T10:42:54Z"
}
```
- Trợ lý tự gọi các **công cụ chỉ-đọc** để lấy dữ liệu: `search_patients`, `list_doctors`, `list_appointments`, `get_patient_encounters`. `toolCalls` liệt kê công cụ đã gọi để **truy vết**.
- **Kiểm soát quyền:** bác sĩ bị ép `doctorId` của chính mình ở `list_appointments`/`get_patient_encounters`; bác sĩ chưa gắn hồ sơ → công cụ báo rõ, không lộ dữ liệu. Không có công cụ **ghi**.
- Câu hỏi rỗng / không có lượt người dùng → 400 `code="Ai.QuestionRequired"`. Vượt số vòng gọi công cụ (tối đa 5) → 500 `code="Ai.ToolLoopExceeded"`. Lỗi tầng LLM giữ mã `Ai.*` (xem §6c).
- **Fake:** bật `Ai:UseFake = true` (mặc định Development) → trả lời tất định, **không gọi mạng, không gọi công cụ**.

## 7. Endpoint — Xác thực

Base path: `/api/auth`

| Method | Path | Mô tả | Quyền | Thành công |
|--------|------|-------|-------|-----------|
| POST | `/api/auth/login` | Đăng nhập, trả JWT | Anonymous | 200 / 401 |
| GET | `/api/auth/me` | Thông tin người dùng hiện tại | Đã đăng nhập | 200 / 401 |

### Body đăng nhập
```json
{ "username": "admin", "password": "Admin@123" }
```

### Phản hồi đăng nhập (`data`)
```json
{
  "accessToken": "<JWT>",
  "expiresAt": "2026-08-08T10:42:54Z",
  "user": {
    "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "username": "admin",
    "fullName": "Quản trị hệ thống",
    "role": "Admin",
    "email": "admin@clinic.vn",
    "doctorId": null
  }
}
```
- `user.doctorId`: hồ sơ bác sĩ gắn với tài khoản (chỉ có giá trị với user Bác sĩ đã liên kết); `null` với Admin/Lễ tân hoặc bác sĩ chưa gắn hồ sơ. `GET /api/auth/me` trả cùng cấu trúc `user`.
- Sai tài khoản/mật khẩu hoặc tài khoản bị khoá → 401 `code = "Auth.InvalidCredentials"` (không phân biệt lý do).
- Tài khoản mặc định (seed): Admin `admin` / `Admin@123`; Bác sĩ demo `bacsi` / `Doctor@123` (gắn hồ sơ `BS-000001`).

## 7b. Endpoint — Quản lý người dùng (Admin)

Base path: `/api/users`. **Chỉ Admin** (`[Authorize(Roles = Admin)]`) — vai trò khác → 403. Không endpoint nào lộ `PasswordHash`. Chi tiết đầy đủ (bao gồm gắn/gỡ `User↔Doctor`): xem [users.md](../database/users.md).

| Method | Path | Mô tả | Thành công |
|--------|------|-------|-----------|
| POST | `/api/users` | Tạo tài khoản (băm mật khẩu lúc chạy) | 201 / 400 / 409 |
| GET | `/api/users?page=&pageSize=&search=&role=&isActive=` | Danh sách, lọc theo vai trò/trạng thái | 200 |
| GET | `/api/users/{id}` | Chi tiết (kèm `doctorId` nếu đã gắn) | 200 / 404 |
| PUT | `/api/users/{id}` | Cập nhật họ tên/vai trò/email | 200 / 400 / 404 |
| POST | `/api/users/{id}/reset-password` | Đặt lại mật khẩu | 204 / 400 / 404 |
| POST | `/api/users/{id}/activate` | Mở khoá đăng nhập | 204 / 404 |
| POST | `/api/users/{id}/deactivate` | Khoá đăng nhập (chặn tự khoá) | 204 / 400 / 404 |
| DELETE | `/api/users/{id}` | Xoá mềm (chặn tự xoá) | 204 / 400 / 404 |

### Body & mã lỗi
```json
{ "username": "letan1", "password": "MatKhau@123", "fullName": "Lễ Tân Một", "role": "Receptionist", "email": null }
```
- **`role` nhận CHUỖI** (`"Admin"`/`"Receptionist"`/`"Doctor"`) — **ngoại lệ** so với quy ước enum-số (vd `gender`/`status`), để khớp const-map vai trò dạng chuỗi ở frontend. Body cập nhật (PUT) gồm `fullName`, `role`, `email`.
- Mật khẩu: tối thiểu 8 ký tự, có cả chữ và số. `reset-password` body `{ "newPassword": "…" }`.
- Username trùng (kể cả bản ghi đã xoá) → 409 `code="User.UsernameTaken"`.
- Tự khoá/tự xoá tài khoản đang đăng nhập → 400 `User.CannotDeactivateSelf` / `User.CannotDeleteSelf`.

## 7c. Endpoint — Kho thuốc (Pharmacy)

Nghiệp vụ kho dược P1 (Sprint 11) + P2 (Sprint 12): danh mục thuốc, lô/tồn theo hạn dùng, nhập kho, sổ cái, **cấp phát FEFO** (xem §6b `/complete`) và **cảnh báo kho**. Chi tiết bảng: [medications.md](../database/medications.md), [medication_batches.md](../database/medication_batches.md), [stock_receipts.md](../database/stock_receipts.md), [stock_transactions.md](../database/stock_transactions.md). Mô hình: [ADR 0011](../adr/0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md).

**RBAC:** ghi = `Roles.ManagePharmacy` (**Admin + Dược sĩ**, [ADR 0013](../adr/0013-vai-tro-duoc-si-quan-ly-kho-thuoc.md)); **đọc danh mục thuốc mở cho mọi vai trò** (bác sĩ tra khi kê đơn). Sổ cái + cảnh báo chỉ Admin + Dược sĩ.

| Method | Path | Mô tả | Thành công |
|--------|------|-------|-----------|
| POST | `/api/medications` | Tạo thuốc (sinh mã `TH-`) | 201 / 400 |
| GET | `/api/medications?page=&pageSize=&search=` | Danh sách + tìm kiếm (tên/mã/hoạt chất), kèm `stockOnHand` | 200 |
| GET | `/api/medications/{id}` | Chi tiết + tồn tổng | 200 / 404 |
| GET | `/api/medications/{id}/batches` | Danh sách lô (tồn theo lô + hạn dùng) | 200 / 404 |
| PUT | `/api/medications/{id}` | Cập nhật thuốc | 200 / 400 / 404 |
| DELETE | `/api/medications/{id}` | Xoá mềm thuốc | 204 / 404 |
| POST | `/api/stock-receipts` | Tạo phiếu nhập (tăng tồn theo lô + ghi sổ cái `Import`) | 201 / 400 / 404 |
| GET | `/api/stock-receipts?page=&pageSize=` | Danh sách phiếu nhập (mới nhất trước) | 200 |
| GET | `/api/stock-receipts/{id}` | Chi tiết phiếu + các dòng nhập | 200 / 404 |
| GET | `/api/stock-transactions?page=&pageSize=&medicationId=&type=` | Sổ cái giao dịch tồn (Admin/Lễ tân) | 200 |
| GET | `/api/pharmacy/alerts?expiringInDays=30` | Cảnh báo tồn thấp + lô sắp/đã hết hạn (Admin/Lễ tân) | 200 |

### Body & mã lỗi
```json
// POST /api/medications
{ "name": "Paracetamol 500mg", "activeIngredient": "Paracetamol", "unit": "viên", "reorderLevel": 100, "description": null }

// POST /api/stock-receipts
{ "supplierName": "Cty Dược ABC", "receivedAt": "2026-08-22T00:00:00Z", "note": null,
  "items": [ { "medicationId": "…", "batchNumber": "L2608", "expiryDate": "2027-12-31", "quantity": 40, "unitCost": 1500 } ] }
```
- `expiryDate` là **ngày** (`DateOnly`, `"yyyy-MM-dd"`); `stockOnHand` = tổng tồn các lô chưa xoá (tính phía server).
- `type` (sổ cái) nhận tên enum (`Import`/`Dispense`/`Adjust`) hoặc số. Cấp phát ghi `Dispense` (âm), `ReferenceType="Encounter"`.
- Phiếu nhập **bất biến** (không sửa/xoá). Thuốc trong dòng không tồn tại/đã xoá → 404 `Pharmacy.MedicationNotFound`; phiếu không có dòng → 400.
- **Cảnh báo kho** (`/api/pharmacy/alerts`): `lowStock` (thuốc có `stockOnHand ≤ reorderLevel`) + `expiringBatches` (lô còn tồn có `expiryDate ≤ hôm nay + N`, cờ `isExpired`). `expiringInDays` mặc định 30, kẹp 0..365.
- **Cấp phát** (trừ tồn FEFO) nằm ở luồng chốt phiếu khám — xem §6b `POST /api/encounters/{id}/complete`.

## 7d. Endpoint — Viện phí & Thu ngân (Billing)

Nghiệp vụ thu ngân (Sprint 14 + **P2 Sprint 14.5**): bảng giá dịch vụ, hoá đơn, thu tiền, in. **P2 — Mô hình A (nhiều hoá đơn/lượt):** lễ tân lập HĐ **lúc tiếp nhận** từ bảng giá (khám/tái khám/chỉ-CLS, gắn `appointmentId`), HĐ **thuốc riêng** sau khám, **thu trước/sau** linh hoạt, gom HĐ theo lượt. Chi tiết bảng: [service_prices.md](../database/service_prices.md), [invoices.md](../database/invoices.md), [invoice_items.md](../database/invoice_items.md). Mô hình: [ADR 0014](../adr/0014-mo-hinh-vien-phi-va-thu-ngan.md) (+ bổ sung P2).

**RBAC:** ghi/đọc = `Roles.ManageBilling` (**Admin + Lễ tân**). Bác sĩ/Dược sĩ **không thấy** dữ liệu tài chính (403).

**Tiền tệ:** `decimal`/`numeric(18,2)`, đơn vị **VND**, cộng tổng phía server. FE hiển thị định dạng `vi-VN`.

| Method | Path | Mô tả | Thành công |
|--------|------|-------|-----------|
| POST | `/api/service-prices` | Tạo dịch vụ (sinh mã `DV-`) | 201 / 400 |
| GET | `/api/service-prices?page=&pageSize=&search=&category=` | Danh sách + tìm kiếm (tên/mã) + lọc phân loại | 200 |
| GET | `/api/service-prices/{id}` | Chi tiết dịch vụ | 200 / 404 |
| PUT | `/api/service-prices/{id}` | Cập nhật dịch vụ | 200 / 400 / 404 |
| DELETE | `/api/service-prices/{id}` | Xoá mềm dịch vụ | 204 / 404 |
| POST | `/api/invoices/from-encounter/{encounterId}` | Lập **HĐ thuốc** từ phiếu khám (chỉ dòng thuốc đã cấp — Mô hình A) | 201 / 400 / 404 / 409 |
| POST | `/api/invoices/from-lab-order/{labOrderId}` | Lập **HĐ phí CLS** từ phiếu chỉ định (dòng `Paraclinical`, snapshot giá) | 201 / 400 / 404 / 409 |
| POST | `/api/invoices` | Tạo hoá đơn dịch vụ / lúc tiếp nhận (dòng `servicePriceId`, `appointmentId?`) | 201 / 400 / 404 |
| GET | `/api/invoices?page=&pageSize=&patientId=&appointmentId=&status=&from=&to=` | Danh sách + lọc (mới nhất trước) | 200 |
| GET | `/api/invoices/by-appointment/{appointmentId}` | Gom HĐ theo một lịch + tổng đã lập/đã thu/còn nợ | 200 |
| GET | `/api/invoices/by-visit/{visitId}` | Gom HĐ theo **lượt tiếp nhận** + tổng (ADR 0017) | 200 |
| POST | `/api/invoices/pay-visit/{visitId}` | **Thu tiền cả lượt** (mọi HĐ `Draft` của lượt, một phương thức) | 200 |
| GET | `/api/invoices/{id}` | Chi tiết hoá đơn + các dòng | 200 / 404 |
| PUT | `/api/invoices/{id}` | Sửa cụm dòng + ghi chú (chỉ `Draft`, snapshot lại giá) | 200 / 400 / 404 / 409 |
| POST | `/api/invoices/{id}/pay` | Thu tiền (`Draft → Paid`, chọn phương thức) | 200 / 404 / 409 |
| POST | `/api/invoices/{id}/cancel` | Huỷ (`Draft → Cancelled`) | 200 / 404 / 409 |
| POST | `/api/invoices/{id}/refund` | Hoàn tiền (`Paid → Refunded`, ghi lý do) — ADR 0022 | 200 / 400 / 404 / 409 |
| DELETE | `/api/invoices/{id}` | Xoá mềm (chỉ `Draft`) | 204 / 404 / 409 |

### Body & mã lỗi
```json
// POST /api/service-prices  (category: 0=Consultation, 1=Paraclinical, 2=Other — mặc định Other)
{ "name": "Khám tổng quát", "unitPrice": 150000, "description": null, "category": 0 }

// POST /api/invoices  (hoá đơn dịch vụ / lúc tiếp nhận — appointmentId tuỳ chọn)
{ "patientId": "…", "note": null, "appointmentId": "…",
  "items": [ { "servicePriceId": "…", "quantity": 1 } ] }

// POST /api/invoices/{id}/pay   — paymentMethod nhận CHUỖI (khác quy ước enum-số)
{ "paymentMethod": "Cash" }   // "Cash" | "Card" | "Transfer"
```
- **Enum trong phản hồi** (`status`, `paymentMethod`, `itemType`, `category`) serialize thành **số** (như `gender`/`appointmentStatus`): `InvoiceStatus` `Draft=0`/`Paid=1`/`Cancelled=2`; `PaymentMethod` `Cash=0`/`Card=1`/`Transfer=2`; `InvoiceItemType` `ServiceFee=0`/`Medication=1`/**`Paraclinical=2`**/`Other=3` (⚠️ `Paraclinical` chèn trước `Other` ở Sprint 15 → `Other` đổi 2→3, ADR 0015); `ServiceCategory` `Consultation=0`/`Paraclinical=1`/`Other=2`. **Ngoại lệ:** body `POST /pay` nhận `paymentMethod` **chuỗi** (khớp const-map FE).
- **Snapshot giá:** `invoice_items.unitPrice` copy tại thời điểm lập/sửa — đổi bảng giá/`salePrice` sau không ảnh hưởng hoá đơn cũ. `totalAmount` = Σ `lineTotal` (server tính).
- **Lập HĐ thuốc từ phiếu (Mô hình A — S14.5):** chỉ dựng dòng thuốc đã cấp (bỏ công khám); phiếu chưa `Completed` → 409 `Billing.EncounterNotCompleted`; không có dòng thuốc gắn danh mục → 400 `Billing.NoMedicationToInvoice`; đã lập HĐ thuốc → 409 `Billing.MedicationAlreadyInvoiced` (cờ `encounters.MedicationInvoicedAt`, thay unique `EncounterId` đã bỏ); thuốc tham chiếu không tồn tại → 404.
- **Lập lúc tiếp nhận:** `POST /api/invoices` nhận `appointmentId?` (kiểm tồn tại → 404 `Appointment.NotFound`); nhiều hoá đơn/lượt được phép (bỏ unique `EncounterId`). Thu **trước/sau** linh hoạt qua `Draft`+`Pay`.
- **Vòng đời:** thu tiền/sửa/huỷ/xoá sai trạng thái → 409 `Billing.InvalidTransition`. Hoá đơn `Paid` → chỉ chuyển sang `Refunded` (không xoá/sửa — sổ cái bất biến); `Refunded` bất biến.
- **Hoàn tiền (REF-01, ADR 0022):** `POST /api/invoices/{id}/refund` body `{ "reason": "…" }`; lý do rỗng → 400 `Billing.RefundReasonRequired`. DTO trả thêm `refundedAt`, `refundReason`. Báo cáo doanh thu chỉ tính `Paid`.
- Sinh mã `HD-`/`DV-` đếm `IgnoreQueryFilters()`.

## 7e. Endpoint — Cận lâm sàng (Paraclinical)

Nghiệp vụ CLS (Sprint 15–16, Epic 11): bác sĩ **chỉ định** dịch vụ CLS trong lúc khám, hoặc lễ tân
**đăng ký walk-in** (không qua khám, ADR 0016); **kỹ thuật viên/bác sĩ nhập kết quả**, xem kết quả trong
bệnh án, **in phiếu kết quả**; phí CLS lập **hoá đơn riêng** loại `Paraclinical` (qua §7d `from-lab-order`).
Chi tiết bảng: [lab_orders.md](../database/lab_orders.md), [lab_order_items.md](../database/lab_order_items.md).
Mô hình: [ADR 0015](../adr/0015-mo-hinh-can-lam-sang.md), [ADR 0016](../adr/0016-dang-ky-dich-vu-walkin-cls-va-ky-thuat-vien.md).

**RBAC:** chỉ định trong lúc khám = `Roles.RecordEncounter` (**Admin + Bác sĩ**); đăng ký walk-in =
`Roles.ManageStaff` (**Admin + Lễ tân**); **nhập kết quả/huỷ = `Roles.RecordLabResult`** (**Admin + Bác sĩ +
Kỹ thuật viên**, ADR 0016); **đọc** mở cho mọi vai trò đã đăng nhập. Lập hoá đơn phí CLS = `Roles.ManageBilling` (§7d).

| Method | Path | Mô tả | Thành công |
|--------|------|-------|-----------|
| POST | `/api/lab-orders` | Chỉ định CLS từ phiếu khám (sinh mã `CLS-`, snapshot tên/giá) | 201 / 400 / 404 / 409 |
| POST | `/api/lab-orders/walk-in` | Đăng ký CLS walk-in (không cần phiếu khám; lễ tân) | 201 / 400 / 404 |
| GET | `/api/lab-orders?page=&pageSize=&encounterId=&patientId=&status=` | Danh sách + lọc (mới nhất trước; kỹ thuật viên lọc `status=Ordered/InProgress`) | 200 |
| GET | `/api/lab-orders/{id}` | Chi tiết phiếu chỉ định + các mục | 200 / 404 |
| POST | `/api/lab-orders/{id}/items/{itemId}/result` | Nhập kết quả cho một mục (chuyển vòng đời) | 200 / 404 / 409 |
| POST | `/api/lab-orders/{id}/cancel` | Huỷ phiếu chỉ định (`→ Cancelled`) | 200 / 404 / 409 |

### Body & mã lỗi
```json
// POST /api/lab-orders
{ "encounterId": "…", "note": null, "items": [ { "servicePriceId": "…" } ] }

// POST /api/lab-orders/walk-in  (visitId? tuỳ chọn — gắn phiếu CLS vào lượt, ADR 0017)
{ "patientId": "…", "appointmentId": null, "visitId": null, "note": null, "items": [ { "servicePriceId": "…" } ] }

// POST /api/lab-orders/{id}/items/{itemId}/result
{ "resultText": "WBC 7.5 — bình thường", "conclusion": "Không bất thường" }
```
- **Enum phản hồi** serialize **số**: `LabOrderStatus` `Ordered=0`/`InProgress=1`/`Completed=2`/`Cancelled=3`;
  `LabOrderItemStatus` `Pending=0`/`Completed=1`.
- **Chỉ định** (đường bác sĩ) chỉ khi phiếu khám còn `Draft` → 409 `Paraclinical.EncounterNotDraft`; **walk-in**
  kiểm bệnh nhân (`Patient.NotFound` 404) + lịch nếu có (`Appointment.NotFound` 404) + lượt nếu có (`Visit.NotFound` 404). Cả hai: dịch vụ thiếu →
  404 `ServicePrice.NotFound`; dịch vụ không phải loại Paraclinical → 400 `Paraclinical.ServiceNotParaclinical`.
- **Nhập kết quả:** mục hoàn tất → phiếu `InProgress`, đủ mục → `Completed`; nhập sau khi
  `Completed`/`Cancelled` → 409 `Paraclinical.InvalidTransition`; mục không tồn tại → 404 `Paraclinical.ItemNotFound`.
- **Gating thanh toán (ADR 0021, PAY-01):** chưa thu phí CLS (`lab_orders.PaidAt` null) → nhập kết quả bị chặn
  409 `Paraclinical.NotPaid`. Cờ `PaidAt` đặt khi thu hoá đơn phí CLS (HĐ gắn `Invoice.LabOrderId`). DTO trả thêm `paidAt`.
- **Snapshot giá:** `lab_order_items.unitPrice` copy tại thời điểm chỉ định. **Lập HĐ CLS** (§7d
  `from-lab-order`) idempotent qua cờ `lab_orders.InvoicedAt` → lập lần 2 → 409 `Billing.ParaclinicalAlreadyInvoiced`.
- **In phiếu kết quả:** trang FE `/lab-orders/{id}/print` (print-friendly, `@media print`).
- Sinh mã `CLS-` đếm `IgnoreQueryFilters()`.

## 7f. Endpoint — Lượt tiếp nhận (Visit)

Gom **một lần bệnh nhân đến khám** với **nhiều dịch vụ khám** (mỗi dịch vụ là một `Appointment` con) —
xem [ADR 0017](../adr/0017-mo-hinh-luot-tiep-don-visit.md). Ghi = `Roles.ManageStaff` (Admin/Lễ tân);
**đọc** mở cho mọi vai trò.

| Method | Path | Mô tả |
|--------|------|-------|
| POST | `/api/visits` | Tạo lượt + 1..n dịch vụ khám (walk-in). |
| GET | `/api/visits?page=&pageSize=&patientId=&status=&date=` | Danh sách lượt (kèm `serviceCount`). |
| GET | `/api/visits/{id}` | Chi tiết: các dịch vụ + tổng `totalBilled`/`totalPaid`/`totalOutstanding`. |
| POST | `/api/visits/{id}/services` | Thêm một dịch vụ khám vào lượt còn `Open`. |
| POST | `/api/visits/{id}/close` · `/cancel` | Đóng / huỷ lượt. |

### Body & mã lỗi
```json
// POST /api/visits  (paraclinicalServiceIds? — chỉ định CLS ngay lúc tiếp nhận)
{ "patientId": "…", "note": null,
  "services": [
    { "doctorId": "…", "startTime": "2026-09-10T01:00:00Z", "endTime": "2026-09-10T01:30:00Z", "reason": null, "servicePriceId": "…" }
  ],
  "paraclinicalServiceIds": ["…", "…"] }

// POST /api/visits/{id}/services
{ "doctorId": "…", "startTime": "…", "endTime": "…", "reason": null, "servicePriceId": null }
```
- **Enum phản hồi** serialize **số**: `VisitStatus` `Open=0`/`Closed=1`/`Cancelled=2`. Chi tiết lượt trả kèm
  `appointments` (dịch vụ khám) và `labOrders` (phiếu CLS gắn lượt — tóm tắt: mã/trạng thái/phí/`invoicedAt`).
- **Chỉ định CLS lúc tiếp nhận:** `paraclinicalServiceIds` (tuỳ chọn) — mỗi Id là một dịch vụ loại `Paraclinical`;
  khi tạo lượt sẽ tạo **một phiếu CLS walk-in** (`LabOrder`) gắn lượt, cùng một `SaveChanges`. Dịch vụ sai loại
  → 400 `Paraclinical.ServiceNotParaclinical`; thiếu → 404 `ServicePrice.NotFound`.
- Không có **cả** dịch vụ khám lẫn CLS → 400 `Visit.NoServices` (cho phép lượt **chỉ CLS**); bệnh nhân thiếu → 400
  `Visit.PatientNotFound`; bác sĩ thiếu → 400 `Visit.DoctorNotFound`; dịch vụ khám không phải `Consultation` → 400
  `Appointment.ServiceNotConsultation`; trùng giờ cùng bác sĩ (kể cả nội bộ lượt) → 409 `Appointment.Overlap`.
- Thêm dịch vụ khi lượt không mở → 409 `Visit.NotOpen`; đóng/huỷ sai vòng đời → 409 `Visit.InvalidTransition`.
- **Gom viện phí:** hoá đơn mang `Invoice.VisitId` **suy từ lịch khám gắn HĐ** lúc lập (ADR 0017); chi tiết lượt gom theo `VisitId` (union với `AppointmentId` để tương thích HĐ cũ). HĐ `Cancelled` không tính `totalBilled`. **Thu tiền cả lượt** + gom HĐ ở §7d (`by-visit`/`pay-visit`).
- Sinh mã `LK-` đếm `IgnoreQueryFilters()`.

## 7g. Endpoint — Phòng khám & Lịch làm việc bác sĩ (Resources, ADR 0018)

| Method | Endpoint | Ghi chú |
|--------|----------|---------|
| GET | `/api/rooms?page=&pageSize=&search=` | Danh sách phòng (đọc mọi vai trò). |
| GET | `/api/rooms/{id}` | Chi tiết phòng. |
| POST · PUT · DELETE | `/api/rooms` · `/{id}` | CRUD phòng — `Roles.ManageStaff`. |
| GET | `/api/doctors/{id}/schedules` | Lịch làm việc (mẫu tuần) của bác sĩ. |
| POST · PUT · DELETE | `/api/doctors/{id}/schedules` · `/{scheduleId}` | CRUD khung làm việc — `Roles.ManageStaff`. |

### Body & mã lỗi
```json
// POST /api/rooms
{ "name": "Phòng 101", "description": "Tầng 1" }

// POST /api/doctors/{id}/schedules  (dayOfWeek: 0=CN..6=Thứ 7; time "HH:mm:ss")
{ "dayOfWeek": 1, "startTime": "08:00:00", "endTime": "12:00:00", "roomId": null }
```
- Mã phòng `PK-` sinh tự động (đếm `IgnoreQueryFilters()`). Ngừng dùng phòng = xoá mềm (không có `IsActive`).
- Khung làm việc chồng nhau cùng bác sĩ/thứ → 409 `Doctor.ScheduleOverlap`; phòng không tồn tại → 404 `Room.NotFound`; `endTime ≤ startTime` → 400.
- **Ràng buộc đặt lịch:** `POST/PUT /api/appointments` nhận thêm `roomId?`; giờ khám (UTC, quy đổi UTC+7) phải nằm trong một khung làm việc của bác sĩ hôm đó → nếu không: 409 `Appointment.OutsideWorkingHours`. Bác sĩ **chưa khai lịch nào** ⇒ không ràng buộc (tương thích lịch cũ). `AppointmentDto` trả thêm `roomId`/`roomName`.

## 7h. Endpoint — Sinh hiệu & Hàng đợi (Điều dưỡng, ADR 0019)

**Sinh hiệu** — gắn lượt khám (1–1). Ghi = `Roles.RecordVitals` (Admin/Điều dưỡng); đọc mở cho lâm sàng (bác sĩ xem trong bệnh án).

| Method | Path | Quyền | Mô tả |
|--------|------|-------|-------|
| POST | `/api/appointments/{id}/vitals` | RecordVitals | Nhập/cập nhật (upsert) sinh hiệu cho lượt khám. |
| GET | `/api/appointments/{id}/vitals` | đã đăng nhập | Lấy sinh hiệu; `data=null` nếu chưa đo. |

```json
// POST /api/appointments/{id}/vitals — mọi chỉ số tuỳ chọn; BMI backend tính (không nhận từ client)
{ "heightCm": 170, "weightKg": 68, "temperatureC": 37, "pulse": 78,
  "bloodPressureSystolic": 120, "bloodPressureDiastolic": 80, "spO2": 98,
  "respiratoryRate": 18, "notes": "ổn định" }
```
- `VitalsDto` trả thêm `bmi` (tính) + `measuredByName`. Lượt không tồn tại → 404 `Appointment.NotFound`.

**Hàng đợi** — số thứ tự theo ngày. Ghi = `Roles.ManageQueue` (Admin/Lễ tân/Điều dưỡng); đọc mở cho mọi vai trò.

| Method | Path | Mô tả |
|--------|------|-------|
| POST | `/api/queue` | Lấy số (hỗ trợ vãng lai — `appointmentId` null). |
| GET | `/api/queue?date=&roomId=&doctorId=&status=` | Danh sách vé (mặc định hôm nay, sắp theo số). |
| POST | `/api/queue/{id}/assign` | Gán/đổi phòng & bác sĩ (điều phối). |
| POST | `/api/queue/{id}/{call\|start\|done\|skip}` | Chuyển trạng thái vé. |

```json
// POST /api/queue
{ "patientId": "…", "appointmentId": null, "roomId": null, "doctorId": null }
```
- Cấp số tuần tự trong ngày (`Number`, phạm vi toàn phòng khám). Máy trạng thái `Waiting→Called→InProgress→Done`; `Waiting/Called→Skipped`; chuyển sai → 409 `Queue.InvalidTransition`.
- Bệnh nhân/lịch/phòng/bác sĩ không tồn tại → `Queue.PatientNotFound` (400) / `Appointment.NotFound` / `Room.NotFound` / `Doctor.NotFound` (404).

## 7i. Endpoint — Báo cáo & Thống kê vận hành (Reporting, ADR 0020)

Chỉ-đọc, tổng hợp phía server từ bảng hiện có (không thêm bảng nghiệp vụ). Tôn trọng soft-delete. "Hôm nay"
và việc gom theo ngày dùng **múi giờ phòng khám UTC+7** (thống nhất Sprint 14–19). Ghi/đọc = `Roles.ManageStaff`
(Admin/Lễ tân); riêng năng suất bác sĩ mở thêm cho Bác sĩ nhưng **ép chỉ xem của mình**.

| Method | Path | Mô tả |
|--------|------|-------|
| GET | `/api/reports/overview` | R-01 · KPI hôm nay (Admin/Lễ tân). |
| GET | `/api/reports/appointments?from=&to=&doctorId=` | R-02 · Lịch khám gom theo ngày × trạng thái (Admin/Lễ tân). |
| GET | `/api/reports/by-doctor?from=&to=` | R-03 · Năng suất theo bác sĩ (Admin/Lễ tân/Bác sĩ; Bác sĩ ép `doctorId` của mình). |
| GET | `/api/reports/revenue?from=&to=` | R-04 · Doanh thu theo ngày × loại khoản mục, chỉ HĐ `Paid` (Admin/Lễ tân). |

- **Khoảng ngày** (`from`,`to` kiểu `yyyy-MM-dd`): thiếu → mặc định **7 ngày gần nhất**. `from > to` → 400
  `Report.InvalidRange`; khoảng > 366 ngày → 400 `Report.RangeTooWide`. Danh sách `days` trả **đủ mọi ngày**
  trong khoảng (kể cả ngày 0).
- `OverviewDto`: `totalActivePatients`, `totalDoctors`, `encountersToday`, `revenueToday`, `queueWaiting`,
  `appointmentsToday`, `appointmentsByStatus` (phân rã: `scheduled/checkedIn/inProgress/completed/cancelled/noShow`).
- `AppointmentReportDto`: `from`,`to`,`total`,`days[]` (mỗi ngày: `date`,`total`,`byStatus`).
- `DoctorProductivityDto[]`: `doctorId`,`doctorCode`,`doctorName`,`totalAppointments`,`completedAppointments`,`encounters`.
- `RevenueReportDto`: tổng `grandTotal` + tách `serviceFeeTotal`/`medicationTotal`/`paraclinicalTotal`/`otherTotal`;
  `days[]` (mỗi ngày: `date`,`total` + 4 loại). Chỉ tính hoá đơn đã thanh toán (theo `paidAt`).

```
GET /api/reports/revenue?from=2026-09-01&to=2026-09-07   (Authorization: Bearer <token Admin/Lễ tân>)
```

## 8. Vận hành

| Method | Path | Mô tả |
|--------|------|-------|
| GET | `/health` | Health check; trả `Healthy` (200) khi DbContext kết nối được CSDL. |

- Logging có cấu trúc qua **Serilog** (console sink); mỗi request được ghi tóm tắt (`UseSerilogRequestLogging`).
