using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedNurseUser : Migration
    {
        // Tài khoản Điều dưỡng demo (dieuduong / Nurse@123). Hash BCrypt workFactor 11 tất định,
        // khớp mẫu seed admin/bacsi/duocsi/kythuatvien. Vai trò Nurse lưu dạng chuỗi (không đổi schema). ADR 0019.
        private static readonly Guid NurseUserId = new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Email", "FullName", "IsActive", "IsDeleted", "PasswordHash", "Role", "UpdatedAt", "Username" },
                values: new object[] { NurseUserId, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "dieuduong@clinic.vn", "Điều dưỡng Demo", true, false, "$2a$11$n.yTcfZ.4y2YJhJmfCPH2O0lAuRgP6pLOErj.zVmrLJdA9zLPlPFi", "Nurse", null, "dieuduong" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: NurseUserId);
        }
    }
}
