using System.Net;
using Modulo5.Domain.Exceptions;

namespace Modulo5.Api.Middleware;

/// <summary>
/// Middleware único de manejo de errores (Block 3 del spec FEAT-001a): traduce excepciones de
/// dominio tipadas a códigos HTTP y nunca expone el stack trace (AGENTS.md, "Manejo de errores";
/// threat model, Information Disclosure de <c>Modulo5.Api</c>).
///
/// ASSUMPTION documentada en el reporte del bloque: el spec (Block 3, sección "Logic") lista el
/// mapeo genérico "<see cref="UnauthorizedDomainException"/> → 401", pero la sección "Error
/// handling" del mismo bloque, el contrato de la Api y los 2 tests requeridos de login inválido son
/// explícitos: el login con usuario inexistente o contraseña incorrecta (única fuente de esta
/// excepción en este bloque) debe responder 400 con el mensaje uniforme de AC-09 ("el PRD especifica
/// este caso como 400 lógico de aplicación, no 401 HTTP, porque no hay sesión previa que rechazar").
/// Se resuelve mapeando <see cref="UnauthorizedDomainException"/> → 400, que es lo exigido por
/// contrato/tests/AC-09. El 401 de AC-11 para endpoints protegidos sin JWT válido lo produce
/// nativamente el middleware de `JwtBearer` (no pasa por este catch, porque nunca llega a lanzarse
/// una excepción de dominio en ese caso).
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (UnauthorizedDomainException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Excepción no controlada procesando {Method} {Path}",
                context.Request.Method,
                context.Request.Path);
            await WriteErrorAsync(
                context,
                HttpStatusCode.InternalServerError,
                "Ocurrió un error interno. Intente nuevamente más tarde.");
        }
    }

    private static Task WriteErrorAsync(HttpContext context, HttpStatusCode statusCode, string mensaje)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsJsonAsync(new { mensaje });
    }
}
