using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionMedicationLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MedicationId",
                table: "prescription_items",
                type: "uuid",
                nullable: true);

            // Cột concurrency token 'xmin' là cột hệ thống có sẵn của mọi bảng Postgres —
            // KHÔNG tạo/xoá bằng DDL (chỉ map ở model qua UseXminAsConcurrencyToken). Xem ADR 0011.

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DispensedAt",
                table: "encounters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_prescription_items_MedicationId",
                table: "prescription_items",
                column: "MedicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_prescription_items_MedicationId",
                table: "prescription_items");

            migrationBuilder.DropColumn(
                name: "MedicationId",
                table: "prescription_items");

            // 'xmin' là cột hệ thống — không xoá bằng DDL (xem Up).

            migrationBuilder.DropColumn(
                name: "DispensedAt",
                table: "encounters");
        }
    }
}
