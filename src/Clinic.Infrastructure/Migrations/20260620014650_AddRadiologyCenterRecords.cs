using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRadiologyCenterRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RadiologyCenters",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadiologyCenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RadiologyRecords",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RadiologyCenterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProcedureName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RadiologyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RadiologyRecords_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadiologyRecords_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RadiologyRecords_RadiologyCenters_RadiologyCenterId",
                        column: x => x.RadiologyCenterId,
                        principalTable: "RadiologyCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyRecords_DoctorId",
                table: "RadiologyRecords",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyRecords_PatientId",
                table: "RadiologyRecords",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_RadiologyRecords_RadiologyCenterId",
                table: "RadiologyRecords",
                column: "RadiologyCenterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RadiologyRecords");

            migrationBuilder.DropTable(
                name: "RadiologyCenters");
        }
    }
}
