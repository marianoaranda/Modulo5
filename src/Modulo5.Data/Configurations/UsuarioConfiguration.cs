using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modulo5.Domain.Entities;

namespace Modulo5.Data.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.UsuarioId);

        builder.Property(u => u.UsuarioId)
            .ValueGeneratedOnAdd();

        builder.Property(u => u.NombreUsuario)
            .HasColumnName("Usuario")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.NombreCompleto)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(u => u.Hash)
            .HasColumnType("varbinary(64)")
            .IsRequired();

        builder.Property(u => u.Salt)
            .HasColumnType("varbinary(16)")
            .IsRequired();

        builder.Property(u => u.PerfilId)
            .IsRequired();

        builder.HasIndex(u => u.NombreUsuario)
            .IsUnique()
            .HasDatabaseName("IX_Usuario_Usuario");

        builder.HasOne(u => u.Perfil)
            .WithMany()
            .HasForeignKey(u => u.PerfilId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
