using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMarket.Infrastructure.Persistence.Configurations;

public class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("MovimientosInventario");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Sku).IsRequired().HasMaxLength(20);
        builder.HasIndex(m => m.Sku);
        builder.Property(m => m.Cantidad).IsRequired();
        builder.Property(m => m.Motivo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Referencia).IsRequired();
    }
}