# Clinic Management AI — Hệ thống quản lý khám ngoại trú tích hợp AI

Đồ án tốt nghiệp: hệ thống quản lý khám ngoại trú trên nền tảng web cho phòng khám quy mô vừa và nhỏ,
liên kết toàn bộ quy trình từ tiếp nhận → hàng đợi → sinh hiệu → khám bệnh → cận lâm sàng → kê đơn →
viện phí/thanh toán → cấp phát thuốc, tích hợp trợ lý AI hỗ trợ tóm tắt hồ sơ khám và hỏi đáp nghiệp vụ.

> **Trạng thái:** đã hoàn thiện đầy đủ phạm vi chức năng theo đề cương — 6 vai trò người dùng (Quản trị
> viên, Lễ tân, Bác sĩ, Điều dưỡng, Dược sĩ, Kỹ thuật viên), quy trình khám ngoại trú khép kín có gating
> thanh toán trước khi thực hiện dịch vụ/cấp phát, quản lý kho thuốc theo lô + hạn dùng, thống kê vận
> hành, và trợ lý AI (tóm tắt hồ sơ + hỏi đáp RAG + chatbot nghiệp vụ tool-calling).

## Tính năng chính (theo vai trò)

| Vai trò | Tính năng chính |
|---------|-----------------|
| **Quản trị viên** | Quản lý tài khoản/phân quyền, danh mục dùng chung (bác sĩ, chuyên khoa, phòng khám, bảng giá, danh mục thuốc), dashboard thống kê |
| **Lễ tân** | Tiếp nhận bệnh nhân (đăng ký nhiều dịch vụ khám + cận lâm sàng trong một lượt), quản lý hàng đợi, lập hoá đơn & thu tiền nhanh ngay lúc tiếp nhận |
| **Điều dưỡng** | Gọi số, đo và ghi nhận sinh hiệu — bắt buộc trước khi bác sĩ bắt đầu khám |
| **Bác sĩ** | Khám nhiều bệnh nhân song song (đa tab), lập hồ sơ khám bệnh, chỉ định cận lâm sàng, kê đơn thuốc, xem lịch sử khám, dùng AI tóm tắt hồ sơ |
| **Kỹ thuật viên** | Thực hiện dịch vụ cận lâm sàng đã thu tiền, nhập kết quả — dạng bảng thông số có cấu trúc (tự gắn cờ bất thường) cho xét nghiệm, mô tả tự do cho chẩn đoán hình ảnh/thăm dò chức năng/nội soi |
| **Dược sĩ** | Quản lý danh mục thuốc, nhập kho theo lô + hạn dùng, cấp phát thuốc theo đơn đã thanh toán (FEFO), cảnh báo tồn thấp/sắp hết hạn |

Các mắt xích nghiệp vụ được gating chặt: dịch vụ khám/cận lâm sàng/thuốc đều phải **thanh toán trước
khi thực hiện** (thực hiện cận lâm sàng, cấp phát thuốc); bác sĩ không thể bắt đầu khám khi chưa có
sinh hiệu. Hỗ trợ hoàn tiền hoá đơn đã thu và hoàn kho khi huỷ đơn đã cấp phát.

