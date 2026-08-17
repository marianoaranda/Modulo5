using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Modulo5.Data;

namespace Modulo5.Api.Tests;

/// <summary>
/// Factory de integración del Block 3 del spec FEAT-001a: levanta `Modulo5.Api` en memoria
/// (TestServer) reemplazando el <see cref="Modulo5DbContext"/> registrado por <c>Program.cs</c>
/// (SQL Server) por SQLite en memoria — mismo criterio que <c>Modulo5DbContextTests</c> del Block 1
/// (SQLite sí aplica constraints reales, a diferencia del proveedor InMemory de EF Core).
///
/// La clave de firma JWT y la connection string se proveen vía VARIABLES DE ENTORNO, seteadas en el
/// constructor ANTES de que `WebApplicationFactory` construya el host: `Program.cs` las lee de
/// `IConfiguration` (que incluye `AddEnvironmentVariables()` por defecto) y falla rápido si faltan,
/// igual que en producción — así el test ejercita el mismo camino de configuración sin depender del
/// orden de interceptación interno de `WebApplicationFactory` sobre `IConfiguration`. La connection
/// string real nunca se usa: el DbContext se reemplaza por SQLite antes de atender requests, así que
/// no hace falta SQL Server disponible para correr estos tests.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestSigningKey = "clave-de-pruebas-para-firmar-jwt-min-32-bytes-1234567890";

    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestSigningKey);
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Default",
            "Server=(localdb)\\mssqllocaldb;Database=Modulo5Test;Trusted_Connection=True;");

        _connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<Modulo5DbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<Modulo5DbContext>(options => options.UseSqlite(_connection));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Modulo5DbContext>();
        await context.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
