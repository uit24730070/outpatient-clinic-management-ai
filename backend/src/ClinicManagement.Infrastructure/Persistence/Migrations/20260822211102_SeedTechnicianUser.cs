using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTechnicianUser : Migration
    {
        // Tài khoản Kỹ thuật viên demo (kythuatvien / Technician@123). Hash BCrypt workFactor 11 tất định,
        // khớp mẫu seed admin/bacsi/duocsi. Vai trò Technician lưu dạng chuỗi (không cần đổi schema). ADR 0016.
        private static readonly Guid TechnicianUserId = new Guid("ffffffff-ffff-ffff-ffff-ffffffffffff");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Email", "FullName", "IsActive", "IsDeleted", "PasswordHash", "Role", "UpdatedAt", "Username" },
                values: new object[] { TechnicianUserId, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "kythuatvien@clinic.vn", "Kỹ thuật viên Demo", true, false, "$2a$11$NK.jjokKDzCDmwy4WF5WtOfvEw1nXL0nFoSEhrruHWzSqkS.T8tsm", "Technician", null, "kythuatvien" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: TechnicianUserId);
        }
    }
}
