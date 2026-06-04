using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Action = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ActorName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ActorRoles = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SubjectIdentification = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SubjectName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubjectPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Data = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Action",
                table: "AuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ActorUserId",
                table: "AuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType",
                table: "AuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SubjectIdentification",
                table: "AuditLogs",
                column: "SubjectIdentification");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");
        }
    }
}
