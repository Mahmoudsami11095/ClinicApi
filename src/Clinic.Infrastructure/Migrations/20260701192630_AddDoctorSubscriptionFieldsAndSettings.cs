using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorSubscriptionFieldsAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliedPromoCode",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Doctors",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsInitialFeePaid",
                table: "Doctors",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubscriptionEndDate",
                table: "Doctors",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubscriptionStatus",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TrialEndDate",
                table: "Doctors",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "PromoCodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiscountType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: false),
                    CurrentUses = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    InitialSetupFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AnnualSubscriptionFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrialDurationMonths = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionSettings", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "PromoCodes",
                columns: new[] { "Id", "Code", "CurrentUses", "DiscountType", "ExpiryDate", "IsActive", "MaxUses", "Value" },
                values: new object[,]
                {
                    { "p1", "FREE3MONTHS", 0, "FreeMonths", new DateTime(2028, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 100, 3.0 },
                    { "p2", "SAVE50", 0, "Flat", new DateTime(2028, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 100, 50.0 },
                    { "p3", "HALFPRICE", 0, "Percent", new DateTime(2028, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, 100, 50.0 }
                });

            migrationBuilder.InsertData(
                table: "SubscriptionSettings",
                columns: new[] { "Id", "AnnualSubscriptionFee", "InitialSetupFee", "TrialDurationMonths" },
                values: new object[] { "s_default", 300.00m, 100.00m, 6 });

            migrationBuilder.CreateIndex(
                name: "IX_PromoCodes_Code",
                table: "PromoCodes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromoCodes");

            migrationBuilder.DropTable(
                name: "SubscriptionSettings");

            migrationBuilder.DropColumn(
                name: "AppliedPromoCode",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "IsInitialFeePaid",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "SubscriptionEndDate",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "SubscriptionStatus",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "TrialEndDate",
                table: "Doctors");
        }
    }
}
