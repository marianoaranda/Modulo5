using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Modulo5.Domain.Entities;

namespace Modulo5.Data.Configurations;

public class ArticuloConfiguration : IEntityTypeConfiguration<Articulo>
{
    public void Configure(EntityTypeBuilder<Articulo> builder)
    {
        builder.ToTable("Articulos");

        builder.HasKey(a => a.Codigo);

        builder.Property(a => a.Codigo)
            .HasMaxLength(30)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(a => a.Descripcion)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.PrecioCosto)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(a => a.Margen)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(a => a.PrecioVenta)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(a => a.StockMinimo)
            .IsRequired();

        builder.Property(a => a.PuntoPedido)
            .IsRequired();

        builder.Property(a => a.StockIdeal)
            .IsRequired();
    }
}
