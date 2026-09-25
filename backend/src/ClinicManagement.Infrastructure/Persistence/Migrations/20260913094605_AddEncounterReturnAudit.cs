using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEncounterReturnAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "encounters",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReturnedAt",
                table: "encounters",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReturnedByUserId",
                table: "encounters",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "encounters");

            migrationBuilder.DropColumn(
                name: "ReturnedByUserId",
                table: "encounters");
        }
    }
}
