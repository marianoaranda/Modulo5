using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Modulo5.Domain.Entities;

namespace Modulo5.Api.Security;

/// <summary>
/// Infraestructura de emisión de JWT (Block 3 del spec FEAT-001a). Toma el <see cref="Usuario"/> ya
/// autenticado por <c>Modulo5.Domain.Security.AuthenticationService</c> y firma el token — Domain no
/// conoce JWT (separación de capas, fix del WARN del arch-audit citado en el spec). Expiración de 60
/// minutos, sin refresh token (AGENTS.md, "Architecture conventions").
///
/// La clave de firma se lee de configuración (`Jwt:SigningKey`, ver Program.cs) — NUNCA hardcodeada
/// ni con un valor por defecto inseguro (mitigación del riesgo #1 del threat model).
/// </summary>
public class JwtTokenGenerator
{
    private const int ExpirationMinutes = 60;

    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        var signingKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "La clave de firma JWT (Jwt:SigningKey) no está configurada.");
        }

        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
    }

    /// <summary>
    /// Emite el JWT firmado para <paramref name="usuario"/>, con claims <c>UsuarioId</c>,
    /// <c>PerfilId</c> y <c>NombreUsuario</c>.
    /// </summary>
    public (string Token, DateTime ExpiraEn) GenerateToken(Usuario usuario)
    {
        var expiraEn = DateTime.UtcNow.AddMinutes(ExpirationMinutes);

        var claims = new[]
        {
            new Claim("UsuarioId", usuario.UsuarioId.ToString()),
            new Claim("PerfilId", usuario.PerfilId.ToString()),
            new Claim("NombreUsuario", usuario.NombreUsuario)
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiraEn,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiraEn);
    }
}
