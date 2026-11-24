using LogiMarket.Domain.Entities;
using LogiMarket.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMarket.Infrastructure.Persistence.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Sku).IsRequired().HasMaxLength(20);
        builder.HasIndex(p => p.Sku).IsUnique();   // SKU único, exigido por la guía
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(120);
        builder.Property(p => p.Precio).HasConversion(new DineroConverter()).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.PesoKg).HasColumnType("decimal(10,3)");
        builder.Property(p => p.Stock).IsRequired();
    }
}