using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentBeforeExecution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaidAt",
                table: "lab_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LabOrderId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DispenseStatus",
                table: "encounters",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MedicationPaidAt",
                table: "encounters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReservedAt",
                table: "encounters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_LabOrderId",
                table: "invoices",
                column: "LabOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_encounters_DispenseStatus",
                table: "encounters",
                column: "DispenseStatus");

            // Backfill dữ liệu cũ (không hồi quy — ADR 0021):
            // - Phiếu khám đã cấp phát trước đây (DispensedAt != null) coi như đã xuất kho → 'Dispensed'.
            //   (Cột PascalCase phải quote — Postgres fold lowercase, xem §8 ghi chú dự án.)
            migrationBuilder.Sql(
                "UPDATE encounters SET \"DispenseStatus\" = 'Dispensed' WHERE \"DispensedAt\" IS NOT NULL;");
            // - Phiếu chỉ định CLS đã lập hoá đơn (InvoicedAt != null) coi như đã thu → mở cổng nhập kết quả,
            //   tránh chặn nhầm phiếu đang dở tạo trước khi có gating (PAY-01).
            migrationBuilder.Sql(
                "UPDATE lab_orders SET \"PaidAt\" = \"InvoicedAt\" WHERE \"InvoicedAt\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_invoices_LabOrderId",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_encounters_DispenseStatus",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "lab_orders");

            migrationBuilder.DropColumn(
                name: "LabOrderId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "DispenseStatus",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "MedicationPaidAt",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "ReservedAt",
                table: "encounters");
        }
    }
}