**Trợ lý AI:** tóm tắt hồ sơ khám (context stuffing), hỏi đáp trên bệnh án bằng RAG (embedding +
pgvector), và chatbot tra cứu nghiệp vụ cho nhân viên (tool-calling, chỉ đọc — không chẩn đoán/đề xuất
điều trị). Mặc định chạy ở chế độ mô phỏng (không gọi mạng, không cần khoá API) để demo nhanh; xem
[Bật trợ lý AI thật](#bật-trợ-lý-ai-thật-tuỳ-chọn) bên dưới.

## Tech Stack

| Lớp | Công nghệ |
|-----|-----------|
| Backend | ASP.NET Core Web API (.NET 8), Clean Architecture, RESTful API |
| Frontend | React 19 + TypeScript (Vite), TailwindCSS v4 + shadcn/ui |
| Database | PostgreSQL 16 (extension `pgvector` cho RAG) |
| AI | OpenAI API (GPT-5 mini) qua abstraction Infrastructure — dễ thay/nâng cấp LLM |
| Hạ tầng | Docker Compose |
| Quản lý mã nguồn | Git |

## Cấu trúc thư mục

```
clinic-management-ai/
├── backend/     # Solution .NET (Clean Architecture: Domain/Application/Infrastructure/WebApi/Shared)
├── frontend/    # Ứng dụng React + TypeScript (Vite)
├── database/    # Ghi chú schema/seed bổ sung (migration thực tế nằm trong backend/.../Infrastructure)
├── docs/        # Tài liệu dự án (xem docs/README.md)
├── postman/     # Bộ sưu tập Postman để kiểm thử API
├── scripts/     # Script tiện ích vận hành dev (test, migrate, deploy, lấy token đăng nhập...)
└── docker-compose.yml
```

## Yêu cầu môi trường

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) và npm
- [PostgreSQL](https://www.postgresql.org/) với extension `pgvector` (hoặc chạy qua Docker Compose)
- [Docker Desktop](https://www.docker.com/) (khuyến nghị — cách chạy nhanh nhất)

## Khởi chạy nhanh bằng Docker Compose (khuyến nghị)

Dựng toàn hệ thống (PostgreSQL + API + Web) bằng một lệnh:

```bash
cp .env.example .env      # đặt JWT_KEY thành chuỗi bí mật ngẫu nhiên ≥ 32 byte
docker compose up --build # Web: http://localhost:8080 · API: http://localhost:5006
```

API tự áp migration khi khởi động (`Database__AutoMigrate=true` trong compose); trợ lý AI mặc định
chạy chế độ mô phỏng nên không cần khoá API để chạy thử toàn bộ hệ thống. Hướng dẫn đầy đủ + bảng biến
môi trường: [`docs/deployment/README.md`](./docs/deployment/README.md).

### Tài khoản đăng nhập demo

| Vai trò | Đăng nhập | Mật khẩu |
|---------|-----------|----------|
| Quản trị viên | `admin` | `Admin@123` |
| Lễ tân | `letan` | `Receptionist@123` |
| Bác sĩ | `bacsi` | `Doctor@123` |
| Điều dưỡng | `dieuduong` | `Nurse@123` |
| Dược sĩ | `duocsi` | `Pharmacist@123` |
| Kỹ thuật viên | `kythuatvien` | `Technician@123` |

### Bật trợ lý AI thật (tuỳ chọn)

Trong `.env`, đặt `AI_USE_FAKE=false`, `AI_USE_FAKE_EMBEDDING=false`, điền `AI_API_KEY` (OpenAI) và
`AI_EMBEDDING_API_KEY` (Voyage) rồi build lại. Chi tiết ở
[`docs/deployment/README.md`](./docs/deployment/README.md#bật-trợ-lý-ai-thật-tuỳ-chọn).

## Khởi chạy nhanh (dành cho phát triển)

### Backend

```bash
cd backend
dotnet restore
dotnet build ClinicManagement.sln                 # phải 0 warning / 0 error
dotnet test tests/UnitTests/UnitTests.csproj       # unit test
dotnet run --project src/ClinicManagement.WebApi   # http://localhost:5006, Swagger ở /swagger
```

### Frontend

```bash
cd frontend
npm install
npm run dev      # http://localhost:5173
npm run build    # tsc -b && vite build — phải sạch
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
- **Application** — use case, DTO, validator, interface. Phụ thuộc Domain.
- **Infrastructure** — hiện thực hạ tầng (EF Core/PostgreSQL, LLM, dịch vụ ngoài). Phụ thuộc Application.
- **WebApi** — điểm vào HTTP, controller, middleware, composition root.
- **Shared** — kernel dùng chung (`Result`/`Error`, `ApiResponse<T>`).

## Tài liệu

Xem thư mục [`docs/`](./docs) — mỗi thư mục con có README mô tả mục đích. Chi tiết đầy đủ về kiến
trúc, thiết kế cơ sở dữ liệu và các quyết định kỹ thuật của đồ án nằm trong báo cáo tốt nghiệp.

## Giấy phép

Xem [LICENSE](./LICENSE).
