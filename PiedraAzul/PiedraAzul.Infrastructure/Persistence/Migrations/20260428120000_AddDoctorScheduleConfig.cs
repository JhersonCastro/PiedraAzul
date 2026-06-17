using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    public partial class AddDoctorScheduleConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorScheduleConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SlotIntervalMinutes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorScheduleConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoctorScheduleDayConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorScheduleConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorScheduleDayConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorScheduleDayConfigs_DoctorScheduleConfigs_DoctorScheduleConfigId",
                        column: x => x.DoctorScheduleConfigId,
                        principalTable: "DoctorScheduleConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorScheduleConfigs_DoctorId",
                table: "DoctorScheduleConfigs",
                column: "DoctorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorScheduleDayConfigs_DoctorScheduleConfigId_DayOfWeek",
                table: "DoctorScheduleDayConfigs",
                columns: new[] { "DoctorScheduleConfigId", "DayOfWeek" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorScheduleDayConfigs");

            migrationBuilder.DropTable(
                name: "DoctorScheduleConfigs");
        }
    }
}
