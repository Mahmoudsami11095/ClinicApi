-- =========================================================================================
-- Azure SQL Migration: AddPublicBookingAndEnterpriseTables (Release v5.5.0 - v6.0.0)
-- Description: Idempotent migration to add missing columns to Clinics, Users and create
--              Tenants, Suppliers, PurchaseOrders, PurchaseOrderItems, StandbyQueue, WebAuthn.
-- =========================================================================================

BEGIN TRANSACTION;

-- 1. Tenants Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tenants')
BEGIN
    CREATE TABLE [Tenants] (
        [Id] nvarchar(450) NOT NULL,
        [OrganizationName] nvarchar(200) NOT NULL,
        [Subdomain] nvarchar(100) NOT NULL,
        [SubscriptionTier] nvarchar(50) NOT NULL DEFAULT 'Starter',
        [StripeSubscriptionId] nvarchar(200) NOT NULL DEFAULT '',
        [IsActive] bit NOT NULL DEFAULT 1,
        [LogoUrl] nvarchar(500) NULL,
        [PrimaryColorHex] nvarchar(50) NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );

    CREATE UNIQUE INDEX [IX_Tenants_Subdomain] ON [Tenants] ([Subdomain]);
END;

-- 2. Missing Columns on Clinics
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clinics') AND name = 'Slug')
BEGIN
    ALTER TABLE [Clinics] ADD [Slug] nvarchar(200) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clinics') AND name = 'PublicBookingEnabled')
BEGIN
    ALTER TABLE [Clinics] ADD [PublicBookingEnabled] bit NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clinics') AND name = 'QrPosterAssetUrl')
BEGIN
    ALTER TABLE [Clinics] ADD [QrPosterAssetUrl] nvarchar(500) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Clinics') AND name = 'TenantId')
BEGIN
    ALTER TABLE [Clinics] ADD [TenantId] nvarchar(450) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Clinics') AND name = 'IX_Clinics_Slug')
BEGIN
    CREATE UNIQUE INDEX [IX_Clinics_Slug] ON [Clinics] ([Slug]) WHERE [Slug] IS NOT NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Clinics') AND name = 'IX_Clinics_TenantId')
BEGIN
    CREATE INDEX [IX_Clinics_TenantId] ON [Clinics] ([TenantId]);
END;

-- 3. Missing Columns on Users
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'TenantId')
BEGIN
    ALTER TABLE [Users] ADD [TenantId] nvarchar(450) NULL;
END;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'BiometricEnabled')
BEGIN
    ALTER TABLE [Users] ADD [BiometricEnabled] bit NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID('Users') AND name = 'IX_Users_TenantId')
BEGIN
    CREATE INDEX [IX_Users_TenantId] ON [Users] ([TenantId]);
END;

-- 4. Suppliers Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suppliers')
BEGIN
    CREATE TABLE [Suppliers] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [ContactEmail] nvarchar(200) NOT NULL,
        [Phone] nvarchar(50) NOT NULL,
        [LeadTimeDays] int NOT NULL DEFAULT 3,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Suppliers] PRIMARY KEY ([Id])
    );
END;

-- 5. PurchaseOrders Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrders')
BEGIN
    CREATE TABLE [PurchaseOrders] (
        [Id] nvarchar(450) NOT NULL,
        [PONumber] nvarchar(100) NOT NULL,
        [ClinicId] nvarchar(450) NOT NULL,
        [SupplierId] nvarchar(450) NOT NULL,
        [Status] nvarchar(50) NOT NULL DEFAULT 'Draft',
        [TotalAmount] decimal(18,2) NOT NULL DEFAULT 0.00,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [DispatchedAt] datetime2 NULL,
        [ReceivedAt] datetime2 NULL,
        CONSTRAINT [PK_PurchaseOrders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseOrders_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]),
        CONSTRAINT [FK_PurchaseOrders_Suppliers_SupplierId] FOREIGN KEY ([SupplierId]) REFERENCES [Suppliers] ([Id])
    );

    CREATE INDEX [IX_PurchaseOrders_ClinicId] ON [PurchaseOrders] ([ClinicId]);
    CREATE INDEX [IX_PurchaseOrders_SupplierId] ON [PurchaseOrders] ([SupplierId]);
END;

-- 6. PurchaseOrderItems Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrderItems')
BEGIN
    CREATE TABLE [PurchaseOrderItems] (
        [Id] nvarchar(450) NOT NULL,
        [PurchaseOrderId] nvarchar(450) NOT NULL,
        [MaterialId] nvarchar(450) NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        CONSTRAINT [PK_PurchaseOrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PurchaseOrderItems_Materials_MaterialId] FOREIGN KEY ([MaterialId]) REFERENCES [Materials] ([Id]),
        CONSTRAINT [FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId] FOREIGN KEY ([PurchaseOrderId]) REFERENCES [PurchaseOrders] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_PurchaseOrderItems_MaterialId] ON [PurchaseOrderItems] ([MaterialId]);
    CREATE INDEX [IX_PurchaseOrderItems_PurchaseOrderId] ON [PurchaseOrderItems] ([PurchaseOrderId]);
END;

-- 7. StandbyQueue Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StandbyQueue')
BEGIN
    CREATE TABLE [StandbyQueue] (
        [Id] nvarchar(450) NOT NULL,
        [ClinicId] nvarchar(450) NOT NULL,
        [DoctorId] nvarchar(450) NOT NULL,
        [PatientId] nvarchar(450) NOT NULL,
        [RequestedDate] nvarchar(20) NOT NULL,
        [Status] nvarchar(50) NOT NULL DEFAULT 'Waiting',
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_StandbyQueue] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StandbyQueue_Clinics_ClinicId] FOREIGN KEY ([ClinicId]) REFERENCES [Clinics] ([Id]),
        CONSTRAINT [FK_StandbyQueue_Doctors_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [Doctors] ([Id]),
        CONSTRAINT [FK_StandbyQueue_Patients_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_StandbyQueue_ClinicId_DoctorId_RequestedDate_Status] ON [StandbyQueue] ([ClinicId], [DoctorId], [RequestedDate], [Status]);
    CREATE INDEX [IX_StandbyQueue_DoctorId] ON [StandbyQueue] ([DoctorId]);
    CREATE INDEX [IX_StandbyQueue_PatientId] ON [StandbyQueue] ([PatientId]);
END;

-- 8. WebAuthnCredentials Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WebAuthnCredentials')
BEGIN
    CREATE TABLE [WebAuthnCredentials] (
        [Id] nvarchar(450) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [CredentialId] varbinary(max) NOT NULL,
        [PublicKey] varbinary(max) NOT NULL,
        [SignCount] bigint NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_WebAuthnCredentials] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WebAuthnCredentials_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_WebAuthnCredentials_UserId] ON [WebAuthnCredentials] ([UserId]);
END;

-- 9. Backfill existing Clinics with default Slug if null
UPDATE [Clinics]
SET [Slug] = LOWER(REPLACE(REPLACE(REPLACE([Name], ' ', '-'), '.', ''), '/', '-'))
WHERE [Slug] IS NULL AND [Name] IS NOT NULL;

-- 10. Record in EF Migrations History
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261013210000_AddPublicBookingAndEnterpriseTables')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013210000_AddPublicBookingAndEnterpriseTables', N'9.0.20');
END;

COMMIT;
GO
