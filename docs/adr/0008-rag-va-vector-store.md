# 0008. RAG & vector store cho Trợ lý AI (AI-02 phần 2)

- Trạng thái: Accepted
- Ngày: 2026-08-09

## Bối cảnh
Sprint 6 ([ADR 0007](0007-tich-hop-llm.md)) đưa LLM vào hệ thống với tính năng **tóm tắt bệnh án** theo lối **context stuffing** — nhồi ≤10 phiếu khám gần nhất vào prompt. Cách này chỉ hợp với ít phiếu; bệnh nhân có nhiều phiếu sẽ vượt ngân sách ngữ cảnh và giảm chất lượng.

Sprint 7 nâng lên **RAG** (Retrieval-Augmented Generation): sinh **embedding** cho từng phiếu khám, lưu vào **vector store**, và **truy hồi** các phiếu liên quan nhất trước khi gọi LLM. Cần chốt: (1) provider embedding + abstraction; (2) vector store; (3) chiến lược chunk; (4) thời điểm re-embed; (5) chế độ fake để test không gọi mạng; (6) mã lỗi. Giữ nguyên Clean Architecture, envelope `ApiResponse`, RBAC.

## Các quyết định

### 1. Abstraction embedding — ở Application, trung lập provider
- `Common/Ai/IEmbeddingService` với DTO `EmbeddingRequest` (danh sách văn bản + `EmbeddingInputType` document/query), `EmbeddingResult` (danh sách `float[]` + model). Trả `Result<T>`, **không** ném exception.
- **Provider embedding: Voyage AI** (mặc định, khuyến nghị chính thức của Anthropic — Claude API **không có** endpoint embedding). Model mặc định `voyage-3` (1024 chiều). Provider/model đổi qua cấu hình; abstraction không lộ ra Application.
- Cùng khuôn Sprint 6: SDK/HTTP **chỉ ở Infrastructure** (`Ai/VoyageEmbeddingService` gọi `POST /v1/embeddings` bằng HTTP thuần, header `Authorization: Bearer`), `Ai/FakeEmbeddingService` sinh vector **tất định** (gieo mầm SHA-256 → chuẩn hoá đơn vị), không gọi mạng.

### 2. Vector store — pgvector trên PostgreSQL sẵn có
- **Chọn `pgvector`** thay vì thêm hạ tầng vector DB riêng: tái dùng PostgreSQL đang chạy, là RAG "thật" (truy hồi cosine trong DB), phù hợp đồ án và tái dùng cho AI-03.
- Bảng `encounter_embeddings` (entity Domain `EncounterEmbedding`): `EncounterId` (unique, 1–1 với phiếu), `PatientId` (snapshot để lọc theo bệnh nhân không cần join), `Embedding` `vector(1024)`, `Model`.
- **Kiến trúc sạch:** entity Domain giữ `float[]` (EF-agnostic); Infrastructure map `float[] ↔ Pgvector.Vector` bằng value converter + `HasColumnType("vector(1024)")`. Extension bật qua `HasPostgresExtension("vector")` → migration `AddEncounterEmbeddings` sinh `CREATE EXTENSION vector`.
- **Abstraction `IEncounterEmbeddingStore`** (ở Application) tách pgvector khỏi service: `UpsertAsync` + `SearchAsync(patientId, query, topK)`. Hiện thực `PgEncounterEmbeddingStore` (Infrastructure) truy hồi top-K bằng **SQL thô** dùng toán tử cosine `<=>` (độ tương đồng = `1 − khoảng cách`), lọc `PatientId` và `IsDeleted = false`. Unit test dùng `InMemoryEncounterEmbeddingStore` tính cosine trong C# → **không cần pgvector**, chạy với EF InMemory như các slice cũ.
  - Lưu ý: cột giữ **PascalCase** (`"EncounterId"`, `"Embedding"`, `"PatientId"`, `"IsDeleted"`) — SQL thô phải **quote đúng hoa/thường**, nếu không Postgres fold về lowercase và báo "column does not exist".

