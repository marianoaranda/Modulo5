using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Modulo5.Data;
using Modulo5.Data.Configurations;
using Modulo5.Domain.Entities;
using Modulo5.Domain.Repositories;
using Modulo5.Domain.Security;

namespace Modulo5.Api.Tests;

/// <summary>
/// Tests del Block 4 del spec FEAT-001a: ABM de Usuarios protegido por perfil administrador. Los 9
/// tests requeridos por el bloque están cubiertos acá (ver "Required tests" del spec).
///
/// Los JWT de administrador/no-administrador se obtienen haciendo LOGIN REAL contra
/// <c>POST /api/auth/login</c> (Block 3), con usuarios sembrados directamente en la base SQLite del
/// fixture — uno con <c>PerfilId</c> = el del perfil "administrador" sembrado por el Block 1, otro con
/// un <c>PerfilId</c> distinto ("operador"). Se eligió este camino en vez de fabricar JWTs a mano
/// porque ejercita el mismo flujo autenticación→autorización que en producción (incluida la firma del
/// token con la clave real de test), sin duplicar la lógica de <c>JwtTokenGenerator</c> en los tests.
///
/// Deliberadamente NO se referencian acá los tipos nuevos del Block 4
/// (<c>UsuariosController</c>/<c>UsuarioRequest</c>/<c>UsuarioResponse</c>): los requests se arman con
/// objetos anónimos y las respuestas se leen con records locales, igual que en
/// <see cref="AuthControllerTests"/>. Así, antes de implementar el bloque, estos tests fallan por un
/// 404 real (la ruta <c>/api/usuarios</c> no existe todavía) en vez de por un error de compilación.
/// </summary>
public class UsuariosControllerTests : IAsyncLifetime
{
    private const string AdminUsuario = "admin.test";
    private const string AdminPassword = "AdminPass1";
    private const string NoAdminUsuario = "vendedor.test";
    private const string NoAdminPassword = "Vendedor1";

    private const int OperadorPerfilId = 2;

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsuariosControllerTests()
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

        context.Perfiles.Add(new Perfil { PerfilId = OperadorPerfilId, Descripcion = "operador" });

        var (adminHash, adminSalt) = hasher.Hash(AdminPassword);
        context.Usuarios.Add(new Usuario
        {
            NombreUsuario = AdminUsuario,
            NombreCompleto = "Administrador de Pruebas",
            Hash = adminHash,
            Salt = adminSalt,
            PerfilId = PerfilConfiguration.AdministradorPerfilId
        });

