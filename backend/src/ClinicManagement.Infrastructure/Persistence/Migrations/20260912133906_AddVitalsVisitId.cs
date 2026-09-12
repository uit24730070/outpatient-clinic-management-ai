using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalsVisitId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vitals_AppointmentId",
                table: "vitals");

            migrationBuilder.AddColumn<Guid>(
                name: "VisitId",
                table: "vitals",
                type: "uuid",
                nullable: true);

            // Backfill: lịch đã thuộc một lượt (Appointment.VisitId, ADR 0017) → gán luôn cho sinh hiệu
            // cũ của lịch đó, để các dịch vụ khám khác trong cùng lượt dùng chung ngay sau migration.
            migrationBuilder.Sql(
                "UPDATE \"vitals\" v SET \"VisitId\" = a.\"VisitId\" " +
                "FROM \"appointments\" a " +
                "WHERE a.\"Id\" = v.\"AppointmentId\" AND a.\"VisitId\" IS NOT NULL;");

            // Dữ liệu cũ có thể đã có NHIỀU bản ghi sinh hiệu cho cùng một lượt (mỗi dịch vụ khám đo
            // riêng, trước khi gộp theo lượt) — bây giờ trùng VisitId nên phải dồn về MỘT bản ghi (giữ
            // bản đo gần nhất) trước khi tạo unique index, nếu không migration sẽ lỗi vi phạm ràng buộc.
            migrationBuilder.Sql(
                "DELETE FROM \"vitals\" WHERE \"Id\" IN (" +
                "SELECT \"Id\" FROM (" +
                "SELECT \"Id\", ROW_NUMBER() OVER (" +
                "PARTITION BY \"VisitId\" ORDER BY \"MeasuredAt\" DESC, \"CreatedAt\" DESC) AS rn " +
                "FROM \"vitals\" WHERE \"VisitId\" IS NOT NULL) ranked " +
                "WHERE rn > 1);");

            migrationBuilder.CreateIndex(
                name: "IX_vitals_AppointmentId",
                table: "vitals",
                column: "AppointmentId",
                unique: true,
                filter: "\"VisitId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vitals_VisitId",
                table: "vitals",
                column: "VisitId",
                unique: true,
                filter: "\"VisitId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_vitals_visits_VisitId",
                table: "vitals",
                column: "VisitId",
                principalTable: "visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vitals_visits_VisitId",
                table: "vitals");

            migrationBuilder.DropIndex(
                name: "IX_vitals_AppointmentId",
                table: "vitals");

            migrationBuilder.DropIndex(
                name: "IX_vitals_VisitId",
                table: "vitals");

            migrationBuilder.DropColumn(
                name: "VisitId",
                table: "vitals");

            migrationBuilder.CreateIndex(
                name: "IX_vitals_AppointmentId",
                table: "vitals",
                column: "AppointmentId",
                unique: true);
        }
    }
}