### 3. Chiến lược chunk — mỗi phiếu = 1 chunk
- Mỗi phiếu khám sinh **một** embedding từ văn bản gộp (ngày, bác sĩ, triệu chứng, chẩn đoán, ghi chú, đơn thuốc). Đơn giản, đủ mịn ở quy mô phòng khám; không tách theo trường.

### 4. Thời điểm re-embed — đồng bộ trong luồng ghi, best-effort
- Tạo/sửa/chốt phiếu → **luôn** sinh lại embedding (`IEncounterEmbeddingIndexer.IndexAsync`), gọi **sau** `SaveChanges` của phiếu.
- **Lỗi embedding KHÔNG được chặn nghiệp vụ ghi phiếu:** indexer nuốt mọi lỗi (mạng/provider/store). Phiếu vẫn được lưu; truy hồi bỏ sót phiếu đó tới lần lập chỉ mục kế tiếp hoặc **backfill**.
- **Backfill:** `POST /api/ai/reindex` (Admin) lập chỉ mục toàn bộ phiếu hiện có — dùng khi bật khoá embedding lần đầu hoặc đổi model.

### 5. Truy hồi + hỏi đáp có ngữ cảnh (AI-05)
- Service mới `PatientQuestionService` (giữ nguyên `PatientSummaryService` của Sprint 6): embed câu hỏi (`input_type=query`) → `SearchAsync` top-K (mặc định **5**) → nạp nội dung phiếu (query filter tự loại phiếu đã xoá) → dựng prompt kèm **nguồn** → gọi `IChatCompletionService`.
- Endpoint `POST /api/patients/{id}/ai-ask` (Bác sĩ/Admin). Không truy hồi được phiếu nào → **không gọi LLM**, trả thông điệp phù hợp. Trả kèm danh sách **nguồn** (traceability) cho frontend.

### 6. Chế độ fake & mã lỗi
- `Ai:UseFakeEmbedding` (mặc định `true` ở Development) → fake embedding. DI chọn Voyage thật **chỉ khi** `!UseFakeEmbedding && EmbeddingApiKey` có (`AiSettings.IsRealEmbeddingConfigured`).
- Unit test dùng **stub embedding** (map văn bản→vector) + **in-memory store** + **stub chat** — kiểm truy hồi đúng top-K + ánh xạ lỗi, **không gọi mạng**.
- Lỗi embedding gói mã **`Embedding.*`** (`Embedding.Unavailable`/`Embedding.Timeout`/`Embedding.BadResponse`, đều `Failure` → 500); lỗi chat vẫn `Ai.*`. Câu hỏi rỗng → `Ai.QuestionRequired` (400).

## Hệ quả
- **Ưu:** xử lý được bệnh nhân nhiều phiếu; truy hồi theo ngữ nghĩa; hạ tầng embedding + vector store tái dùng cho AI-03; kiến trúc sạch (Application chỉ thấy interface); test/CI offline, không tốn phí.
- **Nhược/đánh đổi:**
  - **Thêm dependency** `Pgvector.EntityFrameworkCore` (0.2.0, hợp Npgsql 8) — khác chủ trương "không thêm NuGet" của ADR 0007; chấp nhận vì pgvector là lõi tính năng.
  - **Docker cần image có pgvector** (`pgvector/pgvector:pg16`) thay `postgres:16-alpine`.
  - Số chiều cố định **1024** (khớp `voyage-3`); đổi model khác chiều ⇒ đổi cột + migration + backfill.
  - Re-embed đồng bộ tốn một lần gọi mạng khi ghi phiếu (khi bật khoá thật); đã tách best-effort để không chặn nghiệp vụ.
  - Nội dung bệnh án nhạy cảm được gửi đi để embed — cân nhắc quyền riêng tư/nhật ký khi bật khoá thật.
  - Truy hồi có race (upsert/xoá) như các ràng buộc khác; ở quy mô đồ án chấp nhận. Phiếu đã xoá mềm được lọc ở SQL (`IsDeleted`) và khi nạp nội dung.
- **Còn nợ (sprint sau):** AI-03 chatbot đa lượt; streaming token; gợi ý chẩn đoán (cần thẩm định an toàn); tối ưu chunk/ANN index (`ivfflat`/`hnsw`) khi dữ liệu lớn.