        var (noAdminHash, noAdminSalt) = hasher.Hash(NoAdminPassword);
        context.Usuarios.Add(new Usuario
        {
            NombreUsuario = NoAdminUsuario,
            NombreCompleto = "Vendedor de Pruebas",
            Hash = noAdminHash,
            Salt = noAdminSalt,
            PerfilId = OperadorPerfilId
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
    public async Task Administrador_da_de_alta_un_usuario_valido_devuelve_201_y_es_recuperable()
    {
        // Arrange — soporta AC-01
        using var admin = await AuthenticatedClientAsync(AdminUsuario, AdminPassword);

        // Act
        var response = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            usuario = "nuevo.usuario",
            nombreCompleto = "Nuevo Usuario",
            password = "Password1",
            perfilId = OperadorPerfilId
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UsuarioResponseDto>();
        Assert.NotNull(body);
        Assert.True(body!.UsuarioId > 0);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Modulo5DbContext>();
        var persistido = await context.Usuarios.FindAsync(body.UsuarioId);
        Assert.NotNull(persistido);
        Assert.Equal("nuevo.usuario", persistido!.NombreUsuario);
    }

    [Fact]
    public async Task Administrador_elimina_un_usuario_existente_devuelve_204_y_ya_no_es_recuperable()
    {
        // Arrange — soporta AC-02
        using var admin = await AuthenticatedClientAsync(AdminUsuario, AdminPassword);
        var creado = await CrearUsuarioDePruebaAsync(admin, "a.eliminar");

        // Act
        var response = await admin.DeleteAsync($"/api/usuarios/{creado.UsuarioId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Modulo5DbContext>();
        var persistido = await context.Usuarios.FindAsync(creado.UsuarioId);
        Assert.Null(persistido);
    }

    [Fact]
    public async Task Administrador_modifica_un_usuario_existente_devuelve_200_con_cambios_persistidos()
    {
        // Arrange — soporta AC-03
        using var admin = await AuthenticatedClientAsync(AdminUsuario, AdminPassword);
        var creado = await CrearUsuarioDePruebaAsync(admin, "a.modificar");

        // Act
        var response = await admin.PutAsJsonAsync($"/api/usuarios/{creado.UsuarioId}", new
        {
            nombreCompleto = "Nombre Modificado",
            perfilId = OperadorPerfilId
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UsuarioResponseDto>();
        Assert.NotNull(body);
        Assert.Equal("Nombre Modificado", body!.NombreCompleto);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Modulo5DbContext>();
        var persistido = await context.Usuarios.FindAsync(creado.UsuarioId);
        Assert.Equal("Nombre Modificado", persistido!.NombreCompleto);
    }

    [Fact]
    public async Task Usuario_con_JWT_valido_pero_no_administrador_intenta_alta_devuelve_403()
    {
        // Arrange — soporta AC-08 (sad path)
        using var noAdmin = await AuthenticatedClientAsync(NoAdminUsuario, NoAdminPassword);

        // Act
        var response = await noAdmin.PostAsJsonAsync("/api/usuarios", new
        {
            usuario = "otro.usuario",
            nombreCompleto = "Otro Usuario",
            password = "Password1",
            perfilId = OperadorPerfilId
        });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Alta_con_nombreUsuario_ya_existente_devuelve_400()
    {
        // Arrange — soporta la unicidad que AC-01 asume (sad path)
        using var admin = await AuthenticatedClientAsync(AdminUsuario, AdminPassword);

        // Act
        var response = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            usuario = AdminUsuario, // ya existe
            nombreCompleto = "Duplicado",
            password = "Password1",
            perfilId = OperadorPerfilId
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Alta_con_password_que_no_cumple_la_politica_devuelve_400_con_mensaje_exacto()
    {
        // Arrange — soporta AC-06 en el contexto del ABM (sad path)
        using var admin = await AuthenticatedClientAsync(AdminUsuario, AdminPassword);

        // Act
        var response = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            usuario = "usuario.password.invalida",
            nombreCompleto = "Usuario Con Password Invalida",
            password = "corta1",
            perfilId = OperadorPerfilId
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.Equal(
            "La contraseña debe tener al menos 8 caracteres alfanuméricos.",
            body!.Mensaje);
    }

    [Fact]
    public async Task Put_y_Delete_sobre_UsuarioId_inexistente_devuelven_404()
    {
        // Arrange — soporta la integridad de AC-02/AC-03 (sad path). Se verifica el MENSAJE de
        // NotFoundException (no solo el status code): un 404 "framework" por ruta inexistente (p.
        // ej. antes de implementar el bloque) no trae este cuerpo JSON, así que esta aserción
        // distingue el 404 de negocio del 404 de enrutamiento.
        using var admin = await AuthenticatedClientAsync(AdminUsuario, AdminPassword);

        // Act
        var putResponse = await admin.PutAsJsonAsync("/api/usuarios/999999", new
        {
            nombreCompleto = "No Existe",
            perfilId = OperadorPerfilId
        });
        var deleteResponse = await admin.DeleteAsync("/api/usuarios/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        var putBody = await putResponse.Content.ReadFromJsonAsync<ErrorResponseDto>();
        var deleteBody = await deleteResponse.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.Equal("No existe el usuario con UsuarioId 999999.", putBody!.Mensaje);
        Assert.Equal("No existe el usuario con UsuarioId 999999.", deleteBody!.Mensaje);
    }

    [Fact]
    public async Task Respuesta_de_Post_y_Put_no_incluye_Hash_ni_Salt()
    {
        // Arrange — soporta el riesgo #5 del threat model
        using var admin = await AuthenticatedClientAsync(AdminUsuario, AdminPassword);

        // Act
        // NOTA: el username/nombre de prueba NO debe contener las subcadenas "hash"/"salt" en su
        // VALOR (a diferencia de los NOMBRES de propiedad, que es lo que esta aserción verifica) —
        // de lo contrario el chequeo de "la respuesta no contiene la cadena hash/salt" da un falso
        // positivo contra el propio dato de prueba, no contra una fuga real del DTO.
        var postResponse = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            usuario = "sin.datos.expuestos",
            nombreCompleto = "Sin Datos Expuestos",
            password = "Password1",
            perfilId = OperadorPerfilId
        });
        var postRaw = await postResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);
        var creado = JsonSerializer.Deserialize<UsuarioResponseDto>(
            postRaw,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        var putResponse = await admin.PutAsJsonAsync($"/api/usuarios/{creado.UsuarioId}", new
        {
            nombreCompleto = "Modificado Sin Datos Expuestos",
            password = "Password2",
            perfilId = OperadorPerfilId
        });
        var putRaw = await putResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        // Assert
        AssertNoHashNiSalt(postRaw);
        AssertNoHashNiSalt(putRaw);
    }

    [Fact]
    public async Task El_perfil_administrador_sembrado_en_Block1_es_recuperable_y_habilita_la_autorizacion()
    {
        // Assert (1/2) — soporta AC-12: el perfil "administrador" sembrado por el Block 1 es
        // recuperable por el mismo repositorio que consulta el chequeo de autorización del Block 4.
        using var scope = _factory.Services.CreateScope();
        var perfilRepository = scope.ServiceProvider.GetRequiredService<IPerfilRepository>();
        var administrador = await perfilRepository.GetByDescripcionAsync("administrador");
        Assert.NotNull(administrador);
        Assert.Equal(PerfilConfiguration.AdministradorPerfilId, administrador!.PerfilId);

        // Assert (2/2) — el chequeo de autorización (AdminOnlyHandler) usa ese PerfilId resuelto por
        // consulta -no un valor hardcodeado- para conceder acceso a un administrador real.
        using var admin = await AuthenticatedClientAsync(AdminUsuario, AdminPassword);
        var response = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            usuario = "verificacion.autorizacion",
            nombreCompleto = "Verificacion Autorizacion",
            password = "Password1",
            perfilId = OperadorPerfilId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static void AssertNoHashNiSalt(string json)
    {
        Assert.DoesNotContain("hash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("salt", json, StringComparison.OrdinalIgnoreCase);

        using var document = JsonDocument.Parse(json);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            Assert.False(
                property.Name.Equals("hash", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("salt", StringComparison.OrdinalIgnoreCase),
                $"La respuesta no debe incluir la propiedad '{property.Name}'.");
        }
    }

    private async Task<string> LoginAsync(string usuario, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { usuario, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string usuario, string password)
    {
        var token = await LoginAsync(usuario, password);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<UsuarioResponseDto> CrearUsuarioDePruebaAsync(
        HttpClient admin, string usuario)
    {
        var response = await admin.PostAsJsonAsync("/api/usuarios", new
        {
            usuario,
            nombreCompleto = "Usuario De Prueba",
            password = "Password1",
            perfilId = OperadorPerfilId
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UsuarioResponseDto>())!;
    }

    private record LoginResponseDto(string Token, DateTime ExpiraEn);

    private record ErrorResponseDto(string Mensaje);

    private record UsuarioResponseDto(int UsuarioId, string Usuario, string NombreCompleto, int PerfilId);
}
