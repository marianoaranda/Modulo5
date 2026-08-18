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
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817125913_InitialCreate'
)
BEGIN
    CREATE TABLE [Perfiles] (
        [PerfilId] int NOT NULL IDENTITY,
        [Descripcion] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Perfiles] PRIMARY KEY ([PerfilId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817125913_InitialCreate'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [UsuarioId] int NOT NULL IDENTITY,
        [Usuario] nvarchar(50) NOT NULL,
        [NombreCompleto] nvarchar(150) NOT NULL,
        [Hash] varbinary(64) NOT NULL,
        [Salt] varbinary(16) NOT NULL,
        [PerfilId] int NOT NULL,
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([UsuarioId]),
        CONSTRAINT [FK_Usuarios_Perfiles_PerfilId] FOREIGN KEY ([PerfilId]) REFERENCES [Perfiles] ([PerfilId]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817125913_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PerfilId', N'Descripcion') AND [object_id] = OBJECT_ID(N'[Perfiles]'))
        SET IDENTITY_INSERT [Perfiles] ON;
    EXEC(N'INSERT INTO [Perfiles] ([PerfilId], [Descripcion])
    VALUES (1, N''administrador'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PerfilId', N'Descripcion') AND [object_id] = OBJECT_ID(N'[Perfiles]'))
        SET IDENTITY_INSERT [Perfiles] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817125913_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Usuario_Usuario] ON [Usuarios] ([Usuario]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817125913_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Usuarios_PerfilId] ON [Usuarios] ([PerfilId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260817125913_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260817125913_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

