using Microsoft.AspNetCore.Mvc;

namespace Modulo5.Web.Controllers;

/// <summary>
/// Destino de <c>app.UseExceptionHandler("/Error")</c> (spec Block 5, "Error handling": "Cualquier
/// excepción no controlada → UseExceptionHandler("/Error"), página genérica"). Deliberadamente no
/// muestra ningún detalle de la excepción ni stack trace — solo un mensaje genérico.
/// </summary>
[Route("Error")]
public class ErrorController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
