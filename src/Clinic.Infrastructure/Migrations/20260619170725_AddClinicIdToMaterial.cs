using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicIdToMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClinicId",
                table: "Materials",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ClinicId",
                table: "DentalLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materials_ClinicId",
                table: "Materials",
                column: "ClinicId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Clinics_ClinicId",
                table: "Materials",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Clinics_ClinicId",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_Materials_ClinicId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "DentalLogs");
        }
    }
}
