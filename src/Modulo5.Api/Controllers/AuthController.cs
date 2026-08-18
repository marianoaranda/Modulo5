using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Modulo5.Api.Dtos;
using Modulo5.Api.Security;
using Modulo5.Domain.Security;

namespace Modulo5.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly JwtTokenGenerator _tokenGenerator;

    public AuthController(IAuthenticationService authenticationService, JwtTokenGenerator tokenGenerator)
    {
        _authenticationService = authenticationService;
        _tokenGenerator = tokenGenerator;
    }

    /// <summary>
    /// <c>POST /api/auth/login</c> — orquesta <see cref="IAuthenticationService"/> (Domain) +
    /// <see cref="JwtTokenGenerator"/> (Api), sin lógica de validación propia (spec Block 3,
    /// "Logic"). Sin auth requerida (FR-09: único endpoint excluido de la exigencia de JWT).
    /// Sujeto a rate limiting de la política "login" (Program.cs) — mitigación del riesgo #7.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var usuario = await _authenticationService.AuthenticateAsync(request.Usuario, request.Password);
        var (token, expiraEn) = _tokenGenerator.GenerateToken(usuario);

        return Ok(new LoginResponse(token, expiraEn));
    }

    /// <summary>
    /// <c>GET /api/auth/ping</c> — originalmente agregado como endpoint mínimo de diagnóstico para
    /// testear el middleware `JwtBearer` (AC-11: 401 sin token válido / pasa la autenticación con
    /// token válido). Desde FEAT-003, pasa a formar parte del contrato funcional real:
    /// <c>Modulo5.Web</c> lo usa para forzar la validación del JWT en cada visita a
    /// <c>GET /Home</c> (spec FEAT-003 Block 1, FR-01/AC-05), mismo mecanismo de detección de 401
    /// que ya usan las acciones POST de <c>Usuarios</c>/<c>Articulos</c>. Sin cambios de
    /// comportamiento ni de firma respecto de su versión original.
    /// </summary>
    [HttpGet("ping")]
    [Authorize]
    public IActionResult Ping() => Ok();
}
