using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMorePatients : Migration
    {
        // 30 bệnh nhân demo bổ sung (BN-000002..BN-000031) — chỉ 1 bệnh nhân (BN-000001) tồn tại lúc
        // viết migration này (đã kiểm tra COUNT(*) FROM patients trước khi chọn dãy mã, tránh trùng
        // với GenerateCodeAsync đếm cả bản ghi xoá mềm — ghi chú dự án §8). Guid namespace mới "b00000xx"
        // (chưa dùng cho user/doctor/... nào).
        private static readonly DateTimeOffset SeedTime =
            new(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

        private static readonly (Guid Id, string Code, string FullName, DateOnly Dob, string Gender, string Phone, string Address)[] Patients =
        {
            (new Guid("b0000001-0000-0000-0000-000000000001"), "BN-000002", "Nguyễn Văn An",    new DateOnly(1990, 3, 15), "Male",   "0912345001", "12 Lê Lợi, Q1, TP.HCM"),
            (new Guid("b0000002-0000-0000-0000-000000000002"), "BN-000003", "Trần Thị Bích",    new DateOnly(1985, 7, 22), "Female", "0912345002", "45 Nguyễn Huệ, Q1, TP.HCM"),
            (new Guid("b0000003-0000-0000-0000-000000000003"), "BN-000004", "Lê Văn Cường",     new DateOnly(2000, 11, 5), "Male",   "0912345003", "78 Trần Hưng Đạo, Q5, TP.HCM"),
            (new Guid("b0000004-0000-0000-0000-000000000004"), "BN-000005", "Phạm Thị Dung",    new DateOnly(1975, 1, 30), "Female", "0912345004", "23 Cách Mạng Tháng 8, Q3, TP.HCM"),
            (new Guid("b0000005-0000-0000-0000-000000000005"), "BN-000006", "Hoàng Văn Em",     new DateOnly(1995, 9, 18), "Male",   "0912345005", "56 Điện Biên Phủ, Q.Bình Thạnh, TP.HCM"),
            (new Guid("b0000006-0000-0000-0000-000000000006"), "BN-000007", "Vũ Thị Phương",    new DateOnly(2010, 4, 12), "Female", "0912345006", "89 Hai Bà Trưng, Q1, TP.HCM"),
            (new Guid("b0000007-0000-0000-0000-000000000007"), "BN-000008", "Đặng Văn Giang",   new DateOnly(1988, 6, 25), "Male",   "0912345007", "34 Nguyễn Trãi, Q5, TP.HCM"),
            (new Guid("b0000008-0000-0000-0000-000000000008"), "BN-000009", "Bùi Thị Hạnh",     new DateOnly(1992, 12, 8), "Female", "0912345008", "67 Lý Thường Kiệt, Q10, TP.HCM"),
            (new Guid("b0000009-0000-0000-0000-000000000009"), "BN-000010", "Đỗ Văn Hùng",      new DateOnly(1965, 2, 14), "Male",   "0912345009", "90 Pasteur, Q3, TP.HCM"),
            (new Guid("b0000010-0000-0000-0000-000000000010"), "BN-000011", "Ngô Thị Yến",      new DateOnly(1998, 8, 20), "Female", "0912345010", "12 Võ Văn Tần, Q3, TP.HCM"),
            (new Guid("b0000011-0000-0000-0000-000000000011"), "BN-000012", "Nguyễn Văn Khoa",  new DateOnly(2015, 5, 1),  "Male",   "0912345011", "45 Nam Kỳ Khởi Nghĩa, Q1, TP.HCM"),
            (new Guid("b0000012-0000-0000-0000-000000000012"), "BN-000013", "Trần Thị Lan",     new DateOnly(1980, 10, 11), "Female", "0912345012", "78 Hoàng Văn Thụ, Q.Phú Nhuận, TP.HCM"),
            (new Guid("b0000013-0000-0000-0000-000000000013"), "BN-000014", "Lê Văn Minh",      new DateOnly(1993, 3, 27), "Male",   "0912345013", "23 Nguyễn Đình Chiểu, Q3, TP.HCM"),
            (new Guid("b0000014-0000-0000-0000-000000000014"), "BN-000015", "Phạm Thị Ngọc",    new DateOnly(2003, 7, 19), "Female", "0912345014", "56 Trường Chinh, Q.Tân Bình, TP.HCM"),
            (new Guid("b0000015-0000-0000-0000-000000000015"), "BN-000016", "Hoàng Văn Phi",    new DateOnly(1970, 1, 1),  "Male",   "0912345015", "89 Cộng Hòa, Q.Tân Bình, TP.HCM"),
            (new Guid("b0000016-0000-0000-0000-000000000016"), "BN-000017", "Vũ Thị Quyên",     new DateOnly(1997, 4, 23), "Female", "0912345016", "34 Lạc Long Quân, Q11, TP.HCM"),
            (new Guid("b0000017-0000-0000-0000-000000000017"), "BN-000018", "Đặng Văn Sơn",     new DateOnly(1982, 9, 9),  "Male",   "0912345017", "67 Âu Cơ, Q.Tân Phú, TP.HCM"),
            (new Guid("b0000018-0000-0000-0000-000000000018"), "BN-000019", "Bùi Thị Thảo",     new DateOnly(2005, 12, 30), "Female", "0912345018", "90 Kinh Dương Vương, Q6, TP.HCM"),
            (new Guid("b0000019-0000-0000-0000-000000000019"), "BN-000020", "Đỗ Văn Tùng",      new DateOnly(1960, 6, 6),  "Male",   "0912345019", "12 Phan Văn Trị, Q.Gò Vấp, TP.HCM"),
            (new Guid("b0000020-0000-0000-0000-000000000020"), "BN-000021", "Ngô Thị Uyên",     new DateOnly(1991, 11, 15), "Female", "0912345020", "45 Quang Trung, Q.Gò Vấp, TP.HCM"),
            (new Guid("b0000021-0000-0000-0000-000000000021"), "BN-000022", "Nguyễn Văn Vinh",  new DateOnly(1987, 2, 28), "Male",   "0912345021", "78 Lê Văn Việt, Q9, TP.HCM"),
            (new Guid("b0000022-0000-0000-0000-000000000022"), "BN-000023", "Trần Thị Xuân",    new DateOnly(1978, 5, 5),  "Female", "0912345022", "23 Đỗ Xuân Hợp, Q9, TP.HCM"),
            (new Guid("b0000023-0000-0000-0000-000000000023"), "BN-000024", "Lê Văn Việt",      new DateOnly(2001, 8, 8),  "Male",   "0912345023", "56 Nguyễn Văn Linh, Q7, TP.HCM"),
            (new Guid("b0000024-0000-0000-0000-000000000024"), "BN-000025", "Phạm Thị Ánh",     new DateOnly(1994, 1, 17), "Female", "0912345024", "89 Huỳnh Tấn Phát, Q7, TP.HCM"),
            (new Guid("b0000025-0000-0000-0000-000000000025"), "BN-000026", "Hoàng Văn Bảo",    new DateOnly(2012, 3, 3),  "Male",   "0912345025", "34 Nguyễn Thị Thập, Q7, TP.HCM"),
            (new Guid("b0000026-0000-0000-0000-000000000026"), "BN-000027", "Vũ Thị Chi",       new DateOnly(1986, 10, 10), "Female", "0912345026", "67 Nguyễn Hữu Thọ, Q7, TP.HCM"),
            (new Guid("b0000027-0000-0000-0000-000000000027"), "BN-000028", "Đặng Văn Đạt",     new DateOnly(1996, 7, 7),  "Male",   "0912345027", "90 Phạm Hùng, Q.Bình Tân, TP.HCM"),
            (new Guid("b0000028-0000-0000-0000-000000000028"), "BN-000029", "Bùi Thị Hà",       new DateOnly(1968, 4, 4),  "Female", "0912345028", "12 Tên Lửa, Q.Bình Tân, TP.HCM"),
            (new Guid("b0000029-0000-0000-0000-000000000029"), "BN-000030", "Đỗ Văn Khang",     new DateOnly(1999, 9, 29), "Male",   "0912345029", "45 Hồ Học Lãm, Q.Bình Tân, TP.HCM"),
            (new Guid("b0000030-0000-0000-0000-000000000030"), "BN-000031", "Ngô Thị Linh",     new DateOnly(2008, 2, 2),  "Female", "0912345030", "78 An Dương Vương, Q.Bình Tân, TP.HCM"),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var p in Patients)
            {
                migrationBuilder.InsertData(
                    table: "patients",
                    columns: new[] { "Id", "Address", "Code", "CreatedAt", "DateOfBirth", "DeletedAt", "FullName", "Gender", "IsDeleted", "PhoneNumber", "UpdatedAt" },
                    values: new object[] { p.Id, p.Address, p.Code, SeedTime, p.Dob, null, p.FullName, p.Gender, false, p.Phone, null });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var p in Patients)
            {
                migrationBuilder.DeleteData(table: "patients", keyColumn: "Id", keyValue: p.Id);
            }
        }
    }
}
