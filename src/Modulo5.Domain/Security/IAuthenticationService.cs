using Modulo5.Domain.Entities;

namespace Modulo5.Domain.Security;

/// <summary>
/// Autenticación de usuario/contraseña (Block 3 del spec FEAT-001a). Vive en Domain, no en Api: es
/// lógica de negocio pura (usa <see cref="Repositories.IUsuarioRepository"/> +
/// <see cref="IPasswordHasher"/>) y NO conoce JWT ni ningún tipo de <c>Microsoft.AspNetCore.*</c> —
/// la emisión del token es infraestructura y vive en <c>Modulo5.Api.Security.JwtTokenGenerator</c>.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Valida <paramref name="usuario"/>/<paramref name="password"/> contra el hash almacenado.
    /// Devuelve el <see cref="Usuario"/> autenticado o lanza
    /// <see cref="Exceptions.UnauthorizedDomainException"/> con el mensaje uniforme de AC-09 si el
    /// usuario no existe o la contraseña es incorrecta (mismo mensaje para ambos casos, para no
    /// permitir enumeración de usuarios — riesgo #8 del threat model).
    /// </summary>
    Task<Usuario> AuthenticateAsync(string usuario, string password);
}
