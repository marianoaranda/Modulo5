using Modulo5.Domain.Security;

namespace Modulo5.Domain.Tests.Security;

/// <summary>
/// Tests del Block 2 del spec FEAT-001a: hashing de contraseñas (PBKDF2+salt).
/// </summary>
public class Pbkdf2PasswordHasherTests
{
    [Fact]
    public void Hash_nunca_es_igual_al_password_en_texto_plano_y_el_salt_no_esta_vacio()
    {
        // Arrange — soporta AC-04
        var hasher = new Pbkdf2PasswordHasher();
        const string password = "Password123";

        // Act
        var (hash, salt) = hasher.Hash(password);

        // Assert
        Assert.NotEmpty(hash);
        Assert.NotEmpty(salt);
        Assert.NotEqual(System.Text.Encoding.UTF8.GetBytes(password), hash);
    }

    [Fact]
    public void Dos_usuarios_con_la_misma_password_producen_salts_y_hashes_distintos()
    {
        // Arrange — soporta AC-05
        var hasher = new Pbkdf2PasswordHasher();
        const string password = "Password123";

        // Act
        var (hash1, salt1) = hasher.Hash(password);
        var (hash2, salt2) = hasher.Hash(password);

        // Assert
        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_devuelve_true_para_la_password_correcta_y_false_para_una_incorrecta()
    {
        // Arrange — soporta AC-04
        var hasher = new Pbkdf2PasswordHasher();
        var (hash, salt) = hasher.Hash("Password123");

        // Act & Assert
        Assert.True(hasher.Verify("Password123", hash, salt));
        Assert.False(hasher.Verify("OtraPassword1", hash, salt));
    }
}
