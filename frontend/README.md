# Frontend — Clinic Management AI

Ứng dụng web React + TypeScript, dựng bằng [Vite](https://vite.dev/).

> **Trạng thái:** Sprint 0 — skeleton. Chưa có màn hình/component nghiệp vụ.

## Yêu cầu

- Node.js 20+ và npm

## Lệnh thường dùng

```bash
npm install      # cài dependencies
npm run dev      # chạy dev server (HMR)
npm run build    # type-check + build production vào dist/
npm run preview  # xem thử bản build production
npm run lint     # kiểm tra lint (Oxlint)
```

## Cấu trúc `src/`

| Thư mục | Mục đích |
|---------|----------|
| `app/` | Khởi tạo ứng dụng, provider cấp cao, cấu hình toàn cục |
| `assets/` | Tài nguyên tĩnh (ảnh, font, icon) |
| `components/` | Component UI dùng chung, không gắn nghiệp vụ |
| `features/` | Module theo tính năng (mỗi feature tự chứa) |
| `hooks/` | Custom React hooks dùng chung |
| `layouts/` | Bố cục khung trang |
| `pages/` | Component cấp trang, ánh xạ với route |
| `router/` | Cấu hình định tuyến |
| `services/` | Gọi API, client HTTP |
| `store/` | Quản lý state toàn cục |
| `styles/` | Style/theme dùng chung |
| `types/` | Kiểu TypeScript dùng chung |
| `utils/` | Hàm tiện ích thuần |

> Các thư mục hiện để trống (`.gitkeep`) làm khung sẵn sàng cho Sprint 1.
