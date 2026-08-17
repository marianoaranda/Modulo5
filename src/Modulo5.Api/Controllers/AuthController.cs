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
    /// <c>GET /api/auth/ping</c> — endpoint mínimo de diagnóstico, agregado EXCLUSIVAMENTE para
    /// poder testear el middleware `JwtBearer` (AC-11: 401 sin token válido / pasa la autenticación
    /// con token válido). No forma parte del contrato de negocio de este ticket.
    /// </summary>
    [HttpGet("ping")]
    [Authorize]
    public IActionResult Ping() => Ok();
}
