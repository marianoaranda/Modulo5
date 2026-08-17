namespace Modulo5.Domain.Security;

/// <summary>
/// Abstrae la generación y verificación de hashes de contraseña. Ver
/// <see cref="Pbkdf2PasswordHasher"/> para la implementación (PBKDF2+salt, Block 2 del spec
/// FEAT-001a) y el threat model (riesgos #6 y #8) para las mitigaciones que exige.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Genera un salt aleatorio y deriva el hash de <paramref name="password"/> con él.
    /// </summary>
    (byte[] Hash, byte[] Salt) Hash(string password);

    /// <summary>
    /// Deriva el hash de <paramref name="password"/> con el <paramref name="salt"/> dado y lo
    /// compara en tiempo constante contra <paramref name="hash"/>.
    /// </summary>
    bool Verify(string password, byte[] hash, byte[] salt);
}
