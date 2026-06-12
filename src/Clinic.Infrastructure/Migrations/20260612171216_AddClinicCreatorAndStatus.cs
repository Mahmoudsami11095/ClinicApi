using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicCreatorAndStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DoctorClinics",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Accepted");

            migrationBuilder.AddColumn<string>(
                name: "CreatorDoctorId",
                table: "Clinics",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "DoctorClinics");

            migrationBuilder.DropColumn(
                name: "CreatorDoctorId",
                table: "Clinics");
        }
    }
}
