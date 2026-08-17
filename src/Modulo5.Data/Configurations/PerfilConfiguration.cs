using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modulo5.Domain.Entities;

namespace Modulo5.Data.Configurations;

public class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
    /// <summary>
    /// Id fijo del perfil "administrador" sembrado por la migración inicial (FR-10/AC-12). Se
    /// declara como constante para que el seed sea determinístico entre entornos.
    /// </summary>
    public const int AdministradorPerfilId = 1;

    public void Configure(EntityTypeBuilder<Perfil> builder)
    {
        builder.ToTable("Perfiles");

        builder.HasKey(p => p.PerfilId);

        builder.Property(p => p.PerfilId)
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Descripcion)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasData(new Perfil
        {
            PerfilId = AdministradorPerfilId,
            Descripcion = "administrador"
        });
    }
}
