using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleInvoicesPerAppointment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillingRecords_AppointmentId",
                table: "BillingRecords");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_AppointmentId",
                table: "BillingRecords",
                column: "AppointmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BillingRecords_AppointmentId",
                table: "BillingRecords");

            migrationBuilder.CreateIndex(
                name: "IX_BillingRecords_AppointmentId",
                table: "BillingRecords",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");
        }
    }
}
