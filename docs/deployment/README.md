# Triển khai bằng Docker Compose (F-08)

Dựng toàn hệ thống — **PostgreSQL (pgvector) + API + Web** — bằng một lệnh. Phù hợp cho demo/bảo vệ đồ án và chạy thử ngoài máy dev.

## Kiến trúc container

```
                 ┌─────────────────────────────────────────┐
   trình duyệt ─▶│ web (nginx)  :80                         │
                 │   • phục vụ bản build tĩnh React (SPA)   │
                 │   • reverse proxy /api, /health ─▶ api   │
                 └──────────────┬──────────────────────────┘
                                │ (mạng nội bộ compose)
                 ┌──────────────▼──────────────┐   ┌─────────────────────────┐
                 │ api (ASP.NET Core) :8080    │──▶│ db (pgvector/pgvector:   │
                 │   • tự áp migration khi bật │   │      pg16)  :5432        │
                 │     Database__AutoMigrate   │   │   • volume postgres_data │
                 └─────────────────────────────┘   └─────────────────────────┘
```

- **web** phục vụ giao diện và **reverse proxy** mọi lời gọi `/api` + `/health` sang **api** ⇒ trình duyệt gọi same-origin, không cần CORS. Vì vậy `VITE_API_BASE_URL` để **trống** khi build ảnh web (apiClient dùng URL tương đối).
- **api** chỉ chạy HTTP `:8080` trong mạng compose; hạ tầng/reverse proxy ngoài lo TLS.
- **db** **bắt buộc** image `pgvector/pgvector:pg16` (extension `vector` cho RAG — Sprint 7). Dữ liệu bền trong volume `postgres_data`.

## Các bước

```bash
# 1. Chuẩn bị biến môi trường (KHÔNG commit .env)
cp .env.example .env
#   → mở .env, đặt JWT_KEY thành chuỗi bí mật ngẫu nhiên (≥ 32 byte).

# 2. Dựng & chạy toàn bộ
docker compose up --build          # thêm -d để chạy nền

# 3. Truy cập
#   Web  : http://localhost:8080        (đổi qua WEB_PORT)
#   API  : http://localhost:5006/health (đổi qua API_PORT)

# 4. Dừng (giữ dữ liệu)         : docker compose down
#    Dừng và xoá luôn dữ liệu DB: docker compose down -v
```

Thứ tự khởi động được đảm bảo qua **healthcheck** DB (`pg_isready`) + `depends_on: condition: service_healthy`: **api** chỉ chạy khi **db** đã sẵn sàng, rồi tự áp migration.

## Tài khoản mặc định (seed)

| Vai trò | Đăng nhập | Mật khẩu |
|---------|-----------|----------|
| Admin | `admin` | `Admin@123` |
| Bác sĩ | `bacsi` | `Doctor@123` (đã gắn hồ sơ `BS-000001`) |
| Dược sĩ | `duocsi` | `Pharmacist@123` (quản lý kho thuốc — ADR 0013) |

Đăng nhập Admin để vào **Người dùng** (tạo tài khoản, gắn bác sĩ…); đăng nhập `bacsi` để thấy **Phòng khám của tôi**; đăng nhập `duocsi` để quản lý **Kho thuốc** (danh mục, nhập kho, cảnh báo).

## Biến môi trường (`.env`)

| Biến | Mặc định | Ý nghĩa |
|------|----------|---------|
| `POSTGRES_DB` | `clinic_management` | Tên CSDL. |
| `POSTGRES_USER` | `postgres` | User CSDL. |
| `POSTGRES_PASSWORD` | `change_me` | Mật khẩu CSDL. |
| `POSTGRES_PORT` | `5432` | Cổng DB map ra host. |
| `JWT_KEY` | *(bắt buộc)* | Khoá ký JWT (≥ 32 byte). Thiếu → compose báo lỗi và không chạy. |
| `API_PORT` | `5006` | Cổng API map ra host. |
| `WEB_PORT` | `8080` | Cổng web map ra host. |
| `AI_USE_FAKE` | `true` | `true` → tóm tắt AI mô phỏng, không gọi mạng/không tốn phí. |
| `AI_USE_FAKE_EMBEDDING` | `true` | `true` → embedding tất định, không gọi mạng. |
| `AI_API_KEY` | *(trống)* | Khoá OpenAI API (GPT-5 mini) — chỉ cần khi `AI_USE_FAKE=false`. |
| `AI_EMBEDDING_API_KEY` | *(trống)* | Khoá Voyage AI — chỉ cần khi `AI_USE_FAKE_EMBEDDING=false`. |

> **Bí mật:** `.env` đã nằm trong `.gitignore`; chỉ `.env.example` (không chứa giá trị thật) được commit. Đừng đưa khoá thật vào repo.

## Bật trợ lý AI thật (tuỳ chọn)

Trong `.env` đặt `AI_USE_FAKE=false`, `AI_USE_FAKE_EMBEDDING=false` và điền `AI_API_KEY` (OpenAI) + `AI_EMBEDDING_API_KEY` (Voyage). Sau đó đăng nhập Admin và gọi `POST /api/ai/reindex` để lập chỉ mục embedding cho phiếu khám hiện có. Xem [ADR 0007](../adr/0007-tich-hop-llm.md) và [ADR 0008](../adr/0008-rag-va-vector-store.md).

## Ghi chú vận hành

- **Áp migration:** ảnh api bật `Database__AutoMigrate=true` → gọi `Database.Migrate()` lúc khởi động. Cờ này mặc định **tắt** ngoài Docker để không ảnh hưởng luồng dev/test (áp bằng `dotnet ef` thủ công).
- **Đổi mã nguồn:** chạy lại `docker compose up --build` để build lại ảnh liên quan.
- **Xem log:** `docker compose logs -f api` (hoặc `web`/`db`).
