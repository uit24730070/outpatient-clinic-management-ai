# Trợ lý AI / LLM — cấu hình & vận hành (DOC-18)

Tài liệu ngắn về cách tích hợp LLM (Sprint 6). Quyết định kiến trúc đầy đủ: [ADR 0007](../adr/0007-tich-hop-llm.md).

## Kiến trúc (Clean Architecture)
- **Application** định nghĩa abstraction trung lập provider: `Common/Ai/IChatCompletionService` + DTO (`ChatMessage`, `ChatCompletionRequest`, `ChatCompletionResult`). Domain/Application **không** biết OpenAI.
- **Infrastructure** hiện thực:
  - `Ai/OpenAiChatCompletionService` — gọi OpenAI API (model GPT-5 mini) qua **HTTP thuần** (`POST /v1/chat/completions`, header `Authorization: Bearer`), DTO wire-format là `private` bên trong. GPT-5 dùng `max_completion_tokens` + `reasoning_effort`, không nhận `temperature`.
  - `Ai/FakeChatCompletionService` — tóm tắt tất định, **không gọi mạng**.
  - DI chọn hiện thực theo cấu hình: client thật **chỉ khi** `!UseFake && ApiKey` có giá trị; ngược lại fake.
- **WebApi**: `POST /api/patients/{id}/ai-summary` (Bác sĩ/Admin). Service `Application/Ai/PatientSummaryService` nạp ≤10 phiếu khám gần nhất, dựng prompt, gọi client.

## Cấu hình (section `Ai` → `AiSettings`)
| Khoá | Mặc định | Ý nghĩa |
|------|----------|---------|
| `ApiKey` | `""` | Khoá API provider. **Không commit giá trị thật.** |
| `Model` | `gpt-5-mini` | Model đang dùng (đổi qua cấu hình). |
| `BaseUrl` | `https://api.openai.com` | Điểm cuối (không dấu `/` cuối). |
| `ReasoningEffort` | `low` | Mức suy luận GPT-5 (`reasoning_effort`): none/low/medium/high/xhigh. Trống ⇒ không gửi. |
| `MaxTokens` | `2048` | Giới hạn token đầu ra (`max_completion_tokens`, **gồm cả token suy luận**). |
| `TimeoutSeconds` | `120` | Thời gian chờ tối đa (model suy luận có thể chậm hơn). |
| `UseFake` | `false` (prod) / `true` (Development) | Bật chế độ giả lập. |

## Bảo mật khoá (giống `Jwt:Key`)
- `appsettings.json` để **`ApiKey` trống**. Giá trị thật đặt qua **biến môi trường/secret**:
  - PowerShell: `$env:Ai__ApiKey = "sk-ant-..."` rồi `dotnet run ...` (dùng dấu `__` cho cấp lồng).
  - hoặc `dotnet user-secrets set "Ai:ApiKey" "sk-ant-..."`.
- **Không commit khoá.** `.gitignore` đã loại `.env`; kiểm `git diff` trước khi commit.

## Chạy thử
- **Không khoá / dev:** `Ai:UseFake = true` (mặc định Development) → endpoint trả tóm tắt mô phỏng, không tốn phí.
- **Có khoá thật:** đặt `Ai__ApiKey`, đặt `Ai__UseFake=false` (hoặc chạy môi trường khác Development), gọi `POST /api/patients/{id}/ai-summary`.
- Lỗi khi tắt/thiếu khoá → envelope `Ai.*` (500), **không crash** (xem conventions §6c).

## RAG — truy hồi trên bệnh án (Sprint 7, AI-02 phần 2)
Quyết định đầy đủ: [ADR 0008](../adr/0008-rag-va-vector-store.md).

Sơ đồ luồng hỏi đáp (`POST /api/patients/{id}/ai-ask`):
```
câu hỏi ──embed(query)──▶ vector truy vấn
                              │
        pgvector: ORDER BY embedding <=> query  (cosine)
                              │  top-5 phiếu của bệnh nhân
   nạp nội dung phiếu ◀───────┘
        │  dựng prompt kèm "Nguồn N"
        ▼
   IChatCompletionService (OpenAI)  ──▶ câu trả lời + danh sách nguồn
```

Pipeline embedding (`AI-04`) — sinh khi tạo/sửa/chốt phiếu (best-effort, không chặn ghi phiếu):
```
phiếu khám ──văn bản gộp──▶ IEmbeddingService (Voyage/fake) ──▶ vector(1024)
                                                           │
                                    IEncounterEmbeddingStore.Upsert (pgvector)
```

### Thành phần
- **Application (trung lập provider):** `Common/Ai/IEmbeddingService` (+ `EmbeddingRequest/Result`), `Common/Ai/IEncounterEmbeddingStore` (Upsert/Search). Service `Ai/EncounterEmbeddingIndexer` (sinh & lưu embedding) và `Ai/PatientQuestionService` (truy hồi + hỏi đáp).
- **Infrastructure (SDK/pgvector chỉ ở đây):** `Ai/VoyageEmbeddingService` (HTTP `POST /v1/embeddings`) / `Ai/FakeEmbeddingService` (vector tất định); `Persistence/PgEncounterEmbeddingStore` (SQL thô, toán tử `<=>`).
- **Domain:** entity `Ai/EncounterEmbedding` (`float[]`, EF-agnostic) → map `vector(1024)` ở Infrastructure.

