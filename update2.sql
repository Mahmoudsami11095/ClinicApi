BEGIN TRANSACTION;
ALTER TABLE [Doctors] ADD [SpecializationId] nvarchar(450) NULL;

CREATE TABLE [Specializations] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [TranslationKey] nvarchar(100) NOT NULL,
    [Category] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Specializations] PRIMARY KEY ([Id])
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Name', N'TranslationKey') AND [object_id] = OBJECT_ID(N'[Specializations]'))
    SET IDENTITY_INSERT [Specializations] ON;
INSERT INTO [Specializations] ([Id], [Category], [Name], [TranslationKey])
VALUES (N's1', N'Dentistry', N'General Dentistry', N'auth.spec_general_dentistry'),
(N's10', N'Medicine', N'Endocrinology', N'auth.spec_endocrinology'),
(N's11', N'Medicine', N'Gastroenterology', N'auth.spec_gastroenterology'),
(N's12', N'Medicine', N'Neurology', N'auth.spec_neurology'),
(N's13', N'Medicine', N'Obstetrics and Gynecology', N'auth.spec_obgyn'),
(N's14', N'Medicine', N'Oncology', N'auth.spec_oncology'),
(N's15', N'Medicine', N'Ophthalmology', N'auth.spec_ophthalmology'),
(N's16', N'Medicine', N'Orthopedics', N'auth.spec_orthopedics'),
(N's17', N'Medicine', N'Pediatrics', N'auth.spec_pediatrics'),
(N's18', N'Medicine', N'Psychiatry', N'auth.spec_psychiatry'),
(N's19', N'Medicine', N'Radiology', N'auth.spec_radiology'),
(N's2', N'Dentistry', N'Orthodontics', N'auth.spec_orthodontics'),
(N's20', N'Medicine', N'Urology', N'auth.spec_urology'),
(N's21', N'Medicine', N'General Practice', N'auth.spec_general_practice'),
(N's3', N'Dentistry', N'Oral Surgery', N'auth.spec_oral_surgery'),
(N's4', N'Dentistry', N'Endodontics', N'auth.spec_endodontics'),
(N's5', N'Dentistry', N'Periodontics', N'auth.spec_periodontics'),
(N's6', N'Dentistry', N'Pediatric Dentistry', N'auth.spec_pediatric_dentistry'),
(N's7', N'Dentistry', N'Prosthodontics', N'auth.spec_prosthodontics'),
(N's8', N'Medicine', N'Cardiology', N'auth.spec_cardiology'),
(N's9', N'Medicine', N'Dermatology', N'auth.spec_dermatology');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'Name', N'TranslationKey') AND [object_id] = OBJECT_ID(N'[Specializations]'))
    SET IDENTITY_INSERT [Specializations] OFF;

CREATE INDEX [IX_Doctors_SpecializationId] ON [Doctors] ([SpecializationId]);

ALTER TABLE [Doctors] ADD CONSTRAINT [FK_Doctors_Specializations_SpecializationId] FOREIGN KEY ([SpecializationId]) REFERENCES [Specializations] ([Id]) ON DELETE SET NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260627144137_AddSpecializationsTable', N'9.0.17');

COMMIT;
GO


