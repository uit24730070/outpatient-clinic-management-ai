using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLabOrderVisitLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "VisitId",
                table: "lab_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_VisitId",
                table: "lab_orders",
                column: "VisitId");

            migrationBuilder.AddForeignKey(
                name: "FK_lab_orders_visits_VisitId",
                table: "lab_orders",
                column: "VisitId",
                principalTable: "visits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lab_orders_visits_VisitId",
                table: "lab_orders");

            migrationBuilder.DropIndex(
                name: "IX_lab_orders_VisitId",
                table: "lab_orders");

            migrationBuilder.DropColumn(
                name: "VisitId",
                table: "lab_orders");
        }
    }
}