### Cấu hình embedding (section `Ai`)
| Khoá | Mặc định | Ý nghĩa |
|------|----------|---------|
| `EmbeddingApiKey` | `""` | Khoá Voyage AI. **Không commit.** Đặt qua `Ai__EmbeddingApiKey`. |
| `EmbeddingModel` | `voyage-3` | Model embedding (1024 chiều). |
| `EmbeddingBaseUrl` | `https://api.voyageai.com` | Điểm cuối provider embedding. |
| `UseFakeEmbedding` | `false` (prod) / `true` (Dev) | Bật fake embedding (không gọi mạng). |

### Vận hành
- **Docker:** cần image có pgvector — `pgvector/pgvector:pg16` (thay `postgres:16-alpine`). Migration `AddEncounterEmbeddings` tự `CREATE EXTENSION vector`.
- **Backfill:** `POST /api/ai/reindex` (Admin) sau khi bật khoá embedding lần đầu / đổi model.
- **Test/CI offline:** stub embedding + in-memory store + stub chat — không gọi mạng, không cần pgvector.

## Chatbot nghiệp vụ — tool-calling (Sprint 10, AI-03)
Quyết định đầy đủ: [ADR 0010](../adr/0010-chatbot-tool-calling.md).

Trợ lý hội thoại đa lượt để LLM **tự truy vấn dữ liệu** qua các công cụ chỉ-đọc thay vì nhồi sẵn prompt.

```
người dùng hỏi ──▶ AssistantService (Application)
       │  gửi hội thoại + danh sách công cụ
       ▼
IAssistantCompletionService (OpenAI tool-calling / fake)
       │  finish_reason = tool_calls?  ─yes─▶ thực thi IAssistantTool (RBAC) ──┐
       │                                                                     │ (≤5 vòng)
       └──no──▶ câu trả lời cuối + danh sách công cụ đã gọi  ◀── tool_result ┘
```

### Thành phần
- **Application (trung lập provider):** `Common/Ai/AiTool`/`AiToolParameter` (định nghĩa công cụ), `AssistantMessage`/`AssistantContent` (hội thoại đa khối), `IAssistantCompletionService` (một lượt gọi có tool-use), `IAssistantTool` + `AssistantContext` (RBAC). Orchestrator `Assistant/AssistantService` (vòng lặp, giới hạn `MaxToolRounds=5`). Bộ công cụ `Assistant/Tools/*` bọc service sẵn có.
- **Infrastructure (định dạng OpenAI chỉ ở đây):** `Ai/OpenAiAssistantCompletionService` (dịch `AiTool`→`tools[].function`, parse `tool_calls`, gửi kết quả bằng message `role=tool`+`tool_call_id`, HTTP thuần) / `Ai/FakeAssistantCompletionService` (tất định, không gọi công cụ).
- **WebApi:** `POST /api/assistant/chat` (mọi vai trò đã đăng nhập); dựng `AssistantContext` từ claim + `doctorId` tra qua `IAuthService`.

### Bộ công cụ chỉ-đọc & kiểm soát quyền
| Công cụ | Bọc service | Phạm vi theo vai trò |
|---|---|---|
| `search_patients` | `IPatientService` | mọi vai trò |
| `list_doctors` | `IDoctorService` | mọi vai trò |
| `list_appointments` | `IAppointmentService` | Bác sĩ ép `doctorId` của mình; Lễ tân/Admin xem tất cả |
| `get_patient_encounters` | `IEncounterService` | Bác sĩ ép `doctorId` của mình; Lễ tân/Admin xem tất cả |

- **Không** có công cụ ghi; **không** truyền SQL thô. Vượt số vòng → `Ai.ToolLoopExceeded` (500). Câu rỗng → `Ai.QuestionRequired` (400).
- Dev bật `Ai:UseFake = true` → trả lời tất định, không gọi mạng, không lặp công cụ.

## Giới hạn hiện tại
- Số chiều embedding cố định **1024** (khớp `voyage-3`); đổi model khác chiều ⇒ đổi cột + migration + backfill.
- Truy hồi quét tuần tự (chưa index ANN `ivfflat`/`hnsw`) — đủ ở quy mô đồ án.
- Prompt chứa **dữ liệu bệnh án nhạy cảm** (cả tóm tắt lẫn RAG) — cân nhắc quyền riêng tư/nhật ký khi bật khoá thật.
- Kết quả AI là **tham khảo**, không thay chẩn đoán (có disclaimer ở UI).
- Chatbot chưa có công cụ **ghi**, chưa streaming, chưa lưu lịch sử hội thoại (FE gửi lại toàn bộ mỗi lượt).
