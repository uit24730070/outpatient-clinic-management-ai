using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RelaxLabOrderAndAppointmentService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "EncounterId",
                table: "lab_orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "DoctorId",
                table: "lab_orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "lab_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "appointments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ServicePrice",
                table: "appointments",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ServicePriceId",
                table: "appointments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_AppointmentId",
                table: "lab_orders",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_lab_orders_appointments_AppointmentId",
                table: "lab_orders",
                column: "AppointmentId",
                principalTable: "appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lab_orders_appointments_AppointmentId",
                table: "lab_orders");

            migrationBuilder.DropIndex(
                name: "IX_lab_orders_AppointmentId",
                table: "lab_orders");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "lab_orders");

            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "ServicePrice",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "ServicePriceId",
                table: "appointments");

            migrationBuilder.AlterColumn<Guid>(
                name: "EncounterId",
                table: "lab_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DoctorId",
                table: "lab_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
