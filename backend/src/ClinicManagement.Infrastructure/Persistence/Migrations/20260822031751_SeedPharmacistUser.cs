using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedPharmacistUser : Migration
    {
        // Tài khoản Dược sĩ demo (duocsi / Pharmacist@123). Hash BCrypt workFactor 11 tất định,
        // khớp mẫu seed admin/bacsi. Vai trò Pharmacist lưu dạng chuỗi (không cần đổi schema). ADR 0013.
        private static readonly Guid PharmacistUserId = new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Email", "FullName", "IsActive", "IsDeleted", "PasswordHash", "Role", "UpdatedAt", "Username" },
                values: new object[] { PharmacistUserId, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "duocsi@clinic.vn", "Dược sĩ Demo", true, false, "$2a$11$po.591YsPDm8mnr8xZHmC.2B6HEzpE78SB1vtkKYauO4TSBkz2tOm", "Pharmacist", null, "duocsi" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: PharmacistUserId);
        }
    }
}
