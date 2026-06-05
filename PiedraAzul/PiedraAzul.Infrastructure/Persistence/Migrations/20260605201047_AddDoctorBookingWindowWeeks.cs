using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorBookingWindowWeeks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookingWindowWeeks",
                table: "Doctors",
                type: "integer",
                nullable: false,
                defaultValue: 4);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingWindowWeeks",
                table: "Doctors");
        }
    }
}
