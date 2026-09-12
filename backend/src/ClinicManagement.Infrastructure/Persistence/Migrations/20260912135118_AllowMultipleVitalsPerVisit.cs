using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleVitalsPerVisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vitals_AppointmentId",
                table: "vitals");

            migrationBuilder.DropIndex(
                name: "IX_vitals_VisitId",
                table: "vitals");

            migrationBuilder.CreateIndex(
                name: "IX_vitals_AppointmentId",
                table: "vitals",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_vitals_VisitId",
                table: "vitals",
                column: "VisitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_vitals_AppointmentId",
                table: "vitals");

            migrationBuilder.DropIndex(
                name: "IX_vitals_VisitId",
                table: "vitals");

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
        }
    }
}
