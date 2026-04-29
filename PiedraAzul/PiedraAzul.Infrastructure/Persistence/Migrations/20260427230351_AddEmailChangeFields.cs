using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailChangeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailChangeCode",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailChangeCodeExpiresAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingEmail",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailChangeCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EmailChangeCodeExpiresAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PendingEmail",
                table: "AspNetUsers");
        }
    }
}
