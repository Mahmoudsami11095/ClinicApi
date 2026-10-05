BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005103638_AddImageUrlToEquipmentAndMaterial'
)
BEGIN
    ALTER TABLE [Materials] ADD [ImageUrl] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005103638_AddImageUrlToEquipmentAndMaterial'
)
BEGIN
    ALTER TABLE [Equipment] ADD [ImageUrl] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005103638_AddImageUrlToEquipmentAndMaterial'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005103638_AddImageUrlToEquipmentAndMaterial', N'9.0.20');
END;

COMMIT;
GO

