using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentBackgroundJobsRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentBackgroundJobsRecords",
                columns: table => new
                {
                    JobId = table.Column<string>(type: "text", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentBackgroundJobsRecords", x => x.JobId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentBackgroundJobsRecords_AppointmentId",
                table: "AppointmentBackgroundJobsRecords",
                column: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentBackgroundJobsRecords");
        }
    }
}
