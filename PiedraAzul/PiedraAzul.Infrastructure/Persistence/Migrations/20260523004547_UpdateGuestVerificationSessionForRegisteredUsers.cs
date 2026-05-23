using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGuestVerificationSessionForRegisteredUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GuestId",
                table: "GuestVerificationSessions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<int>(
                name: "SessionType",
                table: "GuestVerificationSessions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "GuestVerificationSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "GuestVerificationSessions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "GuestVerificationSessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserPhone",
                table: "GuestVerificationSessions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GuestVerificationSessions_UserId",
                table: "GuestVerificationSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GuestVerificationSessions_UserId",
                table: "GuestVerificationSessions");

            migrationBuilder.DropColumn(
                name: "SessionType",
                table: "GuestVerificationSessions");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "GuestVerificationSessions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "GuestVerificationSessions");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "GuestVerificationSessions");

            migrationBuilder.DropColumn(
                name: "UserPhone",
                table: "GuestVerificationSessions");

            migrationBuilder.AlterColumn<string>(
                name: "GuestId",
                table: "GuestVerificationSessions",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true);
        }
    }
}
