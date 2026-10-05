IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [Clinics] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [Phone] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Clinics] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [Doctors] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Specialization] nvarchar(100) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [ContactNumber] nvarchar(50) NOT NULL,
        [Avatar] nvarchar(500) NULL,
        [AvailabilityDays] nvarchar(500) NOT NULL,
        [AvailabilityHours] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_Doctors] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [Patients] (
        [Id] nvarchar(450) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [Gender] nvarchar(20) NOT NULL,
        [DateOfBirth] nvarchar(50) NOT NULL,
        [ContactNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [BloodGroup] nvarchar(10) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [RegistrationDate] nvarchar(50) NOT NULL,
        [ClinicId] nvarchar(450) NULL,
        CONSTRAINT [PK_Patients] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Patients_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [DoctorClinics] (
        [DoctorId] nvarchar(450) NOT NULL,
        [ClinicId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_DoctorClinics] PRIMARY KEY ([DoctorId], [ClinicId]),
        CONSTRAINT [FK_DoctorClinics_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DoctorClinics_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [Appointments] (
        [Id] nvarchar(450) NOT NULL,
        [PatientId] nvarchar(450) NOT NULL,
        [DoctorId] nvarchar(450) NOT NULL,
        [Date] nvarchar(50) NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        [Type] nvarchar(200) NOT NULL,
        [Notes] nvarchar(1000) NOT NULL,
        [ClinicId] nvarchar(450) NULL,
        CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Appointments_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Appointments_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Appointments_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [DentalLogs] (
        [Id] nvarchar(450) NOT NULL,
        [PatientId] nvarchar(450) NOT NULL,
        [ToothNumber] nvarchar(10) NOT NULL,
        [DoctorId] nvarchar(450) NOT NULL,
        [DoctorName] nvarchar(200) NOT NULL,
        [Date] nvarchar(50) NOT NULL,
        [Status] nvarchar(500) NOT NULL,
        [PainLevel] int NOT NULL,
        [PainDetails] nvarchar(1000) NULL,
        [Treatment] nvarchar(500) NULL,
        [Medication] nvarchar(500) NULL,
        CONSTRAINT [PK_DentalLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DentalLogs_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DentalLogs_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [Role] nvarchar(50) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [ClinicId] nvarchar(450) NULL,
        [DoctorId] nvarchar(450) NULL,
        [PatientId] nvarchar(450) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Users_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Users_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [BillingRecords] (
        [Id] nvarchar(450) NOT NULL,
        [PatientId] nvarchar(450) NOT NULL,
        [AppointmentId] nvarchar(450) NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaidAmount] decimal(18,2) NULL,
        [Status] nvarchar(50) NOT NULL,
        [DateIssued] nvarchar(50) NOT NULL,
        [PaymentMethod] nvarchar(100) NULL,
        [Description] nvarchar(500) NULL,
        [ClinicId] nvarchar(450) NULL,
        CONSTRAINT [PK_BillingRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_BillingRecords_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_BillingRecords_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_BillingRecords_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [Prescriptions] (
        [Id] nvarchar(450) NOT NULL,
        [AppointmentId] nvarchar(450) NOT NULL,
        [PatientId] nvarchar(450) NOT NULL,
        [DoctorId] nvarchar(450) NOT NULL,
        [Date] nvarchar(50) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [PK_Prescriptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Prescriptions_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]),
        CONSTRAINT [FK_Prescriptions_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]),
        CONSTRAINT [FK_Prescriptions_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [PaymentLogs] (
        [BillingRecordId] nvarchar(450) NOT NULL,
        [Id] int NOT NULL IDENTITY,
        [Amount] decimal(18,2) NOT NULL,
        [Date] nvarchar(50) NOT NULL,
        [PaymentMethod] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_PaymentLogs] PRIMARY KEY ([BillingRecordId], [Id]),
        CONSTRAINT [FK_PaymentLogs_BillingRecords_BillingRecordId] FOREIGN KEY ([BillingRecordId]) REFERENCES [BillingRecords] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE TABLE [MedicationItems] (
        [PrescriptionId] nvarchar(450) NOT NULL,
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(200) NOT NULL,
        [Dosage] nvarchar(100) NOT NULL,
        [Frequency] nvarchar(100) NOT NULL,
        [Duration] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_MedicationItems] PRIMARY KEY ([PrescriptionId], [Id]),
        CONSTRAINT [FK_MedicationItems_Prescriptions_PrescriptionId] FOREIGN KEY ([PrescriptionId]) REFERENCES [Prescriptions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_ClinicId] ON [Appointments] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_DoctorId] ON [Appointments] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Appointments_PatientId] ON [Appointments] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_BillingRecords_AppointmentId] ON [BillingRecords] ([AppointmentId]) WHERE [AppointmentId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BillingRecords_ClinicId] ON [BillingRecords] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_BillingRecords_PatientId] ON [BillingRecords] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DentalLogs_DoctorId] ON [DentalLogs] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DentalLogs_PatientId] ON [DentalLogs] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DoctorClinics_ClinicId] ON [DoctorClinics] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Patients_ClinicId] ON [Patients] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Prescriptions_AppointmentId] ON [Prescriptions] ([AppointmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_DoctorId] ON [Prescriptions] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_PatientId] ON [Prescriptions] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_ClinicId] ON [Users] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_DoctorId] ON [Users] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_PatientId] ON [Users] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611154151_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260611154151_InitialCreate', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612171216_AddClinicCreatorAndStatus'
)
BEGIN
    ALTER TABLE [DoctorClinics] ADD [Status] nvarchar(50) NOT NULL DEFAULT N'Accepted';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612171216_AddClinicCreatorAndStatus'
)
BEGIN
    ALTER TABLE [Clinics] ADD [CreatorDoctorId] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612171216_AddClinicCreatorAndStatus'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260612171216_AddClinicCreatorAndStatus', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613150550_AddIsPlannedToDentalLog'
)
BEGIN
    ALTER TABLE [DentalLogs] ADD [IsPlanned] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613150550_AddIsPlannedToDentalLog'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260613150550_AddIsPlannedToDentalLog', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613152954_AddPatientAnamnesisToPatient'
)
BEGIN
    ALTER TABLE [Patients] ADD [Allergies] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613152954_AddPatientAnamnesisToPatient'
)
BEGIN
    ALTER TABLE [Patients] ADD [ChronicDiseases] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613152954_AddPatientAnamnesisToPatient'
)
BEGIN
    ALTER TABLE [Patients] ADD [PastIllnesses] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613152954_AddPatientAnamnesisToPatient'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260613152954_AddPatientAnamnesisToPatient', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613161457_AddAvailabilityToDoctorClinic'
)
BEGIN
    ALTER TABLE [DoctorClinics] ADD [AvailabilityDays] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613161457_AddAvailabilityToDoctorClinic'
)
BEGIN
    ALTER TABLE [DoctorClinics] ADD [AvailabilityHours] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613161457_AddAvailabilityToDoctorClinic'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260613161457_AddAvailabilityToDoctorClinic', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614002811_AddAvailabilityToClinics'
)
BEGIN
    ALTER TABLE [Clinics] ADD [AvailabilityDays] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614002811_AddAvailabilityToClinics'
)
BEGIN
    ALTER TABLE [Clinics] ADD [AvailabilityHours] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614002811_AddAvailabilityToClinics'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614002811_AddAvailabilityToClinics', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619151136_AddNotifications'
)
BEGIN
    CREATE TABLE [Notifications] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [Type] nvarchar(50) NOT NULL,
        [IsRead] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619151136_AddNotifications'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619151136_AddNotifications'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260619151136_AddNotifications', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619164805_AddMaterialInventory2'
)
BEGIN
    ALTER TABLE [DentalLogs] ADD [ConsumedMaterials] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619164805_AddMaterialInventory2'
)
BEGIN
    CREATE TABLE [Materials] (
        [Id] nvarchar(450) NOT NULL,
        [DoctorId] nvarchar(450) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Quantity] int NOT NULL,
        [Unit] nvarchar(50) NULL,
        CONSTRAINT [PK_Materials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Materials_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619164805_AddMaterialInventory2'
)
BEGIN
    CREATE INDEX [IX_Materials_DoctorId] ON [Materials] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619164805_AddMaterialInventory2'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260619164805_AddMaterialInventory2', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619170725_AddClinicIdToMaterial'
)
BEGIN
    ALTER TABLE [Materials] ADD [ClinicId] nvarchar(450) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619170725_AddClinicIdToMaterial'
)
BEGIN
    ALTER TABLE [DentalLogs] ADD [ClinicId] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619170725_AddClinicIdToMaterial'
)
BEGIN
    CREATE INDEX [IX_Materials_ClinicId] ON [Materials] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619170725_AddClinicIdToMaterial'
)
BEGIN
    ALTER TABLE [Materials] ADD CONSTRAINT [FK_Materials_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619170725_AddClinicIdToMaterial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260619170725_AddClinicIdToMaterial', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620014650_AddRadiologyCenterRecords'
)
BEGIN
    CREATE TABLE [RadiologyCenters] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ContactNumber] nvarchar(50) NULL,
        [Address] nvarchar(500) NULL,
        CONSTRAINT [PK_RadiologyCenters] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620014650_AddRadiologyCenterRecords'
)
BEGIN
    CREATE TABLE [RadiologyRecords] (
        [Id] nvarchar(450) NOT NULL,
        [DoctorId] nvarchar(450) NOT NULL,
        [PatientId] nvarchar(450) NOT NULL,
        [RadiologyCenterId] nvarchar(450) NOT NULL,
        [ProcedureName] nvarchar(200) NOT NULL,
        [AmountPaid] decimal(18,2) NOT NULL,
        [Date] nvarchar(50) NOT NULL,
        [Notes] nvarchar(1000) NULL,
        CONSTRAINT [PK_RadiologyRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RadiologyRecords_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RadiologyRecords_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RadiologyRecords_RadiologyCenters_RadiologyCenterId] FOREIGN KEY ([RadiologyCenterId]) REFERENCES [RadiologyCenters] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620014650_AddRadiologyCenterRecords'
)
BEGIN
    CREATE INDEX [IX_RadiologyRecords_DoctorId] ON [RadiologyRecords] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620014650_AddRadiologyCenterRecords'
)
BEGIN
    CREATE INDEX [IX_RadiologyRecords_PatientId] ON [RadiologyRecords] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620014650_AddRadiologyCenterRecords'
)
BEGIN
    CREATE INDEX [IX_RadiologyRecords_RadiologyCenterId] ON [RadiologyRecords] ([RadiologyCenterId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620014650_AddRadiologyCenterRecords'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260620014650_AddRadiologyCenterRecords', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620014726_AddRadiologyEntities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260620014726_AddRadiologyEntities', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    EXEC sp_rename N'[Patients].[ContactNumber]', N'PhoneNumber', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    EXEC sp_rename N'[Doctors].[ContactNumber]', N'PhoneNumber', 'COLUMN';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    ALTER TABLE [Patients] ADD [CountryCode] nvarchar(10) NOT NULL DEFAULT N'+20';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    ALTER TABLE [Doctors] ADD [CountryCode] nvarchar(10) NOT NULL DEFAULT N'+20';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    UPDATE Doctors SET CountryCode = '+20', PhoneNumber = SUBSTRING(PhoneNumber, 4, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '+20%' AND LEN(PhoneNumber) > 3
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    UPDATE Doctors SET CountryCode = '+20', PhoneNumber = SUBSTRING(PhoneNumber, 5, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '0020%' AND LEN(PhoneNumber) > 4
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    UPDATE Doctors SET CountryCode = '+1', PhoneNumber = SUBSTRING(PhoneNumber, 3, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '+1%' AND LEN(PhoneNumber) > 2
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    UPDATE Patients SET CountryCode = '+20', PhoneNumber = SUBSTRING(PhoneNumber, 4, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '+20%' AND LEN(PhoneNumber) > 3
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    UPDATE Patients SET CountryCode = '+20', PhoneNumber = SUBSTRING(PhoneNumber, 5, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '0020%' AND LEN(PhoneNumber) > 4
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    UPDATE Patients SET CountryCode = '+1', PhoneNumber = SUBSTRING(PhoneNumber, 3, LEN(PhoneNumber)) WHERE PhoneNumber LIKE '+1%' AND LEN(PhoneNumber) > 2
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620123957_SplitPhoneNumber'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260620123957_SplitPhoneNumber', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620164627_AddUserClinics'
)
BEGIN
    CREATE TABLE [UserClinics] (
        [UserId] nvarchar(450) NOT NULL,
        [ClinicId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_UserClinics] PRIMARY KEY ([UserId], [ClinicId]),
        CONSTRAINT [FK_UserClinics_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserClinics_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620164627_AddUserClinics'
)
BEGIN
    CREATE INDEX [IX_UserClinics_ClinicId] ON [UserClinics] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260620164627_AddUserClinics'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260620164627_AddUserClinics', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621190127_AllowMultipleInvoicesPerAppointment'
)
BEGIN
    DROP INDEX [IX_BillingRecords_AppointmentId] ON [BillingRecords];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621190127_AllowMultipleInvoicesPerAppointment'
)
BEGIN
    CREATE INDEX [IX_BillingRecords_AppointmentId] ON [BillingRecords] ([AppointmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621190127_AllowMultipleInvoicesPerAppointment'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260621190127_AllowMultipleInvoicesPerAppointment', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621190314_UpdateAppointmentBillingRelationship'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260621190314_UpdateAppointmentBillingRelationship', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625095214_AddClinicLocationFields'
)
BEGIN
    ALTER TABLE [Clinics] ADD [City] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625095214_AddClinicLocationFields'
)
BEGIN
    ALTER TABLE [Clinics] ADD [Country] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625095214_AddClinicLocationFields'
)
BEGIN
    ALTER TABLE [Clinics] ADD [Latitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625095214_AddClinicLocationFields'
)
BEGIN
    ALTER TABLE [Clinics] ADD [Longitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625095214_AddClinicLocationFields'
)
BEGIN
    ALTER TABLE [Clinics] ADD [State] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625095214_AddClinicLocationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625095214_AddClinicLocationFields', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [RadiologyCenters] ADD [City] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [RadiologyCenters] ADD [Country] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [RadiologyCenters] ADD [Latitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [RadiologyCenters] ADD [Longitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [RadiologyCenters] ADD [State] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [Patients] ADD [City] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [Patients] ADD [Country] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [Patients] ADD [Latitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [Patients] ADD [Longitude] float NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    ALTER TABLE [Patients] ADD [State] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625104424_AddGlobalLocationFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625104424_AddGlobalLocationFields', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627144137_AddSpecializationsTable'
)
BEGIN
    ALTER TABLE [Doctors] ADD [SpecializationId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627144137_AddSpecializationsTable'
)
BEGIN
    CREATE TABLE [Specializations] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(100) NOT NULL,
        [TranslationKey] nvarchar(100) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Specializations] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627144137_AddSpecializationsTable'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Name', N'TranslationKey') AND [object_id] = OBJECT_ID(N'[Specializations]'))
        SET IDENTITY_INSERT [Specializations] ON;
    EXEC(N'INSERT INTO [Specializations] ([Id], [Category], [Name], [TranslationKey])
    VALUES (N''s1'', N''Dentistry'', N''General Dentistry'', N''auth.spec_general_dentistry''),
    (N''s10'', N''Medicine'', N''Endocrinology'', N''auth.spec_endocrinology''),
    (N''s11'', N''Medicine'', N''Gastroenterology'', N''auth.spec_gastroenterology''),
    (N''s12'', N''Medicine'', N''Neurology'', N''auth.spec_neurology''),
    (N''s13'', N''Medicine'', N''Obstetrics and Gynecology'', N''auth.spec_obgyn''),
    (N''s14'', N''Medicine'', N''Oncology'', N''auth.spec_oncology''),
    (N''s15'', N''Medicine'', N''Ophthalmology'', N''auth.spec_ophthalmology''),
    (N''s16'', N''Medicine'', N''Orthopedics'', N''auth.spec_orthopedics''),
    (N''s17'', N''Medicine'', N''Pediatrics'', N''auth.spec_pediatrics''),
    (N''s18'', N''Medicine'', N''Psychiatry'', N''auth.spec_psychiatry''),
    (N''s19'', N''Medicine'', N''Radiology'', N''auth.spec_radiology''),
    (N''s2'', N''Dentistry'', N''Orthodontics'', N''auth.spec_orthodontics''),
    (N''s20'', N''Medicine'', N''Urology'', N''auth.spec_urology''),
    (N''s21'', N''Medicine'', N''General Practice'', N''auth.spec_general_practice''),
    (N''s3'', N''Dentistry'', N''Oral Surgery'', N''auth.spec_oral_surgery''),
    (N''s4'', N''Dentistry'', N''Endodontics'', N''auth.spec_endodontics''),
    (N''s5'', N''Dentistry'', N''Periodontics'', N''auth.spec_periodontics''),
    (N''s6'', N''Dentistry'', N''Pediatric Dentistry'', N''auth.spec_pediatric_dentistry''),
    (N''s7'', N''Dentistry'', N''Prosthodontics'', N''auth.spec_prosthodontics''),
    (N''s8'', N''Medicine'', N''Cardiology'', N''auth.spec_cardiology''),
    (N''s9'', N''Medicine'', N''Dermatology'', N''auth.spec_dermatology'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Name', N'TranslationKey') AND [object_id] = OBJECT_ID(N'[Specializations]'))
        SET IDENTITY_INSERT [Specializations] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627144137_AddSpecializationsTable'
)
BEGIN
    CREATE INDEX [IX_Doctors_SpecializationId] ON [Doctors] ([SpecializationId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627144137_AddSpecializationsTable'
)
BEGIN
    ALTER TABLE [Doctors] ADD CONSTRAINT [FK_Doctors_Specializations_SpecializationId] FOREIGN KEY ([SpecializationId]) REFERENCES [Specializations] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260627144137_AddSpecializationsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260627144137_AddSpecializationsTable', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    ALTER TABLE [Doctors] ADD [AppliedPromoCode] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    ALTER TABLE [Doctors] ADD [CreatedAt] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    ALTER TABLE [Doctors] ADD [IsInitialFeePaid] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    ALTER TABLE [Doctors] ADD [SubscriptionEndDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    ALTER TABLE [Doctors] ADD [SubscriptionStatus] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    ALTER TABLE [Doctors] ADD [TrialEndDate] datetime2 NOT NULL DEFAULT '0001-01-01T00:00:00.0000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    CREATE TABLE [PromoCodes] (
        [Id] nvarchar(450) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [DiscountType] nvarchar(50) NOT NULL,
        [Value] float NOT NULL,
        [ExpiryDate] datetime2 NOT NULL,
        [MaxUses] int NOT NULL,
        [CurrentUses] int NOT NULL,
        [IsActive] bit NOT NULL,
        CONSTRAINT [PK_PromoCodes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    CREATE TABLE [SubscriptionSettings] (
        [Id] nvarchar(450) NOT NULL,
        [InitialSetupFee] decimal(18,2) NOT NULL,
        [AnnualSubscriptionFee] decimal(18,2) NOT NULL,
        [TrialDurationMonths] int NOT NULL,
        CONSTRAINT [PK_SubscriptionSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CurrentUses', N'DiscountType', N'ExpiryDate', N'IsActive', N'MaxUses', N'Value') AND [object_id] = OBJECT_ID(N'[PromoCodes]'))
        SET IDENTITY_INSERT [PromoCodes] ON;
    EXEC(N'INSERT INTO [PromoCodes] ([Id], [Code], [CurrentUses], [DiscountType], [ExpiryDate], [IsActive], [MaxUses], [Value])
    VALUES (N''p1'', N''FREE3MONTHS'', 0, N''FreeMonths'', ''2028-01-01T00:00:00.0000000Z'', CAST(1 AS bit), 100, 3.0E0),
    (N''p2'', N''SAVE50'', 0, N''Flat'', ''2028-01-01T00:00:00.0000000Z'', CAST(1 AS bit), 100, 50.0E0),
    (N''p3'', N''HALFPRICE'', 0, N''Percent'', ''2028-01-01T00:00:00.0000000Z'', CAST(1 AS bit), 100, 50.0E0)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Code', N'CurrentUses', N'DiscountType', N'ExpiryDate', N'IsActive', N'MaxUses', N'Value') AND [object_id] = OBJECT_ID(N'[PromoCodes]'))
        SET IDENTITY_INSERT [PromoCodes] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AnnualSubscriptionFee', N'InitialSetupFee', N'TrialDurationMonths') AND [object_id] = OBJECT_ID(N'[SubscriptionSettings]'))
        SET IDENTITY_INSERT [SubscriptionSettings] ON;
    EXEC(N'INSERT INTO [SubscriptionSettings] ([Id], [AnnualSubscriptionFee], [InitialSetupFee], [TrialDurationMonths])
    VALUES (N''s_default'', 300.0, 100.0, 6)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AnnualSubscriptionFee', N'InitialSetupFee', N'TrialDurationMonths') AND [object_id] = OBJECT_ID(N'[SubscriptionSettings]'))
        SET IDENTITY_INSERT [SubscriptionSettings] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PromoCodes_Code] ON [PromoCodes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260701192630_AddDoctorSubscriptionFieldsAndSettings'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260701192630_AddDoctorSubscriptionFieldsAndSettings', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702022244_AddReceiptUrlToDoctor'
)
BEGIN
    ALTER TABLE [Doctors] ADD [ReceiptUrl] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702022244_AddReceiptUrlToDoctor'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702022244_AddReceiptUrlToDoctor', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702120139_AddSubscriptionReceiptsTable'
)
BEGIN
    CREATE TABLE [SubscriptionReceipts] (
        [Id] nvarchar(450) NOT NULL,
        [DoctorId] nvarchar(100) NOT NULL,
        [ReceiptUrl] nvarchar(1000) NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        [Status] nvarchar(50) NOT NULL,
        CONSTRAINT [PK_SubscriptionReceipts] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702120139_AddSubscriptionReceiptsTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702120139_AddSubscriptionReceiptsTable', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702160115_AddSoftDelete'
)
BEGIN
    ALTER TABLE [Users] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702160115_AddSoftDelete'
)
BEGIN
    ALTER TABLE [Patients] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702160115_AddSoftDelete'
)
BEGIN
    ALTER TABLE [Doctors] ADD [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702160115_AddSoftDelete'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702160115_AddSoftDelete', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    ALTER TABLE [DoctorClinics] DROP CONSTRAINT [FK_DoctorClinics_Doctors_DoctorId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DROP INDEX [IX_Notifications_UserId] ON [Notifications];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DROP INDEX [IX_Appointments_ClinicId] ON [Appointments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DROP INDEX [IX_Appointments_DoctorId] ON [Appointments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DECLARE @var sysname;
    SELECT @var = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RadiologyRecords]') AND [c].[name] = N'PatientId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [RadiologyRecords] DROP CONSTRAINT [' + @var + '];');
    ALTER TABLE [RadiologyRecords] ALTER COLUMN [PatientId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RadiologyRecords]') AND [c].[name] = N'DoctorId');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [RadiologyRecords] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [RadiologyRecords] ALTER COLUMN [DoctorId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Prescriptions]') AND [c].[name] = N'PatientId');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Prescriptions] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [Prescriptions] ALTER COLUMN [PatientId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Prescriptions]') AND [c].[name] = N'DoctorId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Prescriptions] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [Prescriptions] ALTER COLUMN [DoctorId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    ALTER TABLE [Materials] ADD [MinStockAlert] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DentalLogs]') AND [c].[name] = N'PatientId');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [DentalLogs] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [DentalLogs] ALTER COLUMN [PatientId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BillingRecords]') AND [c].[name] = N'PatientId');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [BillingRecords] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [BillingRecords] ALTER COLUMN [PatientId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'PatientId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Appointments] ALTER COLUMN [PatientId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'DoctorId');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [Appointments] ALTER COLUMN [DoctorId] nvarchar(450) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_Date] ON [Prescriptions] ([Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    CREATE INDEX [IX_Patients_CountryCode_PhoneNumber] ON [Patients] ([CountryCode], [PhoneNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    CREATE INDEX [IX_Patients_IsDeleted] ON [Patients] ([IsDeleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_CreatedAt] ON [Notifications] ([UserId], [CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    CREATE INDEX [IX_DentalLogs_PatientId_ToothNumber] ON [DentalLogs] ([PatientId], [ToothNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    CREATE INDEX [IX_BillingRecords_Status] ON [BillingRecords] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    CREATE INDEX [IX_Appointments_ClinicId_Date] ON [Appointments] ([ClinicId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    CREATE INDEX [IX_Appointments_DoctorId_Date] ON [Appointments] ([DoctorId], [Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    ALTER TABLE [DoctorClinics] ADD CONSTRAINT [FK_DoctorClinics_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260927213144_AddMinStockAlertToMaterial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260927213144_AddMinStockAlertToMaterial', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003135003_AddExpirationDateAndBatchToMaterial'
)
BEGIN
    ALTER TABLE [Materials] ADD [BatchNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003135003_AddExpirationDateAndBatchToMaterial'
)
BEGIN
    ALTER TABLE [Materials] ADD [ExpirationDate] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003135003_AddExpirationDateAndBatchToMaterial'
)
BEGIN
    ALTER TABLE [BillingRecords] ADD [DiscountAmount] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003135003_AddExpirationDateAndBatchToMaterial'
)
BEGIN
    ALTER TABLE [BillingRecords] ADD [DiscountAuthorizedBy] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003135003_AddExpirationDateAndBatchToMaterial'
)
BEGIN
    ALTER TABLE [BillingRecords] ADD [DiscountPercentage] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003135003_AddExpirationDateAndBatchToMaterial'
)
BEGIN
    ALTER TABLE [BillingRecords] ADD [DiscountReason] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003135003_AddExpirationDateAndBatchToMaterial'
)
BEGIN
    ALTER TABLE [BillingRecords] ADD [Subtotal] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261003135003_AddExpirationDateAndBatchToMaterial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261003135003_AddExpirationDateAndBatchToMaterial', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [DigitalSignature] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [FinalizedAt] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [IsFinalized] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [Status] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [SupersedeReason] nvarchar(1000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [SupersededById] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Prescriptions] ADD [SupersedesPrescriptionId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Patients] ADD [ConsentSignature] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Patients] ADD [ConsentSignedAt] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Materials] ADD [LastRestockedAt] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Materials] ADD [PurchaseOrderRef] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Materials] ADD [SupplierName] nvarchar(200) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Materials] ADD [UnitCost] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [DentalLogs] ADD [Cost] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [DentalLogs] ADD [InvoiceId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [DentalLogs] ADD [Stage] nvarchar(50) NOT NULL DEFAULT N'proposed';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Clinics] ADD [BranchCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Clinics] ADD [Rooms] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [BillingRecords] ADD [InvoiceNumber] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [BillingRecords] ADD [VoidReason] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [BillingRecords] ADD [VoidedAt] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Appointments] ADD [ArrivedAt] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Appointments] ADD [ConsultationEndedAt] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Appointments] ADD [ConsultationStartedAt] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Appointments] ADD [LastReminderSentAt] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Appointments] ADD [QueueNumber] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Appointments] ADD [ReminderCount] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    ALTER TABLE [Appointments] ADD [RoomNumber] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
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
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    CREATE INDEX [IX_Prescriptions_Status] ON [Prescriptions] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    CREATE INDEX [IX_Appointments_ClinicId_Status] ON [Appointments] ([ClinicId], [Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    CREATE INDEX [IX_ClinicalNoteAmendments_ClinicalNoteId] ON [ClinicalNoteAmendments] ([ClinicalNoteId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    CREATE INDEX [IX_ClinicalNotes_ClinicId] ON [ClinicalNotes] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    CREATE INDEX [IX_ClinicalNotes_DoctorId] ON [ClinicalNotes] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    CREATE INDEX [IX_ClinicalNotes_PatientId] ON [ClinicalNotes] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004092801_AddLatestClinicalAndPracticeFields'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004092801_AddLatestClinicalAndPracticeFields', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005042552_AddCategoryAndIsDefaultToMaterial'
)
BEGIN
    ALTER TABLE [Materials] ADD [Category] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005042552_AddCategoryAndIsDefaultToMaterial'
)
BEGIN
    ALTER TABLE [Materials] ADD [IsDefault] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005042552_AddCategoryAndIsDefaultToMaterial'
)
BEGIN
    DROP INDEX [IX_ClinicalNotes_PatientId] ON [ClinicalNotes];
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClinicalNotes]') AND [c].[name] = N'PatientId');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [ClinicalNotes] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [ClinicalNotes] ALTER COLUMN [PatientId] nvarchar(450) NOT NULL;
    CREATE INDEX [IX_ClinicalNotes_PatientId] ON [ClinicalNotes] ([PatientId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005042552_AddCategoryAndIsDefaultToMaterial'
)
BEGIN
    DROP INDEX [IX_ClinicalNotes_DoctorId] ON [ClinicalNotes];
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClinicalNotes]') AND [c].[name] = N'DoctorId');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [ClinicalNotes] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [ClinicalNotes] ALTER COLUMN [DoctorId] nvarchar(450) NOT NULL;
    CREATE INDEX [IX_ClinicalNotes_DoctorId] ON [ClinicalNotes] ([DoctorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005042552_AddCategoryAndIsDefaultToMaterial'
)
BEGIN
    DROP INDEX [IX_ClinicalNotes_ClinicId] ON [ClinicalNotes];
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClinicalNotes]') AND [c].[name] = N'ClinicId');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [ClinicalNotes] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [ClinicalNotes] ALTER COLUMN [ClinicId] nvarchar(450) NULL;
    CREATE INDEX [IX_ClinicalNotes_ClinicId] ON [ClinicalNotes] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005042552_AddCategoryAndIsDefaultToMaterial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005042552_AddCategoryAndIsDefaultToMaterial', N'9.0.20');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005054124_AddEquipmentTable'
)
BEGIN
    CREATE TABLE [Equipment] (
        [Id] nvarchar(450) NOT NULL,
        [ClinicId] nvarchar(450) NOT NULL,
        [DoctorId] nvarchar(max) NULL,
        [Name] nvarchar(200) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [SerialNumber] nvarchar(100) NULL,
        [ModelNumber] nvarchar(100) NULL,
        [Manufacturer] nvarchar(100) NULL,
        [RoomOrChair] nvarchar(100) NULL,
        [Status] nvarchar(50) NOT NULL DEFAULT N'Operational',
        [PurchaseCost] decimal(18,2) NULL,
        [PurchaseDate] datetime2 NULL,
        [WarrantyExpiryDate] datetime2 NULL,
        [LastMaintenanceDate] datetime2 NULL,
        [NextMaintenanceDate] datetime2 NULL,
        [MaintenanceNotes] nvarchar(max) NULL,
        [ServiceProvider] nvarchar(200) NULL,
        [ServiceContactPhone] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Equipment] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Equipment_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005054124_AddEquipmentTable'
)
BEGIN
    CREATE INDEX [IX_Equipment_ClinicId] ON [Equipment] ([ClinicId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005054124_AddEquipmentTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005054124_AddEquipmentTable', N'9.0.20');
END;

COMMIT;
GO

