using Microsoft.AspNetCore.Mvc;
using Modulo5.Web.Models;
using Modulo5.Web.Services;

namespace Modulo5.Web.Controllers;

/// <summary>
/// Login (spec Block 5, FR-08/AC-09/AC-10). Llama a <c>POST /api/auth/login</c> de <c>Modulo5.Api</c>
/// vía <see cref="ApiClient"/>; si es exitoso, guarda el JWT en una cookie
/// <c>HttpOnly</c>+<c>Secure</c>+<c>SameSite=Strict</c> (mitigación del riesgo #2 del threat model —
/// nunca en <c>localStorage</c> ni expuesta a JS).
/// </summary>
[Route("Account")]
public class AccountController : Controller
{
    private readonly ApiClient _apiClient;

    public AccountController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("Login")]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _apiClient.LoginAsync(model.Usuario, model.Password, ct);
        if (!result.Success || result.Data is null)
        {
            // Error handling (spec Block 5): re-mostrar Login.cshtml con el mensaje uniforme que
            // devuelve la Api (AC-09) — nunca un mensaje distinto inventado acá.
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Usuario o contraseña incorrectos");
            return View(model);
        }

        Response.Cookies.Append(AuthCookie.Name, result.Data.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = result.Data.ExpiraEn,
            Path = "/"
        });

        return RedirectToAction("Index", "Home");
    }

    [HttpPost("Logout")]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookie.Name);
        return RedirectToAction(nameof(Login));
    }
}
