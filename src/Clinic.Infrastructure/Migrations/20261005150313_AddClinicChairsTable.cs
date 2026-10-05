using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicChairsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClinicChairs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClinicId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChairName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "available"),
                    CurrentPatientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentPatientName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentDoctorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CurrentDoctorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ProcedureName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OccupancyStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CleaningStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicChairs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicChairs_ClinicId",
                table: "ClinicChairs",
                column: "ClinicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicChairs");
        }
    }
}
