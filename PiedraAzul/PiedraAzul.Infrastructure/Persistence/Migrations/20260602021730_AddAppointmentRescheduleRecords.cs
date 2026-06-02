using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentRescheduleRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentRescheduleRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RootAppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalAppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewAppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RescheduledByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RescheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OriginalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NewDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OriginalDoctorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    NewDoctorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentRescheduleRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRescheduleRecords_NewAppointmentId",
                table: "AppointmentRescheduleRecords",
                column: "NewAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRescheduleRecords_OriginalAppointmentId",
                table: "AppointmentRescheduleRecords",
                column: "OriginalAppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRescheduleRecords_RootAppointmentId",
                table: "AppointmentRescheduleRecords",
                column: "RootAppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentRescheduleRecords");
        }
    }
}
