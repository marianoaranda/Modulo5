using System.Security.Cryptography;

namespace Modulo5.Domain.Security;

/// <summary>
/// Implementación de <see cref="IPasswordHasher"/> con PBKDF2-HMAC-SHA256 y salt aleatorio de 16
/// bytes por usuario (AGENTS.md, sección "Architecture conventions").
///
/// Las 210.000 iteraciones son una mitigación explícita del riesgo #6 del threat model
/// (docs/daw/security/threat-FEAT-001a.md): por debajo de la recomendación OWASP 2023 el hash es
/// crackeable offline en tiempos prácticos si la base se filtra. NO reducir este valor.
///
/// <see cref="Verify"/> compara con <see cref="CryptographicOperations.FixedTimeEquals"/> (tiempo
/// constante) — mitigación del riesgo #8, evita filtrar por timing si la contraseña candidata es
/// parcialmente correcta.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSizeInBytes = 16;
    private const int HashSizeInBytes = 64; // coincide con Usuario.Hash varbinary(64) (Block 1)
    private const int Iterations = 210_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public (byte[] Hash, byte[] Salt) Hash(string password)
    {
        var salt = new byte[SaltSizeInBytes];
        RandomNumberGenerator.Fill(salt);

        var hash = DeriveHash(password, salt);

        return (hash, salt);
    }

    public bool Verify(string password, byte[] hash, byte[] salt)
    {
        var candidateHash = DeriveHash(password, salt);

        return CryptographicOperations.FixedTimeEquals(candidateHash, hash);
    }

    private static byte[] DeriveHash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSizeInBytes);
}
