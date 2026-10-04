using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLatestClinicalAndPracticeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DigitalSignature",
                table: "Prescriptions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalizedAt",
                table: "Prescriptions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFinalized",
                table: "Prescriptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Prescriptions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupersedeReason",
                table: "Prescriptions",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupersededById",
                table: "Prescriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupersedesPrescriptionId",
                table: "Prescriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentSignature",
                table: "Patients",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsentSignedAt",
                table: "Patients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastRestockedAt",
                table: "Materials",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PurchaseOrderRef",
                table: "Materials",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierName",
                table: "Materials",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                table: "Materials",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "DentalLogs",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceId",
                table: "DentalLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stage",
                table: "DentalLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "proposed");

            migrationBuilder.AddColumn<string>(
                name: "BranchCode",
                table: "Clinics",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rooms",
                table: "Clinics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "BillingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VoidReason",
                table: "BillingRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidedAt",
                table: "BillingRecords",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArrivedAt",
                table: "Appointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsultationEndedAt",
                table: "Appointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsultationStartedAt",
                table: "Appointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReminderSentAt",
                table: "Appointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QueueNumber",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderCount",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomNumber",
                table: "Appointments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClinicalNotes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DoctorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClinicId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNoteAmendments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OriginalNoteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AmendedText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AuthorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Timestamp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClinicalNoteId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNoteAmendments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteAmendments_ClinicalNotes_ClinicalNoteId",
                        column: x => x.ClinicalNoteId,
                        principalTable: "ClinicalNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_Status",
                table: "Prescriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId_Status",
                table: "Appointments",
                columns: new[] { "ClinicId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteAmendments_ClinicalNoteId",
                table: "ClinicalNoteAmendments",
                column: "ClinicalNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_ClinicId",
                table: "ClinicalNotes",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_DoctorId",
                table: "ClinicalNotes",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_PatientId",
                table: "ClinicalNotes",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicalNoteAmendments");

            migrationBuilder.DropTable(
                name: "ClinicalNotes");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_Status",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClinicId_Status",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "DigitalSignature",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "IsFinalized",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "SupersedeReason",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "SupersededById",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "SupersedesPrescriptionId",
                table: "Prescriptions");

            migrationBuilder.DropColumn(
                name: "ConsentSignature",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ConsentSignedAt",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "LastRestockedAt",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "PurchaseOrderRef",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "SupplierName",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "DentalLogs");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "DentalLogs");

            migrationBuilder.DropColumn(
                name: "Stage",
                table: "DentalLogs");

            migrationBuilder.DropColumn(
                name: "BranchCode",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "Rooms",
                table: "Clinics");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "BillingRecords");

            migrationBuilder.DropColumn(
                name: "VoidReason",
                table: "BillingRecords");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                table: "BillingRecords");

            migrationBuilder.DropColumn(
                name: "ArrivedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ConsultationEndedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ConsultationStartedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "LastReminderSentAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "QueueNumber",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ReminderCount",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RoomNumber",
                table: "Appointments");
        }
    }
}
