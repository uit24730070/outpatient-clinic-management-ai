using System;
using System.Linq;
using ClinicManagement.Domain.Billing;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMoreServicePrices : Migration
    {
        // 20 dịch vụ cận lâm sàng (tiếp DV-CLS004..DV-CLS023, sau DV-CLS001..003 đã seed ở
        // ServicePriceConfiguration) + 10 dịch vụ khác (namespace riêng DV-SVC001..010) — KHÔNG dùng
        // dãy DV-000004.. vì đó là dãy GenerateCodeAsync tự sinh cho dịch vụ người dùng tạo qua UI,
        // trùng sẽ gây lỗi unique khi tạo dịch vụ mới (ghi chú dự án §8).
        private static readonly DateTimeOffset SeedTime =
            new(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

        private static readonly (Guid Id, string Code, string Name, decimal Price, string Description, ServiceCategory Category)[] ClsServices =
        {
            (new Guid("c1000004-0000-0000-0000-000000000004"), "DV-CLS004", "Xét nghiệm đường huyết",        40000m,  "Định lượng glucose máu", ServiceCategory.Paraclinical),
            (new Guid("c1000005-0000-0000-0000-000000000005"), "DV-CLS005", "Xét nghiệm mỡ máu",             90000m,  "Bilan lipid máu (Cholesterol, Triglycerid, HDL, LDL)", ServiceCategory.Paraclinical),
            (new Guid("c1000006-0000-0000-0000-000000000006"), "DV-CLS006", "Xét nghiệm chức năng gan",      70000m,  "AST, ALT", ServiceCategory.Paraclinical),
            (new Guid("c1000007-0000-0000-0000-000000000007"), "DV-CLS007", "Xét nghiệm chức năng thận",     70000m,  "Ure, Creatinin", ServiceCategory.Paraclinical),
            (new Guid("c1000008-0000-0000-0000-000000000008"), "DV-CLS008", "Tổng phân tích nước tiểu",      35000m,  "Tổng phân tích nước tiểu bằng máy tự động", ServiceCategory.Paraclinical),
            (new Guid("c1000009-0000-0000-0000-000000000009"), "DV-CLS009", "Xét nghiệm HbA1c",              100000m, "Đánh giá kiểm soát đường huyết 3 tháng", ServiceCategory.Paraclinical),
            (new Guid("c100000a-0000-0000-0000-00000000000a"), "DV-CLS010", "Xét nghiệm Acid Uric",          45000m,  "Định lượng acid uric máu", ServiceCategory.Paraclinical),
            (new Guid("c100000b-0000-0000-0000-00000000000b"), "DV-CLS011", "Xét nghiệm điện giải đồ",       60000m,  "Na, K, Cl máu", ServiceCategory.Paraclinical),
            (new Guid("c100000c-0000-0000-0000-00000000000c"), "DV-CLS012", "Xét nghiệm CRP",                65000m,  "Protein phản ứng C (viêm cấp)", ServiceCategory.Paraclinical),
            (new Guid("c100000d-0000-0000-0000-00000000000d"), "DV-CLS013", "Xét nghiệm Troponin T",         150000m, "Chỉ điểm tổn thương cơ tim", ServiceCategory.Paraclinical),
            (new Guid("c100000e-0000-0000-0000-00000000000e"), "DV-CLS014", "Xét nghiệm HBsAg",              55000m,  "Sàng lọc viêm gan B", ServiceCategory.Paraclinical),
            (new Guid("c100000f-0000-0000-0000-00000000000f"), "DV-CLS015", "Xét nghiệm Anti-HCV",           60000m,  "Sàng lọc viêm gan C", ServiceCategory.Paraclinical),
            (new Guid("c1000010-0000-0000-0000-000000000010"), "DV-CLS016", "Xét nghiệm HIV test nhanh",     50000m,  "Test nhanh HIV Ab", ServiceCategory.Paraclinical),
            (new Guid("c1000011-0000-0000-0000-000000000011"), "DV-CLS017", "Xét nghiệm nhóm máu ABO/Rh",    45000m,  "Định nhóm máu hệ ABO và Rh", ServiceCategory.Paraclinical),
            (new Guid("c1000012-0000-0000-0000-000000000012"), "DV-CLS018", "Điện tâm đồ (ECG)",             60000m,  "Đo điện tâm đồ 12 chuyển đạo", ServiceCategory.Paraclinical),
            (new Guid("c1000013-0000-0000-0000-000000000013"), "DV-CLS019", "Siêu âm tim",                   250000m, "Siêu âm tim qua thành ngực", ServiceCategory.Paraclinical),
            (new Guid("c1000014-0000-0000-0000-000000000014"), "DV-CLS020", "Siêu âm tuyến giáp",            150000m, "Siêu âm tuyến giáp 2 thuỳ", ServiceCategory.Paraclinical),
            (new Guid("c1000015-0000-0000-0000-000000000015"), "DV-CLS021", "Chụp X-quang cột sống",         130000m, "X-quang cột sống thẳng/nghiêng", ServiceCategory.Paraclinical),
            (new Guid("c1000016-0000-0000-0000-000000000016"), "DV-CLS022", "Chụp CT scanner sọ não",        800000m, "CT sọ não không cản quang", ServiceCategory.Paraclinical),
            (new Guid("c1000017-0000-0000-0000-000000000017"), "DV-CLS023", "Nội soi dạ dày",                400000m, "Nội soi thực quản - dạ dày - tá tràng", ServiceCategory.Paraclinical),
        };

        private static readonly (Guid Id, string Code, string Name, decimal Price, string Description, ServiceCategory Category)[] OtherServices =
        {
            (new Guid("d2000001-0000-0000-0000-000000000001"), "DV-SVC001", "Tiêm chủng vắc-xin",        200000m, "Dịch vụ tiêm chủng (chưa gồm giá vắc-xin)", ServiceCategory.Other),
            (new Guid("d2000002-0000-0000-0000-000000000002"), "DV-SVC002", "Khám sức khỏe định kỳ",     300000m, "Gói khám sức khỏe tổng quát định kỳ", ServiceCategory.Consultation),
            (new Guid("d2000003-0000-0000-0000-000000000003"), "DV-SVC003", "Khám sức khỏe xin việc",    250000m, "Khám sức khỏe theo mẫu xin việc/du học", ServiceCategory.Consultation),
            (new Guid("d2000004-0000-0000-0000-000000000004"), "DV-SVC004", "Tư vấn dinh dưỡng",         150000m, "Tư vấn chế độ dinh dưỡng", ServiceCategory.Other),
            (new Guid("d2000005-0000-0000-0000-000000000005"), "DV-SVC005", "Thay băng vết thương",      50000m,  "Thay băng, sát trùng vết thương", ServiceCategory.Other),
            (new Guid("d2000006-0000-0000-0000-000000000006"), "DV-SVC006", "Cắt chỉ vết thương",        60000m,  "Cắt chỉ sau phẫu thuật/tiểu phẫu", ServiceCategory.Other),
            (new Guid("d2000007-0000-0000-0000-000000000007"), "DV-SVC007", "Tiểu phẫu nhỏ",             300000m, "Rạch áp xe, cắt u nhỏ ngoài da…", ServiceCategory.Other),
            (new Guid("d2000008-0000-0000-0000-000000000008"), "DV-SVC008", "Truyền dịch",               120000m, "Truyền dịch (1 chai, chưa gồm dịch truyền)", ServiceCategory.Other),
            (new Guid("d2000009-0000-0000-0000-000000000009"), "DV-SVC009", "Khám thai định kỳ",         180000m, "Khám thai định kỳ theo dõi thai kỳ", ServiceCategory.Consultation),
            (new Guid("d200000a-0000-0000-0000-00000000000a"), "DV-SVC010", "Khám nhi định kỳ",          150000m, "Khám sức khỏe/tăng trưởng định kỳ cho trẻ", ServiceCategory.Consultation),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var s in ClsServices.Concat(OtherServices))
            {
                migrationBuilder.InsertData(
                    table: "service_prices",
                    columns: new[] { "Id", "Category", "Code", "CreatedAt", "DeletedAt", "Description", "IsDeleted", "Name", "UnitPrice", "UpdatedAt" },
                    values: new object[] { s.Id, s.Category.ToString(), s.Code, SeedTime, null, s.Description, false, s.Name, s.Price, null });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var s in ClsServices.Concat(OtherServices))
            {
                migrationBuilder.DeleteData(table: "service_prices", keyColumn: "Id", keyValue: s.Id);
            }
        }
    }
}
