using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContactNumber",
                table: "Patients",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "ContactNumber",
                table: "Doctors",
                newName: "PhoneNumber");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Patients",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "+20");

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Doctors",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "+20");

            // Migrate existing doctor records
            migrationBuilder.Sql("UPDATE Doctors SET CountryCode = '+20', PhoneNumber = SUBSTRING(PhoneNumber, 4, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '+20%' AND LEN(PhoneNumber) > 3");
            migrationBuilder.Sql("UPDATE Doctors SET CountryCode = '+20', PhoneNumber = SUBSTRING(PhoneNumber, 5, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '0020%' AND LEN(PhoneNumber) > 4");
            migrationBuilder.Sql("UPDATE Doctors SET CountryCode = '+1', PhoneNumber = SUBSTRING(PhoneNumber, 3, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '+1%' AND LEN(PhoneNumber) > 2");

            // Migrate existing patient records
            migrationBuilder.Sql("UPDATE Patients SET CountryCode = '+20', PhoneNumber = SUBSTRING(PhoneNumber, 4, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '+20%' AND LEN(PhoneNumber) > 3");
            migrationBuilder.Sql("UPDATE Patients SET CountryCode = '+20', PhoneNumber = SUBSTRING(PhoneNumber, 5, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '0020%' AND LEN(PhoneNumber) > 4");
            migrationBuilder.Sql("UPDATE Patients SET CountryCode = '+1', PhoneNumber = SUBSTRING(PhoneNumber, 3, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '+1%' AND LEN(PhoneNumber) > 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "CountryCode",
                table: "Doctors");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Patients",
                newName: "ContactNumber");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "Doctors",
                newName: "ContactNumber");
        }
    }
}
