# 0013. Vai trò Dược sĩ quản lý trực tiếp kho thuốc

- Trạng thái: Accepted
- Ngày: 2026-08-22
- Liên quan: [ADR 0004](0004-chien-luoc-xac-thuc-va-phan-quyen.md) (JWT/RBAC), [ADR 0009](0009-phan-quyen-va-trai-nghiem-giao-dien-theo-vai-tro.md) (phân quyền giao diện), [ADR 0011](0011-mo-hinh-kho-thuoc-va-ton-theo-lo.md) (kho thuốc)

## Bối cảnh

Ở Sprint 11–12 (ADR 0011), vì phòng khám nhỏ nên **chưa tách vai trò Dược sĩ riêng**: quyền
ghi nghiệp vụ kho thuốc (`Roles.ManagePharmacy`) được gộp cho **Admin + Lễ tân** (mirror
`ManageStaff`), còn quyền chốt phiếu kèm cấp phát FEFO (`Roles.DispenseEncounter`) mở cho **cả
ba vai trò** Admin/Lễ tân/Bác sĩ.

Yêu cầu mới: kho thuốc là nghiệp vụ chuyên trách, cần **một vai trò Dược sĩ quản lý trực tiếp**,
không để Lễ tân hay Bác sĩ quản lý kho nữa. Đây là quyết định RBAC thuần — `role` lưu **dạng
chuỗi** nên không cần đổi schema.

## Quyết định

1. **Thêm vai trò `Pharmacist` (Dược sĩ)** vào enum `UserRole` (`= 3`, lưu chuỗi `"Pharmacist"`)
   và hằng `Roles.Pharmacist` ở WebApi. Không cần migration schema (cột `Role` là chuỗi).

2. **`Roles.ManagePharmacy` = Admin + Dược sĩ** (bỏ Lễ tân). Chi phối **ghi** danh mục thuốc,
   nhập kho, và **đọc** sổ cái (`StockTransaction`) + cảnh báo kho (`PharmacyController`).
   **Đọc danh mục thuốc vẫn mở cho mọi vai trò** (bác sĩ cần tra khi kê đơn — không đổi).

3. **`Roles.DispenseEncounter` = Admin + Bác sĩ + Dược sĩ** (bỏ Lễ tân). Vì cấp phát FEFO gộp
   vào bước chốt phiếu (ADR 0011), người khám (Bác sĩ) và quầy phát thuốc (Dược sĩ) đều cần
   chốt phiếu; Lễ tân không đụng tồn kho nữa.

4. **Frontend** đồng bộ nguồn sự thật `config/access.ts`: `canManagePharmacy = Admin||Pharmacist`,
   các mục nav kho (Danh mục thuốc / Nhập kho / Cảnh báo kho) chuyển sang `[Admin, Pharmacist]`,
   landing của Dược sĩ = `/pharmacy/alerts`, thêm nhãn/badge vai trò "Dược sĩ". Guard route
   `MANAGE_PHARMACY = [Admin, Pharmacist]`. Guard FE chỉ là UX — backend vẫn chốt 403.

5. **Seed tài khoản Dược sĩ demo** `duocsi` / `Pharmacist@123` (BCrypt workFactor 11 tất định,
   khớp mẫu `admin`/`bacsi`) qua migration `SeedPharmacistUser` (chỉ `InsertData`, không đổi
   schema).

## Hệ quả

- **Tách bạch trách nhiệm** đúng nghiệp vụ: kho thuốc do Dược sĩ quản lý; Lễ tân quay về đúng
  phận sự tiếp đón/đặt lịch; Bác sĩ tập trung lâm sàng.
- **Đánh đổi:** Lễ tân **mất** quyền nhập kho và chốt phiếu (cấp phát). Ở phòng khám thật, chốt
  phiếu do Bác sĩ (khi khám xong) hoặc Dược sĩ (khi phát thuốc) thực hiện — phù hợp thực tế hơn.
- Enum có thêm giá trị nên các nơi liệt kê vai trò (FE `Object.values(UserRole)`, badge, nav) tự
  bao gồm Dược sĩ; không phá vỡ dữ liệu/tài khoản cũ.
- Vai trò Dược sĩ **chưa gắn hồ sơ bác sĩ** (`doctorId` null) — không dùng luồng `User↔Doctor`.
- **Đã kiểm chứng đầu-cuối** (migration áp trên pgvector; login `duocsi`; đọc/ghi danh mục +
  cảnh báo 200; ghi bệnh nhân 403; Bác sĩ nay ghi kho + cảnh báo 403). 110 unit test giữ xanh.
