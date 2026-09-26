# Architecture Decision Records (ADR)

Nhật ký các quyết định kiến trúc quan trọng. Mỗi quyết định là một file riêng, đánh số tuần tự.

## Quy ước đặt tên

```
NNNN-tieu-de-ngan-gon.md
```

Ví dụ: `0001-su-dung-clean-architecture.md`

## Danh mục
- [0002 — EF Core & chiến lược migration](0002-su-dung-ef-core-va-chien-luoc-migration.md)
- [0003 — Chiến lược soft delete](0003-chien-luoc-soft-delete.md)
- [0004 — Xác thực & phân quyền (JWT + RBAC)](0004-chien-luoc-xac-thuc-va-phan-quyen.md)
- [0005 — Lịch khám & máy trạng thái](0005-mo-hinh-lich-kham-va-may-trang-thai.md)
- [0006 — Bệnh án Encounter/Prescription](0006-mo-hinh-benh-an-encounter-prescription.md)
- [0007 — Tích hợp LLM](0007-tich-hop-llm.md)
- [0008 — RAG & vector store](0008-rag-va-vector-store.md)
- [0009 — Phân quyền & trải nghiệm giao diện theo vai trò](0009-phan-quyen-va-trai-nghiem-giao-dien-theo-vai-tro.md)
- [0010 — Chatbot nghiệp vụ bằng tool-calling](0010-chatbot-tool-calling.md)
- [0011 — Mô hình kho thuốc & tồn theo lô/hạn dùng](0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md)
- [0012 — TailwindCSS + shadcn/ui + react-hook-form/zod cho lớp trình bày](0012-tailwind-shadcn-va-chuan-hoa-form.md)
- [0013 — Vai trò Dược sĩ quản lý trực tiếp kho thuốc](0013-vai-tro-duoc-si-quan-ly-kho-thuoc.md)
- [0014 — Mô hình viện phí & thu ngân](0014-mo-hinh-vien-phi-va-thu-ngan.md)
- [0015 — Mô hình cận lâm sàng](0015-mo-hinh-can-lam-sang.md)
- [0016 — Đăng ký dịch vụ, walk-in CLS & Kỹ thuật viên](0016-dang-ky-dich-vu-walkin-cls-va-ky-thuat-vien.md)
- [0017 — Mô hình lượt tiếp đón (Visit)](0017-mo-hinh-luot-tiep-don-visit.md)
- [0018 — Lịch làm việc bác sĩ & tài nguyên phòng khám](0018-lich-lam-viec-va-tai-nguyen-phong.md)
- [0019 — Điều dưỡng, sinh hiệu & hàng đợi khám](0019-dieu-duong-sinh-hieu-va-hang-doi.md)
- [0020 — Báo cáo & thống kê vận hành (Dashboard)](0020-bao-cao-thong-ke-van-hanh.md)
- [0021 — Thanh toán trước khi thực hiện](0021-thanh-toan-truoc-khi-thuc-hien.md)
- [0022 — Hoàn tiền & Hoàn kho](0022-hoan-tien-hoan-kho.md)
- [0023 — Khép kín vai trò Điều dưỡng](0023-khep-kin-vai-tro-dieu-duong.md)
- [0024 — Gom nhóm hiển thị dịch vụ Cận lâm sàng khi chỉ định](0024-nhom-hien-thi-can-lam-sang.md)
- [0025 — Kết quả có cấu trúc cho nhóm Xét nghiệm](0025-ket-qua-co-cau-truc-xet-nghiem.md)
- [0026 — Tự lập hoá đơn ngay lúc tiếp nhận](0026-tu-lap-hoa-don-luc-tiep-nhan.md)

## Mẫu ADR đề xuất

```markdown
# NNNN. Tiêu đề quyết định

- Trạng thái: Proposed | Accepted | Deprecated | Superseded
- Ngày: YYYY-MM-DD

## Bối cảnh
Vấn đề/tình huống dẫn tới quyết định.

## Quyết định
Lựa chọn được chốt.

## Hệ quả
Ưu điểm, nhược điểm, đánh đổi kèm theo.
```
