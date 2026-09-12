# Scripts

Chứa các script tiện ích hỗ trợ phát triển, build, chạy và bảo trì dự án.

| Thư mục | Mục đích |
|---------|----------|
| `windows/` | Script cho Windows (PowerShell `.ps1` / batch `.cmd`) |
| `linux/` | Script cho Linux/macOS (shell `.sh`) |

## Quy ước

- Mỗi tác vụ nên có bản tương đương cho cả hai nền tảng khi khả thi
- Đặt tên script theo hành động: `setup`, `build`, `run-backend`, `run-frontend`, `db-migrate`...
- Script cần idempotent và có thông báo rõ ràng khi lỗi

## Script wrapper (dùng trực tiếp trong `scripts/`, chạy qua Git Bash)

Ba script sau **bắt buộc dùng thay vì gọi lệnh trực tiếp** (xem ghi chú dự án mục 7)
để giảm lượng log khi chạy lệnh lặp lại — chỉ in tín hiệu cần thiết,
không đổ full log.

| Script | Cách gọi | Việc làm |
|--------|----------|----------|
| `test.sh` | `./scripts/test.sh` | Chạy `dotnet test backend/tests/UnitTests/UnitTests.csproj --verbosity quiet`, chỉ in dòng chứa `Failed`/`Passed!`/`error`/`Total tests` (hoặc `Passed!` nếu không có dòng nào khớp). Thoát với đúng exit code của `dotnet test`. |
| `dev-up.sh` | `./scripts/dev-up.sh` | Kiểm tra bằng `docker ps` xem container Postgres (tên chứa `clinic-pg` hoặc `clinic-management-ai-db`) đã chạy chưa; nếu đã chạy in `already running, skipping` và không làm gì thêm, nếu chưa thì chạy `docker compose up -d api web`. |
| `wait-for-api.sh` | `./scripts/wait-for-api.sh <url> <timeout_giay>` | Poll `<url>` bằng `curl` mỗi giây đến khi trả về HTTP 200 hoặc hết `<timeout_giay>`; in `ready` hoặc `timeout after Ns`. |
| `redeploy.sh` | `./scripts/redeploy.sh [service...]` | Rebuild + `docker compose up -d --build` cho service (mặc định `api`), chỉ in dòng chứa `error/fail/Recreated/Started` thay vì đổ full log build. Dùng sau khi sửa code backend/frontend cần áp dụng lên container đang chạy. |

> Sprint 0 chưa thêm script cụ thể trong `windows/` và `linux/`. Các thư mục để trống làm khung sẵn sàng.
