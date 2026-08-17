using Microsoft.EntityFrameworkCore;
using Modulo5.Data.Configurations;
using Modulo5.Domain.Entities;

namespace Modulo5.Data;

public class Modulo5DbContext : DbContext
{
    public Modulo5DbContext(DbContextOptions<Modulo5DbContext> options) : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Perfil> Perfiles => Set<Perfil>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PerfilConfiguration());
        modelBuilder.ApplyConfiguration(new UsuarioConfiguration());
    }
}
