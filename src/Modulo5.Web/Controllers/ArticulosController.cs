using System.Net;
using Microsoft.AspNetCore.Mvc;
using Modulo5.Web.Models;
using Modulo5.Web.Services;

namespace Modulo5.Web.Controllers;

/// <summary>
/// ABM de Artículos del lado Web (spec Block 3, FR-01 a FR-03/FR-06/FR-07). Llama a
/// <c>Modulo5.Api</c> vía <see cref="ApiClient"/> reenviando el JWT de la cookie; si la Api responde
/// 401 redirige a Login — este controller nunca decide la autorización por sí mismo, solo refleja lo
/// que la Api resuelve (spec Block 3, "Logic").
///
/// A diferencia de <c>UsuariosController</c>, NO se maneja 403: la Api (Block 2) solo exige
/// <c>[Authorize]</c> sin política adicional, así que ese código nunca puede ocurrir para estos
/// endpoints (spec Block 3, "Error handling": "403 no se documenta como caso manejado en este
/// bloque").
///
/// ASSUMPTION documentada en el reporte del bloque: igual que <c>Usuarios</c>, la Api (Block 2) no
/// expone ningún endpoint de listado (<c>GET /api/articulos</c>), así que <see cref="Index"/> no
/// puede mostrar una grilla real de artículos — es un panel de navegación (alta / modificar por
/// Código / eliminar por Código). El route param de <see cref="Edit(string, CancellationToken)"/>/
/// <see cref="Edit(string, ArticuloEditViewModel, CancellationToken)"/>/
/// <see cref="Delete(string, CancellationToken)"/> es <c>{codigo}</c> (string), no <c>{id:int}</c>:
/// <c>Codigo</c> es la PK natural de <c>Articulo</c> (spec Block 1, "Data model").
/// </summary>
[Route("Articulos")]
public class ArticulosController : Controller
{
    private readonly ApiClient _apiClient;

    public ArticulosController(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("Create")]
    public IActionResult Create() => View(new ArticuloCreateViewModel());

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticuloCreateViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new ArticuloRequestDto(
            model.Codigo, model.Descripcion, model.PrecioCosto, model.Margen, model.StockMinimo,
            model.PuntoPedido, model.StockIdeal);
        var result = await _apiClient.CrearArticuloAsync(request, ct);

        var redirect = HandleUnauthorized<ArticuloDto>(result);
        if (redirect is not null)
        {
            return redirect;
        }

        if (!result.Success)
        {
            // spec Block 3, "Error handling": cualquier 400 de la Api (duplicado, negativos,
            // umbrales) se maneja por el mismo code path, sin distinguir causa.
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo crear el artículo.");
            return View(model);
        }

        TempData["Mensaje"] = $"Artículo '{result.Data!.Codigo}' creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{codigo}")]
    public IActionResult Edit(string codigo) => View(new ArticuloEditViewModel { Codigo = codigo });

    [HttpPost("Edit/{codigo}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string codigo, ArticuloEditViewModel model, CancellationToken ct)
    {
        if (codigo != model.Codigo)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new ArticuloRequestDto(
            Codigo: null, model.Descripcion, model.PrecioCosto, model.Margen, model.StockMinimo,
            model.PuntoPedido, model.StockIdeal);
        var result = await _apiClient.ModificarArticuloAsync(codigo, request, ct);

        var redirect = HandleUnauthorized<ArticuloDto>(result);
        if (redirect is not null)
        {
            return redirect;
        }

        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            TempData["Error"] = result.ErrorMessage ?? $"No existe el artículo con Código {codigo}.";
            return RedirectToAction(nameof(Index));
        }

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "No se pudo modificar el artículo.");
            return View(model);
        }

        TempData["Mensaje"] = "Artículo modificado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{codigo}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string codigo, CancellationToken ct)
    {
        var result = await _apiClient.EliminarArticuloAsync(codigo, ct);

        var redirect = HandleUnauthorized<object?>(result);
        if (redirect is not null)
        {
            return redirect;
        }

        if (result.StatusCode == HttpStatusCode.NotFound)
        {
            TempData["Error"] = result.ErrorMessage ?? $"No existe el artículo con Código {codigo}.";
            return RedirectToAction(nameof(Index));
        }

        TempData[result.Success ? "Mensaje" : "Error"] =
            result.Success ? "Artículo eliminado correctamente." : (result.ErrorMessage ?? "No se pudo eliminar el artículo.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Traduce 401→redirect a Login (spec Block 3, "Error handling"). A diferencia de
    /// <c>UsuariosController.HandleUnauthorizedOrForbidden</c>, no maneja 403: no es alcanzable para
    /// estos endpoints (ver comentario de clase). Devuelve <c>null</c> si la respuesta no fue 401 (el
    /// caller sigue con su propio manejo de 400/404/200/204).
    /// </summary>
    private IActionResult? HandleUnauthorized<T>(ApiResult<T> result)
    {
        if (result.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction("Login", "Account");
        }

        return null;
    }
}
