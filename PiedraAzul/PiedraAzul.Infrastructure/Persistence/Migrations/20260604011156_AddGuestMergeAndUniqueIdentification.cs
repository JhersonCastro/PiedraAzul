using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestMergeAndUniqueIdentification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MergedAt",
                table: "Patients",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MergedToUserId",
                table: "Patients",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_IdentificationNumber",
                table: "AspNetUsers",
                column: "IdentificationNumber",
                unique: true,
                filter: "\"IdentificationNumber\" <> '' AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_IdentificationNumber",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MergedAt",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "MergedToUserId",
                table: "Patients");
        }
    }
}
