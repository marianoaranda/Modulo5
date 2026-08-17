using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modulo5.Data;

/// <summary>
/// Factory de diseño usada exclusivamente por las herramientas de EF Core (`dotnet ef migrations
/// add` / `dotnet ef database update`) para generar/aplicar migraciones sin depender de un proyecto
/// de arranque (`Modulo5.Api` recién se crea en el Block 3). La connection string real de runtime
/// NUNCA sale de acá: en desarrollo se lee de `user-secrets` y en producción de la variable de
/// entorno `ConnectionStrings__Default` (ver `Modulo5.Api/Program.cs`, Block 3). Este placeholder
/// solo necesita tener sintaxis válida — EF no abre conexión real para generar la migración.
/// </summary>
public class Modulo5DbContextFactory : IDesignTimeDbContextFactory<Modulo5DbContext>
{
    public Modulo5DbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<Modulo5DbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=Modulo5;Trusted_Connection=True;");

        return new Modulo5DbContext(optionsBuilder.Options);
    }
}
