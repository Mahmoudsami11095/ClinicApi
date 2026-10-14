-- =========================================================================================
-- Azure SQL Migration: AddMobileDevicesTable (Release v7.0.0 Mobile Application Suite)
-- Description: Idempotent migration to add MobileDevices table for APNs/FCM push token sync.
-- =========================================================================================

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MobileDevices')
BEGIN
    CREATE TABLE [MobileDevices] (
        [Id] nvarchar(450) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [DeviceToken] nvarchar(500) NOT NULL,
        [Platform] nvarchar(50) NOT NULL DEFAULT 'web',
        [AppVersion] nvarchar(50) NOT NULL DEFAULT '7.0.0',
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [LastActiveAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_MobileDevices] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MobileDevices_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_MobileDevices_UserId_DeviceToken] ON [MobileDevices] ([UserId], [DeviceToken]);
END;

IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20261014060000_AddMobileDevicesTable')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261014060000_AddMobileDevicesTable', N'9.0.20');
END;

COMMIT;
GO
