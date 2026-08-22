using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RelaxInvoiceModelAndMedicationInvoiced : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoices_EncounterId",
                table: "invoices");

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MedicationInvoicedAt",
                table: "encounters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_AppointmentId",
                table: "invoices",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_EncounterId",
                table: "invoices",
                column: "EncounterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoices_AppointmentId",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_EncounterId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "MedicationInvoicedAt",
                table: "encounters");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_EncounterId",
                table: "invoices",
                column: "EncounterId",
                unique: true);
        }
    }
}
