using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VisitId",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "visits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_visits_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_VisitId",
                table: "appointments",
                column: "VisitId");

            migrationBuilder.CreateIndex(
                name: "IX_visits_Code",
                table: "visits",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_visits_PatientId",
                table: "visits",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_visits_VisitId",
                table: "appointments",
                column: "VisitId",
                principalTable: "visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_visits_VisitId",
                table: "appointments");

            migrationBuilder.DropTable(
                name: "visits");

            migrationBuilder.DropIndex(
                name: "IX_appointments_VisitId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "VisitId",
                table: "appointments");
        }
    }
}
