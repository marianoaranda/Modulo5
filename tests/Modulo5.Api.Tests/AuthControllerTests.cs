using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Modulo5.Data;
using Modulo5.Data.Configurations;
using Modulo5.Domain.Entities;
using Modulo5.Domain.Security;

namespace Modulo5.Api.Tests;

/// <summary>
/// Tests del Block 3 del spec FEAT-001a: login JWT + manejo de errores + rate limiting.
/// Los 6 tests requeridos por el bloque están cubiertos acá (ver "Required tests" del spec).
/// </summary>
public class AuthControllerTests : IAsyncLifetime
{
    private const string UsuarioValido = "jperez";
    private const string PasswordValido = "Passw0rd1";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthControllerTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Modulo5DbContext>();
        var hasher = new Pbkdf2PasswordHasher();
        var (hash, salt) = hasher.Hash(PasswordValido);

        context.Usuarios.Add(new Usuario
        {
            NombreUsuario = UsuarioValido,
            NombreCompleto = "Juan Perez",
            Hash = hash,
            Salt = salt,
            PerfilId = PerfilConfiguration.AdministradorPerfilId
        });
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Login_con_credenciales_validas_devuelve_200_y_un_JWT_bien_formado()
    {
        // Act — soporta AC-10
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            usuario = UsuarioValido,
            password = PasswordValido
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal(3, body.Token.Split('.').Length); // header.payload.signature: JWT bien formado
        Assert.True(body.ExpiraEn > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_con_usuario_inexistente_devuelve_400_con_el_mensaje_uniforme()
    {
        // Act — soporta AC-09 (sad path)
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            usuario = "no-existe",
            password = "cualquiera1"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.Equal("Usuario o contraseña incorrectos", body!.Mensaje);
    }

    [Fact]
    public async Task Login_con_password_incorrecta_devuelve_400_con_el_mismo_mensaje()
    {
        // Act — soporta AC-09, no-enumeración (sad path)
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            usuario = UsuarioValido,
            password = "incorrecta1"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.Equal("Usuario o contraseña incorrectos", body!.Mensaje);
    }

    [Fact]
    public async Task Request_a_endpoint_protegido_sin_Authorization_devuelve_401()
    {
        // Act — soporta AC-11 (sad path)
        var response = await _client.GetAsync("/api/auth/ping");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Request_a_endpoint_protegido_con_JWT_valido_pasa_la_autenticacion()
    {
        // Arrange — soporta AC-11
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            usuario = UsuarioValido,
            password = PasswordValido
        });
        var body = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/ping");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Sexto_intento_de_login_en_menos_de_un_minuto_desde_la_misma_IP_devuelve_429()
    {
        // Act — 5 intentos consumen el límite de la ventana fija, el 6º debe rechazarse
        HttpResponseMessage? last = null;
        for (var i = 0; i < 6; i++)
        {
            last = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                usuario = "no-existe",
                password = "cualquiera1"
            });
        }

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        var body = await last.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.Equal("Demasiados intentos, intente nuevamente en unos minutos.", body!.Mensaje);
    }

    private record LoginResponseDto(string Token, DateTime ExpiraEn);

    private record ErrorResponseDto(string Mensaje);
}
