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
/// Tests del Block 2 del spec FEAT-001b: ABM de Artículos protegido solo por JWT (sin política de
/// perfil, a diferencia de Usuarios). Los 9 tests requeridos por el bloque están cubiertos acá (ver
/// "Required tests" del spec).
///
/// El JWT se obtiene haciendo LOGIN REAL contra <c>POST /api/auth/login</c> (Block 3 de FEAT-001a),
/// con un usuario sembrado directamente en la base SQLite del fixture — igual criterio que
/// <c>UsuariosControllerTests</c> de FEAT-001a. No hace falta que el usuario tenga ningún perfil en
/// particular: cualquier JWT válido alcanza (el PRD de Artículos no restringe por perfil).
///
/// Deliberadamente NO se referencian acá los tipos nuevos del Block 2
/// (<c>ArticulosController</c>/<c>ArticuloRequest</c>/<c>ArticuloResponse</c>): los requests se arman
/// con objetos anónimos y las respuestas se leen con records locales, igual que en
/// <see cref="UsuariosControllerTests"/>. Así, antes de implementar el bloque, estos tests fallan por
/// un 404 real (la ruta <c>/api/articulos</c> no existe todavía) en vez de por un error de compilación.
/// </summary>
public class ArticulosControllerTests : IAsyncLifetime
{
    private const string Usuario = "usuario.test";
    private const string Password = "Password1";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ArticulosControllerTests()
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

