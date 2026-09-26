# 0007. Tích hợp LLM (Trợ lý AI)

- Trạng thái: Accepted
- Ngày: 2026-08-08

> **Cập nhật 2026-09-12 — đổi provider sang OpenAI GPT-5 mini (tuân thủ đề cương).**
> Đề cương chốt provider AI là **OpenAI API, model `gpt-5-mini`**. Đổi hiện thực ở Infrastructure từ
> Claude sang OpenAI **mà không đụng abstraction** ở Application (`IChatCompletionService`/
> `IAssistantCompletionService` giữ nguyên — đây chính là lợi ích thiết kế đã nêu ADR này):
> - Client: `OpenAiChatCompletionService` + `OpenAiAssistantCompletionService` gọi
>   `POST /v1/chat/completions` (header `Authorization: Bearer`), thay `ClaudeChatCompletionService`/
>   `ClaudeAssistantCompletionService` (đã xoá).
> - Ràng buộc riêng của model GPT-5: dùng **`max_completion_tokens`** (không phải `max_tokens`),
>   **không nhận `temperature`** tuỳ biến (bỏ qua), thêm **`reasoning_effort`** (mặc định `low`).
>   `AiSettings`: `Model=gpt-5-mini`, `BaseUrl=https://api.openai.com`, bỏ `AnthropicVersion`, thêm
>   `ReasoningEffort`; `MaxTokens` mặc định 2048 (gồm cả token suy luận), `TimeoutSeconds` 120.
> - Tool-calling (AI-03) dịch sang định dạng `tools[].function` của OpenAI; kết quả công cụ gửi lại
>   bằng message `role=tool` + `tool_call_id`. Fake/stub và RBAC không đổi. 187 unit test vẫn xanh.
> - **Embedding (RAG) vẫn dùng Voyage AI** — nằm ngoài phạm vi tóm tắt của đề cương; đổi sang OpenAI
>   embeddings sẽ kéo theo đổi số chiều cột `vector` + reindex, để phần nợ.

## Bối cảnh
Sprint 6 mở Epic 6 — **Trợ lý AI**: đưa LLM đầu tiên vào hệ thống và hiện thực tính năng có giá trị lâm sàng đầu tiên là **tóm tắt lịch sử khám** của một bệnh nhân từ dữ liệu bệnh án (Sprint 5). Đây là nền cho RAG/chatbot ở các sprint sau.

Cần chốt: (1) provider + model; (2) vị trí và hình dạng abstraction để không rò SDK/HTTP của provider ra Application/Domain; (3) cách hiện thực client; (4) cấu hình & bảo mật khoá; (5) chế độ giả lập (fake) để test/dev không tốn phí và không gọi mạng; (6) xử lý lỗi & mã. Giữ nguyên Clean Architecture, envelope `ApiResponse`, nền RBAC (Sprint 3).

## Các phương án & quyết định

### 1. Provider & model
- **Chọn Claude API (Anthropic), model `claude-opus-4-8`** (model Claude mới nhất tại thời điểm sprint). Endpoint `POST /v1/messages`, header `x-api-key` + `anthropic-version`.
- Model đặt qua cấu hình (`Ai:Model`) để đổi không cần build lại. Provider/model đổi nhanh — ghi rõ id đang dùng ở đây.

### 2. Vị trí & hình dạng abstraction
- **Interface trung lập provider đặt ở Application:** `Common/Ai/IChatCompletionService` với DTO trung lập (`ChatMessage` gồm `ChatRole` + nội dung, `ChatCompletionRequest`, `ChatCompletionResult`). Trả `Result<T>` (không ném exception).
- **Hiện thực + wire-format của provider chỉ ở Infrastructure.** Application/Domain **không** biết Anthropic. Cùng khuôn với Sprint 3 (`IJwtTokenGenerator`/`IPasswordHasher` ở Application, hiện thực ở Infrastructure).
- `ChatCompletionRequest` để trống `Model`/`MaxTokens` → hiện thực điền mặc định từ cấu hình. Application không cần biết các tham số riêng của provider.

