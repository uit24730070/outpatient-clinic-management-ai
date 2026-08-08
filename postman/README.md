# Postman

Chứa bộ sưu tập (collection) và environment của [Postman](https://www.postman.com/) để kiểm thử API thủ công.

Nội dung dự kiến:

- `*.postman_collection.json` — collection nhóm các request theo module/endpoint
- `*.postman_environment.json` — environment chứa biến (base URL, token...)

## Quy ước

- Dùng biến môi trường (`{{baseUrl}}`, `{{token}}`) thay vì hardcode giá trị
- Export collection/environment và commit vào thư mục này để cả nhóm dùng chung
- KHÔNG commit token/bí mật thật trong file environment

> Sprint 0 chưa có API nên chưa có collection. Thư mục để trống làm khung sẵn sàng.
