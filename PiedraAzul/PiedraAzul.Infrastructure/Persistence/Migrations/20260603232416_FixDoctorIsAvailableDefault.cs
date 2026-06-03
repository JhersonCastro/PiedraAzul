using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixDoctorIsAvailableDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Marcar todos los doctores existentes como disponibles
            migrationBuilder.Sql("""UPDATE "Doctors" SET "IsAvailable" = TRUE WHERE "IsAvailable" = FALSE;""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "Doctors" SET "IsAvailable" = FALSE;""");
        }
    }
}
