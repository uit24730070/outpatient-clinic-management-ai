using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePriceGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "service_prices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000001"),
                column: "Group",
                value: null);

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000002"),
                column: "Group",
                value: null);

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000003"),
                column: "Group",
                value: null);

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000004"),
                column: "Group",
                value: "LabTest");

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000005"),
                column: "Group",
                value: "Imaging");

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000006"),
                column: "Group",
                value: "Imaging");

            // Gán nhóm cho 20 dịch vụ CLS seed sẵn ở migration SeedMoreServicePrices (DV-CLS004..023) —
            // các dòng này được nạp bằng InsertData thô nên không nằm trong HasData ở trên.
            migrationBuilder.Sql("""
                UPDATE service_prices SET "Group" = CASE "Code"
                    WHEN 'DV-CLS004' THEN 'LabTest'
                    WHEN 'DV-CLS005' THEN 'LabTest'
                    WHEN 'DV-CLS006' THEN 'LabTest'
                    WHEN 'DV-CLS007' THEN 'LabTest'
                    WHEN 'DV-CLS008' THEN 'LabTest'
                    WHEN 'DV-CLS009' THEN 'LabTest'
                    WHEN 'DV-CLS010' THEN 'LabTest'
                    WHEN 'DV-CLS011' THEN 'LabTest'
                    WHEN 'DV-CLS012' THEN 'LabTest'
                    WHEN 'DV-CLS013' THEN 'LabTest'
                    WHEN 'DV-CLS014' THEN 'LabTest'
                    WHEN 'DV-CLS015' THEN 'LabTest'
                    WHEN 'DV-CLS016' THEN 'LabTest'
                    WHEN 'DV-CLS017' THEN 'LabTest'
                    WHEN 'DV-CLS018' THEN 'Functional'
                    WHEN 'DV-CLS019' THEN 'Imaging'
                    WHEN 'DV-CLS020' THEN 'Imaging'
                    WHEN 'DV-CLS021' THEN 'Imaging'
                    WHEN 'DV-CLS022' THEN 'Imaging'
                    WHEN 'DV-CLS023' THEN 'Endoscopy'
                    ELSE "Group"
                END
                WHERE "Code" IN (
                    'DV-CLS004','DV-CLS005','DV-CLS006','DV-CLS007','DV-CLS008','DV-CLS009','DV-CLS010',
                    'DV-CLS011','DV-CLS012','DV-CLS013','DV-CLS014','DV-CLS015','DV-CLS016','DV-CLS017',
                    'DV-CLS018','DV-CLS019','DV-CLS020','DV-CLS021','DV-CLS022','DV-CLS023');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Group",
                table: "service_prices");
        }
    }
}