### 3. Hiện thực client
- **Chọn HTTP thuần (`HttpClient` + `System.Text.Json`)** thay vì thêm SDK NuGet. Lý do: chỉ cần **một** lệnh gọi đơn giản (`/v1/messages`); **không thêm dependency**, build/test **offline chắc chắn chạy**; thoả mãn "SDK/HTTP chỉ ở Infrastructure" một cách hiển nhiên. DTO khớp wire-format là `private` bên trong `ClaudeChatCompletionService`.
- Đăng ký `HttpClient` giữ **một instance dùng lại** toàn vòng đời (tránh cạn socket), không dùng `AddHttpClient` để khỏi thêm gói `Microsoft.Extensions.Http`.

### 4. Cấu hình & bảo mật khoá
- Section **`Ai`** (`AiSettings`): `ApiKey`, `Model`, `BaseUrl`, `AnthropicVersion`, `MaxTokens`, `TimeoutSeconds`, `UseFake`.
- **`ApiKey` để trống ở `appsettings.json`** (như `Jwt:Key`); giá trị thật đặt qua **biến môi trường/secret** (`Ai__ApiKey`). Không commit khoá.
- `appsettings.Development.json` bật `Ai:UseFake = true` → dev không cần khoá, không tốn phí.

### 5. Chế độ giả lập (fake)
- **`FakeChatCompletionService`** trả tóm tắt **tất định**, không gọi mạng. Chọn hiện thực tại DI: dùng client thật **chỉ khi** `!UseFake && ApiKey` có giá trị (`AiSettings.IsRealClientConfigured`); ngược lại dùng fake.
- Unit test dùng **stub `IChatCompletionService`** (ghi lại request, trả kết quả/lỗi cấu hình sẵn) — kiểm dựng prompt đúng + ánh xạ lỗi, **không gọi mạng**.

### 6. Xử lý lỗi & mã
- Lỗi gói vào `Error` mã **`Ai.*`**: `Ai.Unavailable` (thiếu khoá/không kết nối/HTTP lỗi), `Ai.Timeout` (quá thời gian chờ), `Ai.BadResponse` (phản hồi rỗng/không parse được). Đều `ErrorType.Failure` → **HTTP 500** trong envelope `ApiResponse` (client hiển thị thông điệp thân thiện). Không crash khi AI tắt/thiếu khoá.

### 7. Tính năng tóm tắt (AI-02 phần 1)
- Endpoint **`POST /api/patients/{id}/ai-summary`**, RBAC **Bác sĩ/Admin** (`Roles.RecordEncounter` — đọc bệnh án là hành vi lâm sàng, giống ghi phiếu).
- `PatientSummaryService` nạp **tối đa 10 phiếu khám gần nhất** (context stuffing — **chưa RAG**), dựng prompt tiếng Việt (triệu chứng, chẩn đoán, đơn thuốc, mốc thời gian), gọi `IChatCompletionService`. Bệnh nhân không tồn tại → `Patient.NotFound` (404); **chưa có phiếu → không gọi LLM**, trả thông điệp phù hợp (tiết kiệm chi phí).

## Hệ quả
- **Ưu:** mọi tính năng AI sau dựa lên một abstraction sạch; đổi provider/model qua cấu hình; test/CI không gọi mạng, không tốn phí; không thêm dependency; kiến trúc sạch được giữ.
- **Nhược/đánh đổi:**
  - Tự viết wire-format JSON (thay vì SDK) — chấp nhận vì bề mặt gọi nhỏ; nếu mở rộng nhiều tính năng (tool use, streaming) có thể cân nhắc SDK chính thức sau.
  - **Context stuffing** chỉ hợp với ít phiếu; dữ liệu lớn cần **RAG** (embeddings + vector store) — để sprint sau (AI-02 phần 2).
  - Prompt chứa **dữ liệu bệnh án nhạy cảm** — giới hạn N phiếu, chỉ gửi trường cần thiết; cân nhắc quyền riêng tư/nhật ký khi bật khoá thật.
  - Tóm tắt là **hỗ trợ tham khảo**, không thay chẩn đoán — có disclaimer ở UI; sprint này không tự sinh chẩn đoán/đơn.
  - Mã `Ai.*` ánh xạ 500 (envelope chưa có 503) — chấp nhận ở quy mô đồ án.
