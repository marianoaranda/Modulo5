using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Modulo5.Data;
using Modulo5.Domain.Entities;

namespace Modulo5.Domain.Tests;

/// <summary>
/// Tests del Block 1 del spec FEAT-001a: persistencia base de Usuario/Perfil vía EF Core.
///
/// ASSUMPTION (documentada en el reporte del bloque): el spec no especifica qué proveedor de base
/// de datos usar en estos tests. Se usa SQLite en memoria (una conexión abierta y compartida por
/// test, vía "cache=shared") en vez del proveedor InMemory de EF Core, porque InMemory NO aplica
/// índices únicos (verificado empíricamente) y el tercer test exige exactamente esa verificación.
/// SQLite ejecuta SQL real y sí impone la constraint UNIQUE, siendo más fiel al comportamiento que
/// tendrá SQL Server en producción. `Modulo5.Data` en sí sigue apuntando solo a SqlServer (ver
/// Modulo5.Data.csproj) — SQLite es una dependencia exclusiva de este proyecto de tests.
/// </summary>
public class Modulo5DbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<Modulo5DbContext> _options;

    public Modulo5DbContextTests()
    {
        _connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared");
        _connection.Open();

        _options = new DbContextOptionsBuilder<Modulo5DbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new Modulo5DbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void Usuario_con_datos_validos_se_persiste_y_se_recupera_por_UsuarioId()
    {
        // Arrange — soporta AC-01
        int perfilId;
        int usuarioId;
        using (var context = new Modulo5DbContext(_options))
        {
            perfilId = context.Perfiles.First().PerfilId;
            var usuario = new Usuario
            {
                NombreUsuario = "jperez",
                NombreCompleto = "Juan Perez",
                Hash = new byte[] { 1, 2, 3 },
                Salt = new byte[] { 4, 5, 6 },
                PerfilId = perfilId
            };

            // Act
            context.Usuarios.Add(usuario);
            context.SaveChanges();
            usuarioId = usuario.UsuarioId;
        }

        // Assert — se recupera con un DbContext nuevo, misma conexión (evita falsos positivos por
        // el change tracker de la primera instancia)
        using var readContext = new Modulo5DbContext(_options);
        var recuperado = readContext.Usuarios.Find(usuarioId);

        Assert.NotNull(recuperado);
        Assert.Equal("jperez", recuperado!.NombreUsuario);
        Assert.Equal("Juan Perez", recuperado.NombreCompleto);
        Assert.Equal(perfilId, recuperado.PerfilId);
    }

    [Fact]
    public void Migracion_aplicada_crea_el_perfil_administrador_y_es_recuperable()
    {
        // Arrange / Act — soporta AC-12
        using var context = new Modulo5DbContext(_options);

        // Assert
        var administrador = context.Perfiles.SingleOrDefault(p => p.Descripcion == "administrador");
        Assert.NotNull(administrador);
    }

    [Fact]
    public void Persistir_dos_Usuario_con_el_mismo_nombre_viola_el_indice_unico()
    {
        // Arrange
        using var context = new Modulo5DbContext(_options);
        var perfilId = context.Perfiles.First().PerfilId;
        context.Usuarios.Add(new Usuario
        {
            NombreUsuario = "duplicado",
            NombreCompleto = "Primero",
            Hash = new byte[] { 1 },
            Salt = new byte[] { 2 },
            PerfilId = perfilId
        });
        context.SaveChanges();

        context.Usuarios.Add(new Usuario
        {
            NombreUsuario = "duplicado",
            NombreCompleto = "Segundo",
            Hash = new byte[] { 3 },
            Salt = new byte[] { 4 },
            PerfilId = perfilId
        });

        // Act & Assert
        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
