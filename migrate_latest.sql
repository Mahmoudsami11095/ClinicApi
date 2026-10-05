BEGIN TRANSACTION;
ALTER TABLE [Prescriptions] ADD [DigitalSignature] nvarchar(500) NULL;

ALTER TABLE [Prescriptions] ADD [FinalizedAt] nvarchar(50) NULL;

ALTER TABLE [Prescriptions] ADD [IsFinalized] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Prescriptions] ADD [Status] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Prescriptions] ADD [SupersedeReason] nvarchar(1000) NULL;

ALTER TABLE [Prescriptions] ADD [SupersededById] nvarchar(100) NULL;

ALTER TABLE [Prescriptions] ADD [SupersedesPrescriptionId] nvarchar(100) NULL;

ALTER TABLE [Patients] ADD [ConsentSignature] nvarchar(max) NULL;

ALTER TABLE [Patients] ADD [ConsentSignedAt] nvarchar(50) NULL;

ALTER TABLE [Materials] ADD [LastRestockedAt] nvarchar(50) NULL;

ALTER TABLE [Materials] ADD [PurchaseOrderRef] nvarchar(100) NULL;

ALTER TABLE [Materials] ADD [SupplierName] nvarchar(200) NULL;

ALTER TABLE [Materials] ADD [UnitCost] decimal(18,2) NULL;

ALTER TABLE [DentalLogs] ADD [Cost] decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE [DentalLogs] ADD [InvoiceId] nvarchar(100) NULL;

ALTER TABLE [DentalLogs] ADD [Stage] nvarchar(50) NOT NULL DEFAULT N'proposed';

ALTER TABLE [Clinics] ADD [BranchCode] nvarchar(50) NULL;

ALTER TABLE [Clinics] ADD [Rooms] nvarchar(500) NULL;

ALTER TABLE [BillingRecords] ADD [InvoiceNumber] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [BillingRecords] ADD [VoidReason] nvarchar(max) NULL;

ALTER TABLE [BillingRecords] ADD [VoidedAt] nvarchar(max) NULL;

ALTER TABLE [Appointments] ADD [ArrivedAt] nvarchar(50) NULL;

ALTER TABLE [Appointments] ADD [ConsultationEndedAt] nvarchar(50) NULL;

ALTER TABLE [Appointments] ADD [ConsultationStartedAt] nvarchar(50) NULL;

ALTER TABLE [Appointments] ADD [LastReminderSentAt] nvarchar(50) NULL;

ALTER TABLE [Appointments] ADD [QueueNumber] int NULL;

ALTER TABLE [Appointments] ADD [ReminderCount] int NULL;

ALTER TABLE [Appointments] ADD [RoomNumber] nvarchar(100) NULL;

CREATE TABLE [ClinicalNotes] (
    [Id] nvarchar(450) NOT NULL,
    [PatientId] nvarchar(450) NOT NULL,
    [DoctorId] nvarchar(450) NOT NULL,
    [DoctorName] nvarchar(200) NOT NULL,
    [ClinicId] nvarchar(450) NULL,
    [CreatedAt] nvarchar(50) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    [Notes] nvarchar(4000) NOT NULL,
    CONSTRAINT [PK_ClinicalNotes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClinicalNotes_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_ClinicalNotes_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id])
);

CREATE TABLE [ClinicalNoteAmendments] (
    [Id] nvarchar(450) NOT NULL,
    [OriginalNoteId] nvarchar(100) NOT NULL,
    [AmendedText] nvarchar(4000) NOT NULL,
    [Reason] nvarchar(500) NULL,
    [AuthorId] nvarchar(100) NOT NULL,
    [AuthorName] nvarchar(200) NOT NULL,
    [Timestamp] nvarchar(50) NOT NULL,
    [ClinicalNoteId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_ClinicalNoteAmendments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ClinicalNoteAmendments_ClinicalNotes_ClinicalNoteId] FOREIGN KEY ([ClinicalNoteId]) REFERENCES [ClinicalNotes] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Prescriptions_Status] ON [Prescriptions] ([Status]);

CREATE INDEX [IX_Appointments_ClinicId_Status] ON [Appointments] ([ClinicId], [Status]);

CREATE INDEX [IX_ClinicalNoteAmendments_ClinicalNoteId] ON [ClinicalNoteAmendments] ([ClinicalNoteId]);

CREATE INDEX [IX_ClinicalNotes_ClinicId] ON [ClinicalNotes] ([ClinicId]);

CREATE INDEX [IX_ClinicalNotes_DoctorId] ON [ClinicalNotes] ([DoctorId]);

CREATE INDEX [IX_ClinicalNotes_PatientId] ON [ClinicalNotes] ([PatientId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261004092801_AddLatestClinicalAndPracticeFields', N'9.0.20');

COMMIT;
GO

