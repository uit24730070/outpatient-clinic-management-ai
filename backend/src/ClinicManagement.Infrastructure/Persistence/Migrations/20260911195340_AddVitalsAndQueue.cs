using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalsAndQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "queue_tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomId = table.Column<Guid>(type: "uuid", nullable: true),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CalledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queue_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_queue_tickets_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_queue_tickets_doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_queue_tickets_rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "vitals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    HeightCm = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    WeightKg = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    TemperatureC = table.Column<decimal>(type: "numeric(4,1)", nullable: true),
                    Pulse = table.Column<int>(type: "integer", nullable: true),
                    BloodPressureSystolic = table.Column<int>(type: "integer", nullable: true),
                    BloodPressureDiastolic = table.Column<int>(type: "integer", nullable: true),
                    SpO2 = table.Column<int>(type: "integer", nullable: true),
                    RespiratoryRate = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MeasuredBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vitals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vitals_appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_queue_tickets_AppointmentId",
                table: "queue_tickets",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_queue_tickets_DoctorId",
                table: "queue_tickets",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_queue_tickets_RoomId",
                table: "queue_tickets",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_queue_tickets_TicketDate_DoctorId",
                table: "queue_tickets",
                columns: new[] { "TicketDate", "DoctorId" });

            migrationBuilder.CreateIndex(
                name: "IX_queue_tickets_TicketDate_RoomId",
                table: "queue_tickets",
                columns: new[] { "TicketDate", "RoomId" });

            migrationBuilder.CreateIndex(
                name: "IX_queue_tickets_TicketDate_Status",
                table: "queue_tickets",
                columns: new[] { "TicketDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_vitals_AppointmentId",
                table: "vitals",
                column: "AppointmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "queue_tickets");

            migrationBuilder.DropTable(
                name: "vitals");
        }
    }
}
