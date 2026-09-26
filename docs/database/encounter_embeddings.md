# Bảng `encounter_embeddings` — Từ điển dữ liệu

Embedding (vector ngữ nghĩa) của phiếu khám phục vụ **truy hồi RAG** (Sprint 7). Sinh từ entity `ClinicManagement.Domain.Ai.EncounterEmbedding`, migration `AddEncounterEmbeddings`. Dùng **pgvector** trên PostgreSQL (xem [ADR 0008](../adr/0008-rag-va-vector-store.md)).

> **Yêu cầu hạ tầng:** extension `vector` (migration tự `CREATE EXTENSION IF NOT EXISTS vector`). Container dev cần image `pgvector/pgvector:pg16`.

## Bảng `encounter_embeddings`

| Cột | Kiểu (PostgreSQL) | Null | Ràng buộc / Ghi chú |
|-----|-------------------|------|---------------------|
| `Id` | `uuid` | Không | Khóa chính. Sinh phía ứng dụng (`Guid.NewGuid()`). |
| `EncounterId` | `uuid` | Không | **FK** → `encounters.Id`, `ON DELETE CASCADE`. **Unique** (1–1: mỗi phiếu một embedding). |
| `PatientId` | `uuid` | Không | **Snapshot** để lọc truy hồi theo bệnh nhân không cần join. Có index. |
| `Embedding` | `vector(1024)` | Không | Vector embedding (pgvector). Số chiều khớp `voyage-3` = 1024. Map `float[] ↔ Pgvector.Vector` ở Infrastructure. |
| `Model` | `varchar(100)` | Không | Model đã sinh embedding (đối chiếu khi đổi provider/model → cần re-embed). |
| `CreatedAt` | `timestamptz` | Không | Gán tự động khi tạo. |
| `UpdatedAt` | `timestamptz` | Có | Gán tự động khi cập nhật (re-embed). |
| `IsDeleted` | `boolean` | Không | Cờ xoá mềm (kế thừa `Entity`; mặc định `false`). |
| `DeletedAt` | `timestamptz` | Có | Thời điểm xoá mềm. |

### Index & khoá ngoại
- `PK_encounter_embeddings` — khóa chính trên `Id`.
- `IX_encounter_embeddings_EncounterId` — **UNIQUE** (1–1 với phiếu; upsert theo cột này).
- `IX_encounter_embeddings_PatientId` — lọc truy hồi theo bệnh nhân.
- `FK_encounter_embeddings_encounters_EncounterId` — `ON DELETE CASCADE` (xoá cứng phiếu ⇒ xoá embedding; hệ thống chỉ xoá mềm phiếu nên thực tế ít kích hoạt).

## Quy tắc nghiệp vụ
- **Sinh embedding** khi tạo/sửa/chốt phiếu (`IEncounterEmbeddingIndexer`, gọi sau `SaveChanges`). Mỗi phiếu = **1 chunk** (văn bản gộp: ngày, bác sĩ, triệu chứng, chẩn đoán, ghi chú, đơn thuốc).
- **Best-effort:** lỗi sinh/lưu embedding **không** chặn ghi phiếu; phiếu vẫn được lưu, truy hồi bỏ sót tới lần lập chỉ mục kế tiếp / **backfill** (`POST /api/ai/reindex`, Admin).
- **Truy hồi** (`PgEncounterEmbeddingStore.SearchAsync`): SQL thô, toán tử cosine `<=>`, độ tương đồng `= 1 − khoảng cách`; lọc `PatientId` và `IsDeleted = false`, lấy top-K (mặc định 5).
  - Cột giữ **PascalCase** → SQL thô quote đúng hoa/thường (`"Embedding"`, `"PatientId"`, `"IsDeleted"`).
- **Fake/không khoá:** dev/test dùng vector tất định (`Ai:UseFakeEmbedding = true`) — không gọi mạng, không cần pgvector (unit test dùng store in-memory).
- Chiến lược xoá mềm: xem [ADR 0003](../adr/0003-chien-luoc-soft-delete.md).
