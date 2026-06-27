using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSpecializationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpecializationId",
                table: "Doctors",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Specializations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TranslationKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specializations", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Specializations",
                columns: new[] { "Id", "Category", "Name", "TranslationKey" },
                values: new object[,]
                {
                    { "s1", "Dentistry", "General Dentistry", "auth.spec_general_dentistry" },
                    { "s10", "Medicine", "Endocrinology", "auth.spec_endocrinology" },
                    { "s11", "Medicine", "Gastroenterology", "auth.spec_gastroenterology" },
                    { "s12", "Medicine", "Neurology", "auth.spec_neurology" },
                    { "s13", "Medicine", "Obstetrics and Gynecology", "auth.spec_obgyn" },
                    { "s14", "Medicine", "Oncology", "auth.spec_oncology" },
                    { "s15", "Medicine", "Ophthalmology", "auth.spec_ophthalmology" },
                    { "s16", "Medicine", "Orthopedics", "auth.spec_orthopedics" },
                    { "s17", "Medicine", "Pediatrics", "auth.spec_pediatrics" },
                    { "s18", "Medicine", "Psychiatry", "auth.spec_psychiatry" },
                    { "s19", "Medicine", "Radiology", "auth.spec_radiology" },
                    { "s2", "Dentistry", "Orthodontics", "auth.spec_orthodontics" },
                    { "s20", "Medicine", "Urology", "auth.spec_urology" },
                    { "s21", "Medicine", "General Practice", "auth.spec_general_practice" },
                    { "s3", "Dentistry", "Oral Surgery", "auth.spec_oral_surgery" },
                    { "s4", "Dentistry", "Endodontics", "auth.spec_endodontics" },
                    { "s5", "Dentistry", "Periodontics", "auth.spec_periodontics" },
                    { "s6", "Dentistry", "Pediatric Dentistry", "auth.spec_pediatric_dentistry" },
                    { "s7", "Dentistry", "Prosthodontics", "auth.spec_prosthodontics" },
                    { "s8", "Medicine", "Cardiology", "auth.spec_cardiology" },
                    { "s9", "Medicine", "Dermatology", "auth.spec_dermatology" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_SpecializationId",
                table: "Doctors",
                column: "SpecializationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Doctors_Specializations_SpecializationId",
                table: "Doctors",
                column: "SpecializationId",
                principalTable: "Specializations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Doctors_Specializations_SpecializationId",
                table: "Doctors");

            migrationBuilder.DropTable(
                name: "Specializations");

            migrationBuilder.DropIndex(
                name: "IX_Doctors_SpecializationId",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "SpecializationId",
                table: "Doctors");
        }
    }
}
