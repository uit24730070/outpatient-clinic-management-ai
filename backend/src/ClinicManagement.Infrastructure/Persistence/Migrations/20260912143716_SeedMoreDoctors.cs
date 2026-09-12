using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedMoreDoctors : Migration
    {
        // 10 bác sĩ demo bổ sung (bacsi02..bacsi11 / Doctor@123) — dùng LẠI hash BCrypt của tài khoản
        // "bacsi" gốc (cùng mật khẩu Doctor@123, khớp mẫu seed admin/bacsi/duocsi/kythuatvien/dieuduong).
        // Guid namespace mới (bbbbbbbb-.../d0c700xx-...) — tránh trùng các dãy a/c/d/e/f + d0c70001 đã dùng.
        private const string DoctorPasswordHash = "$2a$11$vC9hLKLDQ8OWKFT6UomVCueEHaM8eFy9LgR/C4lreoJf0uFITCRXq";
        private static readonly DateTimeOffset SeedTime =
            new(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero);

        private static readonly Guid NoiTongQuat = new("11111111-1111-1111-1111-111111111111");
        private static readonly Guid TimMach = new("22222222-2222-2222-2222-222222222222");
        private static readonly Guid NhiKhoa = new("33333333-3333-3333-3333-333333333333");
        private static readonly Guid TaiMuiHong = new("44444444-4444-4444-4444-444444444444");
        private static readonly Guid DaLieu = new("55555555-5555-5555-5555-555555555555");

        private static readonly (Guid UserId, Guid DoctorId, string Username, string Code, string FullName, string Phone, Guid SpecialtyId)[] Doctors =
        {
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000002"), new Guid("d0c70002-0000-0000-0000-000000000002"), "bacsi02", "BS-000002", "Nguyễn Văn Bình", "0901000002", NoiTongQuat),
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000003"), new Guid("d0c70003-0000-0000-0000-000000000003"), "bacsi03", "BS-000003", "Trần Thị Cúc", "0901000003", TimMach),
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000004"), new Guid("d0c70004-0000-0000-0000-000000000004"), "bacsi04", "BS-000004", "Lê Văn Dũng", "0901000004", NhiKhoa),
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000005"), new Guid("d0c70005-0000-0000-0000-000000000005"), "bacsi05", "BS-000005", "Phạm Thị Hoa", "0901000005", TaiMuiHong),
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000006"), new Guid("d0c70006-0000-0000-0000-000000000006"), "bacsi06", "BS-000006", "Hoàng Văn Nam", "0901000006", DaLieu),
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000007"), new Guid("d0c70007-0000-0000-0000-000000000007"), "bacsi07", "BS-000007", "Vũ Thị Lan", "0901000007", NoiTongQuat),
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000008"), new Guid("d0c70008-0000-0000-0000-000000000008"), "bacsi08", "BS-000008", "Đặng Văn Long", "0901000008", TimMach),
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000009"), new Guid("d0c70009-0000-0000-0000-000000000009"), "bacsi09", "BS-000009", "Bùi Thị Mai", "0901000009", NhiKhoa),
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000010"), new Guid("d0c70010-0000-0000-0000-000000000010"), "bacsi10", "BS-000010", "Đỗ Văn Phúc", "0901000010", TaiMuiHong),
            (new Guid("bbbbbbbb-bbbb-bbbb-bbbb-000000000011"), new Guid("d0c70011-0000-0000-0000-000000000011"), "bacsi11", "BS-000011", "Ngô Thị Quỳnh", "0901000011", DaLieu),
        };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var d in Doctors)
            {
                migrationBuilder.InsertData(
                    table: "users",
                    columns: new[] { "Id", "CreatedAt", "DeletedAt", "Email", "FullName", "IsActive", "IsDeleted", "PasswordHash", "Role", "UpdatedAt", "Username" },
                    values: new object[] { d.UserId, SeedTime, null, $"{d.Username}@clinic.vn", d.FullName, true, false, DoctorPasswordHash, "Doctor", null, d.Username });

                migrationBuilder.InsertData(
                    table: "doctors",
                    columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "Email", "FullName", "IsDeleted", "PhoneNumber", "SpecialtyId", "UpdatedAt", "UserId" },
                    values: new object[] { d.DoctorId, d.Code, SeedTime, null, $"{d.Username}@clinic.vn", d.FullName, false, d.Phone, d.SpecialtyId, null, d.UserId });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var d in Doctors)
            {
                migrationBuilder.DeleteData(table: "doctors", keyColumn: "Id", keyValue: d.DoctorId);
                migrationBuilder.DeleteData(table: "users", keyColumn: "Id", keyValue: d.UserId);
            }
        }
    }
}
