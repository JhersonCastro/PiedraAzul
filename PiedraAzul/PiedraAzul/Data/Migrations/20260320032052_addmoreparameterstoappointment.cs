using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PiedraAzul.Data.Migrations
{
    /// <inheritdoc />
    public partial class addmoreparameterstoappointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_DoctorAvailabilitySlots_DoctorAvailabilityBloc~",
                table: "Appointments");

            migrationBuilder.RenameColumn(
                name: "DoctorAvailabilityBlockId",
                table: "Appointments",
                newName: "DoctorAvailabilitySlotId");

            migrationBuilder.RenameColumn(
                name: "DayOfYear",
                table: "Appointments",
                newName: "Date");

            migrationBuilder.RenameIndex(
                name: "IX_Appointments_DoctorAvailabilityBlockId_DayOfYear",
                table: "Appointments",
                newName: "IX_Appointments_DoctorAvailabilitySlotId_Date");

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "Appointments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "PatientExtraInfo",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientIdentificationNumber",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientName",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientPhone",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_DoctorAvailabilitySlots_DoctorAvailabilitySlot~",
                table: "Appointments",
                column: "DoctorAvailabilitySlotId",
                principalTable: "DoctorAvailabilitySlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_DoctorAvailabilitySlots_DoctorAvailabilitySlot~",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PatientExtraInfo",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PatientIdentificationNumber",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PatientName",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PatientPhone",
                table: "Appointments");

            migrationBuilder.RenameColumn(
                name: "DoctorAvailabilitySlotId",
                table: "Appointments",
                newName: "DoctorAvailabilityBlockId");

            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Appointments",
                newName: "DayOfYear");

            migrationBuilder.RenameIndex(
                name: "IX_Appointments_DoctorAvailabilitySlotId_Date",
                table: "Appointments",
                newName: "IX_Appointments_DoctorAvailabilityBlockId_DayOfYear");

            migrationBuilder.AlterColumn<Guid>(
                name: "PatientId",
                table: "Appointments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_DoctorAvailabilitySlots_DoctorAvailabilityBloc~",
                table: "Appointments",
                column: "DoctorAvailabilityBlockId",
                principalTable: "DoctorAvailabilitySlots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
