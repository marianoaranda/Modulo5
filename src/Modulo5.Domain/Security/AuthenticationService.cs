using Modulo5.Domain.Entities;
using Modulo5.Domain.Exceptions;
using Modulo5.Domain.Repositories;

namespace Modulo5.Domain.Security;

/// <inheritdoc cref="IAuthenticationService" />
public class AuthenticationService : IAuthenticationService
{
    /// <summary>
    /// Mensaje exacto de AC-09 — igual para usuario inexistente y para contraseña incorrecta
    /// (mitigación de riesgo #8 del threat model: evita revelar cuál de los dos casos ocurrió).
    /// </summary>
    private const string MensajeCredencialesInvalidas = "Usuario o contraseña incorrectos";

    /// <summary>
    /// Hash y salt "dummy" fijos (no derivados de ningún usuario real), usados para invocar
    /// <see cref="IPasswordHasher.Verify"/> cuando el usuario no existe.
    ///
    /// Corrección de seguridad (ronda 2 de revisión del Block 3): el operador `||` de C# hace
    /// short-circuit, así que `candidato is null || !Verify(...)` nunca ejecutaba `Verify()` para un
    /// usuario inexistente, mientras que sí lo hacía completo (210.000 iteraciones de PBKDF2) para
    /// una password incorrecta de un usuario existente. Esa asimetría de costo computacional crea un
    /// canal de timing que permite enumerar usuarios midiendo tiempos de respuesta —
    /// `CryptographicOperations.FixedTimeEquals` dentro de `Verify()` (riesgo #8 del threat model,
    /// docs/daw/security/threat-FEAT-001a.md) solo protege la comparación de bytes, no si `Verify()`
    /// se invoca o no. El fix invoca `Verify()` siempre, contra estos valores fijos cuando no hay
    /// candidato real, para igualar el costo de CPU de ambos caminos. El resultado contra estos
    /// valores dummy siempre da `false` (no corresponden a ningún password real).
    /// </summary>
    private static readonly byte[] HashDummy = CrearBytesFijos(64);
    private static readonly byte[] SaltDummy = CrearBytesFijos(16);

    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticationService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Usuario> AuthenticateAsync(string usuario, string password)
    {
        var candidato = await _usuarioRepository.GetByUsuarioAsync(usuario);

        var hash = candidato?.Hash ?? HashDummy;
        var salt = candidato?.Salt ?? SaltDummy;

        // Se invoca Verify() SIEMPRE (aunque candidato sea null) para que el costo computacional sea
        // el mismo en ambos casos — ver comentario de HashDummy/SaltDummy arriba.
        var passwordValida = _passwordHasher.Verify(password, hash, salt);

        if (candidato is null || !passwordValida)
        {
            throw new UnauthorizedDomainException(MensajeCredencialesInvalidas);
        }

        return candidato;
    }

    private static byte[] CrearBytesFijos(int longitud)
    {
        var bytes = new byte[longitud];
        for (var i = 0; i < longitud; i++)
        {
            bytes[i] = (byte)(i + 1);
        }

        return bytes;
    }
}
