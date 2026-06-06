using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixAppointmentBackgroundJobsRecordsPK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AppointmentBackgroundJobsRecords",
                table: "AppointmentBackgroundJobsRecords");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "AppointmentBackgroundJobsRecords",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppointmentBackgroundJobsRecords",
                table: "AppointmentBackgroundJobsRecords",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentBackgroundJobsRecords_JobId",
                table: "AppointmentBackgroundJobsRecords",
                column: "JobId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AppointmentBackgroundJobsRecords",
                table: "AppointmentBackgroundJobsRecords");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentBackgroundJobsRecords_JobId",
                table: "AppointmentBackgroundJobsRecords");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "AppointmentBackgroundJobsRecords");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppointmentBackgroundJobsRecords",
                table: "AppointmentBackgroundJobsRecords",
                column: "JobId");
        }
    }
}
