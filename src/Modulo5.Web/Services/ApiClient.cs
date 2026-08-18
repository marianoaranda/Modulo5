using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Modulo5.Web.Services;

/// <summary>
/// Encapsula TODAS las llamadas HTTP de <c>Modulo5.Web</c> a <c>Modulo5.Api</c> (spec Block 5,
/// "Files"). Adjunta el JWT leído de la cookie <see cref="AuthCookie.Name"/> como header
/// <c>Authorization: Bearer</c> en las operaciones del ABM de Usuarios; si la cookie no existe,
/// deliberadamente NO se agrega el header y se deja que sea la Api quien responda 401 — esta clase
/// nunca decide la autorización por sí misma, solo refleja lo que la Api resuelve (spec Block 5,
/// "Logic").
///
/// Las excepciones de conectividad (p. ej. <see cref="HttpRequestException"/> si la Api no está
/// disponible) NO se atrapan acá a propósito: deben burbujear hasta el middleware
/// <c>UseExceptionHandler("/Error")</c> de <c>Program.cs</c> (spec Block 5, "Error handling" — último
/// test manual requerido del bloque).
/// </summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary><c>POST /api/auth/login</c> — sin JWT (es el propio login). Ver Block 3 del spec.</summary>
    public async Task<ApiResult<LoginResultDto>> LoginAsync(
        string usuario, string password, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new { usuario, password }, options: JsonOptions)
        };

        using var response = await _httpClient.SendAsync(request, ct);
        return await BuildResultAsync<LoginResultDto>(response, ct);
    }

    /// <summary><c>POST /api/usuarios</c> — alta (FR-01/AC-01). Requiere JWT + perfil administrador
    /// (política <c>AdminOnly</c> de la Api).</summary>
    public Task<ApiResult<UsuarioDto>> CrearUsuarioAsync(UsuarioRequestDto request, CancellationToken ct = default) =>
        SendAuthenticatedAsync<UsuarioDto>(HttpMethod.Post, "api/usuarios", request, ct);

    /// <summary><c>PUT /api/usuarios/{id}</c> — modificación (FR-03/AC-03).</summary>
    public Task<ApiResult<UsuarioDto>> ModificarUsuarioAsync(
        int id, UsuarioRequestDto request, CancellationToken ct = default) =>
        SendAuthenticatedAsync<UsuarioDto>(HttpMethod.Put, $"api/usuarios/{id}", request, ct);

    /// <summary><c>DELETE /api/usuarios/{id}</c> — baja (FR-02/AC-02).</summary>
    public Task<ApiResult<object?>> EliminarUsuarioAsync(int id, CancellationToken ct = default) =>
        SendAuthenticatedAsync<object?>(HttpMethod.Delete, $"api/usuarios/{id}", body: null, ct);

    /// <summary><c>POST /api/articulos</c> — alta (spec Block 3, FR-01/AC-01). Requiere JWT (sin
    /// política adicional, a diferencia de Usuarios).</summary>
    public Task<ApiResult<ArticuloDto>> CrearArticuloAsync(ArticuloRequestDto request, CancellationToken ct = default) =>
        SendAuthenticatedAsync<ArticuloDto>(HttpMethod.Post, "api/articulos", request, ct);

    /// <summary><c>PUT /api/articulos/{codigo}</c> — modificación (spec Block 3, FR-03/AC-03).</summary>
    public Task<ApiResult<ArticuloDto>> ModificarArticuloAsync(
        string codigo, ArticuloRequestDto request, CancellationToken ct = default) =>
        SendAuthenticatedAsync<ArticuloDto>(HttpMethod.Put, $"api/articulos/{Uri.EscapeDataString(codigo)}", request, ct);

    /// <summary><c>DELETE /api/articulos/{codigo}</c> — baja (spec Block 3, FR-02/AC-02).</summary>
    public Task<ApiResult<object?>> EliminarArticuloAsync(string codigo, CancellationToken ct = default) =>
        SendAuthenticatedAsync<object?>(HttpMethod.Delete, $"api/articulos/{Uri.EscapeDataString(codigo)}", body: null, ct);

    /// <summary><c>GET /api/auth/ping</c> — forzar la validación del JWT en cada visita a Home
    /// (spec FEAT-003 Block 1, FR-01). Sin body de request ni de response, mismo patrón que
    /// <see cref="EliminarUsuarioAsync"/>.</summary>
    public Task<ApiResult<object?>> PingAsync(CancellationToken ct = default) =>
        SendAuthenticatedAsync<object?>(HttpMethod.Get, "api/auth/ping", body: null, ct);

    private async Task<ApiResult<T>> SendAuthenticatedAsync<T>(
        HttpMethod method, string uri, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, uri);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        var token = _httpContextAccessor.HttpContext?.Request.Cookies[AuthCookie.Name];
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await _httpClient.SendAsync(request, ct);
        return await BuildResultAsync<T>(response, ct);
    }

    private static async Task<ApiResult<T>> BuildResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = default(T);
            if (response.Content.Headers.ContentLength is > 0)
            {
                data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            }

            return ApiResult<T>.Ok(data, response.StatusCode);
        }

        string? mensaje = null;
        try
        {
            var error = await response.Content.ReadFromJsonAsync<ApiErrorDto>(JsonOptions, ct);
            mensaje = error?.Mensaje;
        }
        catch (JsonException)
        {
            // Cuerpo no-JSON (p. ej. un 401 emitido directamente por el middleware JwtBearer, sin
            // body) — se ignora, el StatusCode alcanza para que el controller reaccione.
        }

        return ApiResult<T>.Fail(response.StatusCode, mensaje);
    }
}
