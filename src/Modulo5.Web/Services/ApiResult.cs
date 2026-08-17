using System.Net;

namespace Modulo5.Web.Services;

/// <summary>
/// Envuelve la respuesta de <see cref="ApiClient"/> con el <see cref="HttpStatusCode"/> real devuelto
/// por <c>Modulo5.Api</c>, para que los controllers de <c>Modulo5.Web</c> reaccionen a él (redirigir a
/// Login en 401, mostrar "Acceso denegado" en 403, etc.) sin decidir la autorización por sí mismos
/// (spec Block 5, "Logic": "Web nunca decide la autorización por sí mismo, solo refleja lo que la Api
/// resuelve").
/// </summary>
public class ApiResult<T>
{
    private ApiResult(bool success, HttpStatusCode statusCode, T? data, string? errorMessage)
    {
        Success = success;
        StatusCode = statusCode;
        Data = data;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }

    public HttpStatusCode StatusCode { get; }

    public T? Data { get; }

    public string? ErrorMessage { get; }

    public static ApiResult<T> Ok(T? data, HttpStatusCode statusCode) => new(true, statusCode, data, null);

    public static ApiResult<T> Fail(HttpStatusCode statusCode, string? errorMessage) =>
        new(false, statusCode, default, errorMessage);
}
