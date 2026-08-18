using System.Net;
using Microsoft.AspNetCore.Mvc;
using Modulo5.Web.Services;

namespace Modulo5.Web.Controllers;

/// <summary>
/// Punto de entrada post-login (spec FEAT-003 Block 1, FR-01/FR-02). A diferencia de los <c>GET</c>
/// actuales de <see cref="UsuariosController"/>/<see cref="ArticulosController"/> (que no llaman a la
/// Api), el <c>GET</c> de Home llama a <c>GET /api/auth/ping</c> vía <see cref="ApiClient.PingAsync"/>
/// para forzar la validación del JWT en cada visita — si la Api responde 401 redirige a Login, mismo
/// mecanismo de detección de 401 que ya usan Usuarios/Articulos (spec, "Logic").
///
/// Sin <c>[Authorize]</c>, mismo criterio que <see cref="UsuariosController"/>/
/// <see cref="ArticulosController"/>: <c>Modulo5.Web</c> no decide autorización por sí mismo, solo
/// refleja lo que la Api resuelve (ver <c>Program.cs:46-48</c>).
///
/// <c>api/auth/ping</c> no tiene política de autorización adicional a <c>[Authorize]</c> (a
/// diferencia de <c>api/usuarios</c>, que exige <c>AdminOnly</c>), así que — igual que
/// <see cref="ArticulosController"/> — no se maneja 403 acá, solo 401.
/// </summary>
[Route("Home")]
public class HomeController : Controller
{
    private readonly ApiClient _apiClient;

    public HomeController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _apiClient.PingAsync(ct);

        var redirect = HandleUnauthorized(result);
        if (redirect is not null)
        {
            return redirect;
        }

        return View();
    }

    /// <summary>
    /// Traduce 401→redirect a Login, mismo patrón que
    /// <c>ArticulosController.HandleUnauthorized</c> (no <c>HandleUnauthorizedOrForbidden</c> de
    /// <c>UsuariosController</c>: acá no hay política adicional que pueda devolver 403). Devuelve
    /// <c>null</c> si la respuesta no fue 401.
    /// </summary>
    private IActionResult? HandleUnauthorized(ApiResult<object?> result)
    {
        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        return null;
    }
}
