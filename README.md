# Clinic Management AI — Hệ thống quản lý khám ngoại trú tích hợp AI

Đồ án tốt nghiệp: hệ thống quản lý phòng khám / khám ngoại trú, tích hợp trợ lý AI (LLM) hỗ trợ nghiệp vụ.

> **Trạng thái:** Sprint 9 — Quản lý người dùng (Admin) + gắn/gỡ `User↔Doctor` qua UI + **Docker Compose** dựng toàn hệ thống. (Đã có: bệnh nhân, bác sĩ/chuyên khoa, đặt lịch/tiếp đón, bệnh án/đơn thuốc, xác thực/RBAC, trợ lý AI tóm tắt + hỏi đáp RAG.)

## Tech Stack

| Lớp | Công nghệ |
|-----|-----------|
| Backend | ASP.NET Core Web API (.NET 8), Clean Architecture, RESTful API |
| Frontend | React + TypeScript (Vite) |
| Database | PostgreSQL |
| AI | LLM API |
| Hạ tầng | Docker Compose |
| Quản lý mã nguồn | Git |

## Cấu trúc thư mục

```
clinic-management-ai/
├── backend/     # Solution .NET (Clean Architecture)
├── frontend/    # Ứng dụng React + TypeScript (Vite)
├── database/    # Schema, migrations, seeds, scripts SQL
├── docs/        # Tài liệu kiến trúc, DB, API, ADR, biểu đồ, biên bản họp
├── prompts/     # Prompt cho LLM (system, template, summary)
├── postman/     # Bộ sưu tập Postman để kiểm thử API
├── scripts/     # Script tiện ích (Windows / Linux)
└── tasks/       # Backlog và kế hoạch theo sprint
```

## Yêu cầu môi trường

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) và npm
- [PostgreSQL](https://www.postgresql.org/) (hoặc chạy qua Docker Compose)
- [Docker Desktop](https://www.docker.com/) (tùy chọn)

## Khởi chạy nhanh bằng Docker Compose (khuyến nghị)

Dựng toàn hệ thống (PostgreSQL + API + Web) bằng một lệnh:

```bash
cp .env.example .env      # đặt JWT_KEY thành chuỗi bí mật ≥ 32 byte
docker compose up --build # Web: http://localhost:8080 · API: http://localhost:5006
```

Đăng nhập mặc định: Admin `admin`/`Admin@123`, Bác sĩ `bacsi`/`Doctor@123`. Hướng dẫn đầy đủ + bảng biến môi trường: [`docs/deployment/`](./docs/deployment/README.md).

## Khởi chạy nhanh (dành cho phát triển)

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/ClinicManagement.WebApi
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Kiến trúc Backend (Clean Architecture)

Chiều phụ thuộc luôn hướng vào trong (inward):

```
WebApi ──▶ Application ──▶ Domain
   │            │            ▲
   └─▶ Infrastructure ───────┘
                 ▲
            Shared (cross-cutting, được tham chiếu bởi các lớp trong)
```

- **Domain** — thực thể, quy tắc nghiệp vụ cốt lõi. Không phụ thuộc framework.
- **Application** — use case, interface, orchestration. Phụ thuộc Domain.
- **Infrastructure** — hiện thực hạ tầng (DB, LLM, dịch vụ ngoài). Phụ thuộc Application.
- **WebApi** — điểm vào HTTP, composition root. Phụ thuộc Application + Infrastructure.
- **Shared** — kernel dùng chung (kiểu, hằng số, tiện ích cross-cutting).

## Tài liệu

Xem thư mục [`docs/`](./docs) — mỗi thư mục con có README mô tả mục đích.

## Giấy phép

Xem [LICENSE](./LICENSE).
