using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilityToDoctorClinic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvailabilityDays",
                table: "DoctorClinics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityHours",
                table: "DoctorClinics",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailabilityDays",
                table: "DoctorClinics");

            migrationBuilder.DropColumn(
                name: "AvailabilityHours",
                table: "DoctorClinics");
        }
    }
}
