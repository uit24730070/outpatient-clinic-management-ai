using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParaclinical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "service_prices",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Other");

            migrationBuilder.CreateTable(
                name: "lab_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EncounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InvoicedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lab_orders_doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lab_orders_encounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "encounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_lab_orders_patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lab_order_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServicePriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ResultText = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Conclusion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ResultedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LabOrderId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_order_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lab_order_items_lab_orders_LabOrderId",
                        column: x => x.LabOrderId,
                        principalTable: "lab_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000001"),
                column: "Category",
                value: "Consultation");

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000002"),
                column: "Category",
                value: "Consultation");

            migrationBuilder.UpdateData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000003"),
                column: "Category",
                value: "Consultation");

            migrationBuilder.InsertData(
                table: "service_prices",
                columns: new[] { "Id", "Category", "Code", "CreatedAt", "DeletedAt", "Description", "IsDeleted", "Name", "UnitPrice", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("22222222-3333-4444-5555-000000000004"), "Paraclinical", "DV-CLS001", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Tổng phân tích tế bào máu ngoại vi", false, "Xét nghiệm công thức máu", 80000m, null },
                    { new Guid("22222222-3333-4444-5555-000000000005"), "Paraclinical", "DV-CLS002", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "X-quang ngực thẳng", false, "Chụp X-quang ngực thẳng", 120000m, null },
                    { new Guid("22222222-3333-4444-5555-000000000006"), "Paraclinical", "DV-CLS003", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Siêu âm ổ bụng", false, "Siêu âm ổ bụng tổng quát", 150000m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_lab_order_items_LabOrderId",
                table: "lab_order_items",
                column: "LabOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_lab_order_items_ServicePriceId",
                table: "lab_order_items",
                column: "ServicePriceId");

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_Code",
                table: "lab_orders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_DoctorId",
                table: "lab_orders",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_EncounterId",
                table: "lab_orders",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_lab_orders_PatientId",
                table: "lab_orders",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lab_order_items");

            migrationBuilder.DropTable(
                name: "lab_orders");

            migrationBuilder.DeleteData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000004"));

            migrationBuilder.DeleteData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000005"));

            migrationBuilder.DeleteData(
                table: "service_prices",
                keyColumn: "Id",
                keyValue: new Guid("22222222-3333-4444-5555-000000000006"));

            migrationBuilder.DropColumn(
                name: "Category",
                table: "service_prices");
        }
    }
}