        var (hash, salt) = hasher.Hash(Password);
        context.Usuarios.Add(new Usuario
        {
            NombreUsuario = Usuario,
            NombreCompleto = "Usuario de Pruebas",
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
    public async Task Alta_de_articulo_con_datos_validos_devuelve_201_y_es_recuperable()
    {
        // Arrange — soporta AC-01
        using var client = await AuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/articulos", new
        {
            codigo = "ART-001",
            descripcion = "Artículo de prueba",
            precioCosto = 100m,
            margen = 20m,
            stockMinimo = 1,
            puntoPedido = 2,
            stockIdeal = 3
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ArticuloResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("ART-001", body!.Codigo);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Modulo5DbContext>();
        var persistido = await context.Articulos.FindAsync("ART-001");
        Assert.NotNull(persistido);
        Assert.Equal("Artículo de prueba", persistido!.Descripcion);
    }

    [Fact]
    public async Task Baja_de_articulo_existente_devuelve_204_y_ya_no_es_recuperable()
    {
        // Arrange — soporta AC-02
        using var client = await AuthenticatedClientAsync();
        var creado = await CrearArticuloDePruebaAsync(client, "ART-BAJA");

        // Act
        var response = await client.DeleteAsync($"/api/articulos/{creado.Codigo}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Modulo5DbContext>();
        var persistido = await context.Articulos.FindAsync("ART-BAJA");
        Assert.Null(persistido);
    }

    [Fact]
    public async Task Modificacion_de_articulo_existente_devuelve_200_con_cambios_persistidos()
    {
        // Arrange — soporta AC-03
        using var client = await AuthenticatedClientAsync();
        var creado = await CrearArticuloDePruebaAsync(client, "ART-MOD");

        // Act
        var response = await client.PutAsJsonAsync($"/api/articulos/{creado.Codigo}", new
        {
            descripcion = "Descripción Modificada",
            precioCosto = 200m,
            margen = 10m,
            stockMinimo = 1,
            puntoPedido = 2,
            stockIdeal = 3
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ArticuloResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("Descripción Modificada", body!.Descripcion);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Modulo5DbContext>();
        var persistido = await context.Articulos.FindAsync("ART-MOD");
        Assert.Equal("Descripción Modificada", persistido!.Descripcion);
    }

    [Fact]
    public async Task Alta_calcula_PrecioVenta_ignorando_el_enviado_por_el_cliente()
    {
        // Arrange — soporta AC-04
        using var client = await AuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/articulos", new
        {
            codigo = "ART-PRECIO",
            descripcion = "Cálculo de precio",
            precioCosto = 100m,
            margen = 20m,
            precioVenta = 999999m, // debe ser ignorado
            stockMinimo = 1,
            puntoPedido = 2,
            stockIdeal = 3
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ArticuloResponseDto>();
        Assert.NotNull(body);
        Assert.Equal(120m, body!.PrecioVenta);
    }

    [Fact]
    public async Task Alta_con_Codigo_duplicado_devuelve_400_con_mensaje_exacto()
    {
        // Arrange — soporta AC-05 (sad path)
        using var client = await AuthenticatedClientAsync();
        await CrearArticuloDePruebaAsync(client, "ART-DUP");

        // Act
        var response = await client.PostAsJsonAsync("/api/articulos", new
        {
            codigo = "ART-DUP",
            descripcion = "Duplicado",
            precioCosto = 100m,
            margen = 20m,
            stockMinimo = 1,
            puntoPedido = 2,
            stockIdeal = 3
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.Equal("Ya existe un artículo con el Código ingresado.", body!.Mensaje);
    }

    [Fact]
    public async Task Alta_o_modificacion_con_valor_negativo_devuelve_400_con_mensaje_exacto()
    {
        // Arrange — soporta AC-06 (sad path)
        using var client = await AuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/articulos", new
        {
            codigo = "ART-NEG",
            descripcion = "Valor negativo",
            precioCosto = -1m,
            margen = 20m,
            stockMinimo = 1,
            puntoPedido = 2,
            stockIdeal = 3
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.Equal(
            "Los valores de Precio de Costo, Margen, Stock Mínimo, Punto de Pedido y Stock Ideal " +
            "no pueden ser negativos.",
            body!.Mensaje);
    }

    [Fact]
    public async Task Alta_o_modificacion_que_rompe_orden_de_stock_devuelve_400_con_mensaje_exacto()
    {
        // Arrange — soporta AC-07 (sad path)
        using var client = await AuthenticatedClientAsync();

        // Act
        var response = await client.PostAsJsonAsync("/api/articulos", new
        {
            codigo = "ART-ORDEN",
            descripcion = "Orden de stock roto",
            precioCosto = 100m,
            margen = 20m,
            stockMinimo = 5,
            puntoPedido = 2,
            stockIdeal = 3
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.Equal(
            "El Stock Mínimo debe ser menor o igual al Punto de Pedido, y este menor o igual al " +
            "Stock Ideal.",
            body!.Mensaje);
    }

    [Fact]
    public async Task Put_y_Delete_sobre_Codigo_inexistente_devuelven_404()
    {
        // Arrange — soporta la integridad de AC-02/AC-03 (sad path). Se verifica el MENSAJE de
        // NotFoundException (no solo el status code): un 404 "framework" por ruta inexistente (p.
        // ej. antes de implementar el bloque) no trae este cuerpo JSON.
        using var client = await AuthenticatedClientAsync();

        // Act
        var putResponse = await client.PutAsJsonAsync("/api/articulos/NO-EXISTE", new
        {
            descripcion = "No existe",
            precioCosto = 100m,
            margen = 20m,
            stockMinimo = 1,
            puntoPedido = 2,
            stockIdeal = 3
        });
        var deleteResponse = await client.DeleteAsync("/api/articulos/NO-EXISTE");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        var putBody = await putResponse.Content.ReadFromJsonAsync<ErrorResponseDto>();
        var deleteBody = await deleteResponse.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.NotNull(putBody);
        Assert.NotNull(deleteBody);
    }

    [Fact]
    public async Task Request_sin_header_Authorization_devuelve_401()
    {
        // Arrange — soporta el sad path de "sin JWT válido"

        // Act
        var response = await _client.PostAsJsonAsync("/api/articulos", new
        {
            codigo = "ART-SIN-AUTH",
            descripcion = "Sin autenticación",
            precioCosto = 100m,
            margen = 20m,
            stockMinimo = 1,
            puntoPedido = 2,
            stockIdeal = 3
        });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { usuario = Usuario, password = Password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }

    private async Task<HttpClient> AuthenticatedClientAsync()
    {
        var token = await LoginAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<ArticuloResponseDto> CrearArticuloDePruebaAsync(HttpClient client, string codigo)
    {
        var response = await client.PostAsJsonAsync("/api/articulos", new
        {
            codigo,
            descripcion = "Artículo de prueba",
            precioCosto = 100m,
            margen = 20m,
            stockMinimo = 1,
            puntoPedido = 2,
            stockIdeal = 3
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ArticuloResponseDto>())!;
    }

    private record LoginResponseDto(string Token, DateTime ExpiraEn);

    private record ErrorResponseDto(string Mensaje);

    private record ArticuloResponseDto(
        string Codigo,
        string Descripcion,
        decimal PrecioCosto,
        decimal Margen,
        decimal PrecioVenta,
        int StockMinimo,
        int PuntoPedido,
        int StockIdeal);
}
