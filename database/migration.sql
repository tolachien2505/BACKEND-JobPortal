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
CREATE TABLE [Categories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NULL,
    [ParentId] int NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Categories_Categories_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Role] nvarchar(20) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

CREATE TABLE [RoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_RoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RoleClaims_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Companies] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [CompanyName] nvarchar(200) NOT NULL,
    [Description] nvarchar(500) NULL,
    [Logo] nvarchar(200) NULL,
    [Website] nvarchar(200) NULL,
    [Address] nvarchar(200) NULL,
    [Industry] nvarchar(100) NULL,
    [CompanySize] nvarchar(50) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Companies] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Companies_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserClaims_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_UserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_UserLogins_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [UserTokens] (
    [UserId] int NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_UserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_UserTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Jobs] (
    [Id] int NOT NULL IDENTITY,
    [CompanyId] int NOT NULL,
    [CategoryId] int NOT NULL,
    [Title] nvarchar(250) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Requirements] nvarchar(500) NULL,
    [Location] nvarchar(200) NULL,
    [SalaryMin] decimal(18,2) NULL,
    [SalaryMax] decimal(18,2) NULL,
    [JobType] nvarchar(50) NULL,
    [ExperienceLevel] nvarchar(50) NULL,
    [Vacancies] int NULL,
    [PostedDate] datetime2 NOT NULL,
    [ExpiryDate] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [IsFeatured] bit NOT NULL,
    CONSTRAINT [PK_Jobs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Jobs_Categories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Jobs_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Applications] (
    [Id] int NOT NULL IDENTITY,
    [JobId] int NOT NULL,
    [UserId] int NOT NULL,
    [CoverLetter] nvarchar(max) NULL,
    [ResumePath] nvarchar(100) NULL,
    [AppliedDate] datetime2 NOT NULL,
    [Status] nvarchar(50) NOT NULL,
    [InterviewDate] datetime2 NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Applications_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Applications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [SavedJobs] (
    [Id] int NOT NULL IDENTITY,
    [JobId] int NOT NULL,
    [UserId] int NOT NULL,
    [SavedDate] datetime2 NOT NULL,
    CONSTRAINT [PK_SavedJobs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_SavedJobs_Jobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [Jobs] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_SavedJobs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name', N'ParentId') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] ON;
INSERT INTO [Categories] ([Id], [Description], [Name], [ParentId])
VALUES (1, NULL, N'Công nghệ thông tin', NULL),
(2, NULL, N'Kinh doanh / Marketing', NULL),
(3, NULL, N'Kế toán / Tài chính', NULL),
(4, NULL, N'Nhân sự / Hành chính', NULL),
(5, NULL, N'Kỹ thuật', NULL),
(6, NULL, N'Giáo dục / Đào tạo', NULL),
(7, NULL, N'Y tế / Chăm sóc sức khỏe', NULL),
(8, NULL, N'Du lịch / Khách sạn', NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Description', N'Name', N'ParentId') AND [object_id] = OBJECT_ID(N'[Categories]'))
    SET IDENTITY_INSERT [Categories] OFF;

CREATE INDEX [IX_Applications_JobId] ON [Applications] ([JobId]);

CREATE INDEX [IX_Applications_UserId] ON [Applications] ([UserId]);

CREATE INDEX [IX_Categories_ParentId] ON [Categories] ([ParentId]);

CREATE UNIQUE INDEX [IX_Companies_UserId] ON [Companies] ([UserId]);

CREATE INDEX [IX_Jobs_CategoryId] ON [Jobs] ([CategoryId]);

CREATE INDEX [IX_Jobs_CompanyId] ON [Jobs] ([CompanyId]);

CREATE INDEX [IX_RoleClaims_RoleId] ON [RoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [Roles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_SavedJobs_JobId] ON [SavedJobs] ([JobId]);

CREATE INDEX [IX_SavedJobs_UserId] ON [SavedJobs] ([UserId]);

CREATE INDEX [IX_UserClaims_UserId] ON [UserClaims] ([UserId]);

CREATE INDEX [IX_UserLogins_UserId] ON [UserLogins] ([UserId]);

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [Users] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [Users] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260312071446_InitialCreate', N'10.0.0');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Jobs] ADD [ModeratedAt] datetime2 NULL;

ALTER TABLE [Jobs] ADD [ModeratedByUserId] int NULL;

ALTER TABLE [Jobs] ADD [ModerationNote] nvarchar(500) NULL;

ALTER TABLE [Jobs] ADD [ModerationStatus] nvarchar(20) NOT NULL DEFAULT N'Pending';

DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Applications]') AND [c].[name] = N'ResumePath');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Applications] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Applications] ALTER COLUMN [ResumePath] nvarchar(255) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260312082307_AddModerationFieldsToJob', N'10.0.0');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [UserCvs] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [FileName] nvarchar(255) NOT NULL,
    [StoredPath] nvarchar(500) NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    [IsDefault] bit NOT NULL,
    CONSTRAINT [PK_UserCvs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserCvs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_UserCvs_UserId] ON [UserCvs] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260312105052_AddUserCv', N'10.0.0');

COMMIT;
GO

