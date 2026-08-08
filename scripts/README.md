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

> Sprint 0 chưa thêm script cụ thể. Các thư mục để trống làm khung sẵn sàng.
