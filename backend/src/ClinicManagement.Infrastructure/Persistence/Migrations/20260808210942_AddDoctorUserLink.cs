using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorUserLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "doctors",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAt", "DeletedAt", "Email", "FullName", "IsActive", "IsDeleted", "PasswordHash", "Role", "UpdatedAt", "Username" },
                values: new object[] { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "bacsi@clinic.vn", "Bác sĩ Demo", true, false, "$2a$11$vC9hLKLDQ8OWKFT6UomVCueEHaM8eFy9LgR/C4lreoJf0uFITCRXq", "Doctor", null, "bacsi" });

            migrationBuilder.InsertData(
                table: "doctors",
                columns: new[] { "Id", "Code", "CreatedAt", "DeletedAt", "Email", "FullName", "IsDeleted", "PhoneNumber", "SpecialtyId", "UpdatedAt", "UserId" },
                values: new object[] { new Guid("d0c70001-0000-0000-0000-000000000001"), "BS-000001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "bacsi@clinic.vn", "Bác sĩ Demo", false, null, new Guid("11111111-1111-1111-1111-111111111111"), null, new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd") });

            migrationBuilder.CreateIndex(
                name: "IX_doctors_UserId",
                table: "doctors",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_doctors_users_UserId",
                table: "doctors",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_doctors_users_UserId",
                table: "doctors");

            migrationBuilder.DropIndex(
                name: "IX_doctors_UserId",
                table: "doctors");

            migrationBuilder.DeleteData(
                table: "doctors",
                keyColumn: "Id",
                keyValue: new Guid("d0c70001-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "doctors");
        }
    }
}
