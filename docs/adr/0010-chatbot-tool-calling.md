# 0010. Chatbot nghiệp vụ bằng tool-calling (LLM tự truy vấn qua công cụ chỉ-đọc)

- Trạng thái: Accepted
- Ngày: 2026-08-10

## Bối cảnh
Sprint 6–7 đã có trợ lý AI nhưng **bó theo từng bệnh nhân**: tóm tắt bệnh án (context stuffing — [ADR 0007](0007-tich-hop-llm.md)) và hỏi đáp RAG trên phiếu khám (embeddings + pgvector — [ADR 0008](0008-rag-va-vector-store.md)). Mỗi màn một nút AI rời rạc, chỉ chạm được **một loại dữ liệu** đã nạp sẵn vào prompt.

Sprint 10 cần một **chatbot hội thoại toàn cục** cho nhân viên (Lễ tân/Bác sĩ/Admin): hỏi tự do, nhiều lượt, về **nhiều loại dữ liệu nghiệp vụ** (bệnh nhân, lịch khám, bác sĩ, lịch sử khám). Nhồi sẵn mọi dữ liệu vào prompt là bất khả thi (khối lượng lớn, không biết trước cần gì). Cần cơ chế để **LLM tự quyết định lấy dữ liệu nào, khi nào** — nhưng phải **an toàn** (không cho chạm DB tuỳ ý) và **đúng quyền** (không vượt RBAC).

## Các quyết định

### 1. Tool-calling thay vì chỉ RAG
- Cho LLM một tập **công cụ (tool/function) chỉ-đọc**; LLM sinh yêu cầu gọi công cụ, hệ thống thực thi rồi trả kết quả để LLM chốt câu trả lời. Hợp bài toán "hỏi có cấu trúc trên nhiều thực thể" hơn RAG thuần (RAG mạnh cho truy hồi văn bản tự do — vẫn giữ ở luồng hỏi đáp bệnh án).
- **Không** cho công cụ **ghi** ở sprint này (tạo lịch/sửa dữ liệu qua chat) — cần thẩm định an toàn + xác nhận người dùng. Cũng **không** truyền SQL thô/tham số tự do xuống DB: LLM chỉ chạm dữ liệu qua **registry công cụ có kiểm soát**. Đây là ranh giới bảo mật quan trọng nhất.

### 2. Trừu tượng trung lập provider ở Application (bám ADR 0007)
- Thêm ở `Common/Ai`: `AiTool`/`AiToolParameter` (định nghĩa công cụ trung lập — tên, mô tả, tham số, không phải JSON Schema thô); mô hình hội thoại đa khối `AssistantMessage` + `AssistantContent` (`AssistantText`/`AssistantToolUse`/`AssistantToolResult`); `IAssistantCompletionService` (một lượt gọi LLM có tool-use, trả các khối nội dung + cờ `StopIsToolUse`).
- **Chi tiết định dạng tool-use của Claude chỉ ở Infrastructure:** `ClaudeAssistantCompletionService` dịch `AiTool` → `input_schema` (JSON Schema) và parse các khối `text`/`tool_use`/`stop_reason` qua HTTP thuần (`POST /v1/messages`, `System.Text.Json.Nodes`, **không thêm SDK**). `FakeAssistantCompletionService` trả lời tất định, **không** yêu cầu công cụ (không gọi mạng, không lặp).
- Giữ `IChatCompletionService` cũ **nguyên vẹn** (tóm tắt/RAG vẫn dùng) — abstraction tool-use là interface **mới**, tránh phá vỡ test/luồng sẵn có.

### 3. Orchestration + giới hạn vòng lặp ở Application
- `AssistantService` điều phối: gửi hội thoại + danh sách công cụ → nếu LLM đòi công cụ thì thực thi qua registry, nối kết quả (`tool_result`) rồi lặp lại; dừng khi LLM trả câu trả lời cuối.
- **Chặn lặp vô hạn/chi phí phình to:** tối đa `MaxToolRounds = 5` vòng gọi công cụ; vượt → `Ai.ToolLoopExceeded` (Failure→500). Công cụ lỗi/không tồn tại → trả thông điệp lỗi cho LLM thay vì ném (không crash hội thoại). Kết quả trả về gồm **danh sách công cụ đã gọi** để truy vết.

### 4. Kiểm soát quyền khi thực thi công cụ (RBAC)
- Mỗi công cụ nhận `AssistantContext(UserId, Role, DoctorId?)` — dựng ở controller từ claim + tra `doctorId` server-side qua `IAuthService` (tái dùng cơ chế [ADR 0009](0009-phan-quyen-va-trai-nghiem-giao-dien-theo-vai-tro.md), **không** nhét claim JWT).
- **Bác sĩ bị thu hẹp phạm vi "của mình":** `list_appointments`/`get_patient_encounters` ép `doctorId = context.DoctorId` (bỏ qua doctorId do LLM/người dùng truyền); bác sĩ **chưa gắn hồ sơ** → công cụ trả thông điệp rõ, không lộ dữ liệu. Lễ tân/Admin xem toàn bộ. Bộ công cụ khởi đầu: `search_patients`, `list_doctors`, `list_appointments`, `get_patient_encounters` — đều **bọc service Application sẵn có** (không truy DB trực tiếp), trả JSON gọn.
- Endpoint `POST /api/assistant/chat` cho **mọi vai trò đã đăng nhập**; phạm vi dữ liệu do lớp công cụ chốt, không phải bởi `[Authorize(Roles=...)]`.

## Hệ quả
- **Tích cực:** một trợ lý hội thoại duy nhất trả lời nhiều loại câu hỏi nghiệp vụ, tự lấy dữ liệu đúng quyền; abstraction trung lập provider (đổi provider chỉ sửa Infrastructure); Fake cho phép test/dev tất định không tốn phí; dễ mở rộng thêm công cụ (chỉ thêm `IAssistantTool` + đăng ký DI).
- **Đánh đổi:** multi-turn + tool-calling tốn token/độ trễ hơn (mặc định Dev dùng Fake; có `MaxTokens`/timeout như Sprint 6–7). RBAC nằm ở **từng công cụ** — mỗi công cụ mới phải tự cân nhắc phạm vi (đã kiểm bằng unit test). FE gửi lại toàn bộ lịch sử mỗi lượt (chưa lưu hội thoại vào DB).
- **Còn nợ:** công cụ **ghi** (tạo/sửa qua chat, cần xác nhận), streaming token, lưu lịch sử hội thoại, và mở rộng bộ công cụ (thống kê/báo cáo) — để sprint sau.
