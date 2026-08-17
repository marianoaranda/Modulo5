using System.Net;
using Microsoft.AspNetCore.Mvc;
using Modulo5.Web.Models;
using Modulo5.Web.Services;

namespace Modulo5.Web.Controllers;

/// <summary>
/// ABM de Usuarios del lado Web (spec Block 5, FR-01 a FR-03/FR-07). Llama a <c>Modulo5.Api</c> vía
/// <see cref="ApiClient"/> reenviando el JWT de la cookie; SI la Api responde 401 redirige a Login, si
/// responde 403 muestra "Acceso denegado" — este controller nunca decide la autorización por sí mismo,
/// solo refleja lo que la Api resuelve (spec Block 5, "Logic").
///
/// ASSUMPTION documentada en el reporte del bloque: el contrato de la Api (Block 4) no expone ningún
/// endpoint de listado (<c>GET /api/usuarios</c>), así que <see cref="Index"/> no puede mostrar una
/// grilla real de usuarios — es un panel de navegación (alta / modificar por Id / eliminar por Id).
/// Por el mismo motivo, <see cref="Index"/> no llama a la Api: no hay ninguna operación protegida que
/// ejecutar solo por entrar a la pantalla. La autorización real (redirect a Login / "Acceso denegado")
/// se ejerce en el momento en que se envía una acción real (Create/Edit/Delete POST), que es cuando
/// existe una llamada HTTP cuya respuesta la Api puede resolver.
/// </summary>
[Route("Usuarios")]
public class UsuariosController : Controller
{
    private readonly ApiClient _apiClient;

    public UsuariosController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("Create")]
    public IActionResult Create() => View(new UsuarioCreateViewModel());

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UsuarioCreateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new UsuarioRequestDto(model.Usuario, model.NombreCompleto, model.Password, model.PerfilId);
        var result = await _apiClient.CrearUsuarioAsync(request, ct);

        var redirect = HandleUnauthorizedOrForbidden<UsuarioDto>(result);
        if (redirect is not null)
        {
            return redirect;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo crear el usuario.");
            return View(model);
        }

        TempData["Mensaje"] = $"Usuario '{result.Data!.Usuario}' creado correctamente (UsuarioId {result.Data.UsuarioId}).";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:int}")]
    public IActionResult Edit(int id) => View(new UsuarioEditViewModel { UsuarioId = id });

    [HttpPost("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UsuarioEditViewModel model, CancellationToken ct)
    {
        if (id != model.UsuarioId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new UsuarioRequestDto(Usuario: null, model.NombreCompleto, model.Password, model.PerfilId);
        var result = await _apiClient.ModificarUsuarioAsync(id, request, ct);

        var redirect = HandleUnauthorizedOrForbidden<UsuarioDto>(result);
        if (redirect is not null)
        {
            return redirect;
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo modificar el usuario.");
            return View(model);
        }

        TempData["Mensaje"] = "Usuario modificado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _apiClient.EliminarUsuarioAsync(id, ct);

        var redirect = HandleUnauthorizedOrForbidden<object?>(result);
        if (redirect is not null)
        {
            return redirect;
        }

        TempData[result.Success ? "Mensaje" : "Error"] =
            result.Success ? "Usuario eliminado correctamente." : (result.ErrorMessage ?? "No se pudo eliminar el usuario.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Traduce 401→redirect a Login y 403→vista "Acceso denegado" (spec Block 5, "Error handling").
    /// Devuelve <c>null</c> si la respuesta no fue ni 401 ni 403 (el caller sigue con su propio manejo
    /// de 400/404/200).
    /// </summary>
    private IActionResult? HandleUnauthorizedOrForbidden<T>(ApiResult<T> result)
    {
        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        if (result.StatusCode == HttpStatusCode.Forbidden)
        {
            return View("AccesoDenegado");
        }

        return null;
    }
}
