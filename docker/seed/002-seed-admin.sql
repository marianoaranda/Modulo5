-- Seed de un usuario administrador de arranque para pruebas manuales locales (Docker).
-- NO es parte de la migración de EF Core del ticket (Block 1 solo siembra el perfil
-- "administrador", sin ningún Usuario — no hay endpoint público de alta, y crear un Usuario
-- requiere ya ser administrador, así que hace falta este bootstrap fuera de la app para
-- poder loguearse la primera vez).
--
-- Usuario: admin
-- Password: Admin12345 (cumple PasswordPolicy: >=8 caracteres alfanuméricos)
-- Hash/Salt precalculados con el mismo algoritmo de Pbkdf2PasswordHasher (PBKDF2-HMAC-SHA256,
-- 210.000 iteraciones, salt 16 bytes, hash 64 bytes) y verificados byte a byte contra
-- Rfc2898DeriveBytes.Pbkdf2 antes de commitear este script.
IF NOT EXISTS (SELECT 1 FROM [Usuarios] WHERE [Usuario] = N'admin')
BEGIN
    INSERT INTO [Usuarios] ([Usuario], [NombreCompleto], [Hash], [Salt], [PerfilId])
    VALUES (
        N'admin',
        N'Administrador',
        0x10687c07de1851650cb24466c67bed4d0825c5a434c35f1ab3311cd05c9c038f5879c692259435eac57aee8263da8c031645ea7fba01320dd82cdab0bdd1f86e,
        0xf2c25c8c81320510f3fa776d42f3ce82,
        1
    );
END;
GO
