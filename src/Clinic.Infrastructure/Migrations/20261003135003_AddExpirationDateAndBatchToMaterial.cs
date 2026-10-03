using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpirationDateAndBatchToMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BatchNumber",
                table: "Materials",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpirationDate",
                table: "Materials",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "BillingRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountAuthorizedBy",
                table: "BillingRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "BillingRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountReason",
                table: "BillingRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "BillingRecords",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchNumber",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "ExpirationDate",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "BillingRecords");

            migrationBuilder.DropColumn(
                name: "DiscountAuthorizedBy",
                table: "BillingRecords");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "BillingRecords");

            migrationBuilder.DropColumn(
                name: "DiscountReason",
                table: "BillingRecords");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "BillingRecords");
        }
    }
}
