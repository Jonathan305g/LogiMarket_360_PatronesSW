using LogiMarket.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMarket.Infrastructure.Persistence.Configurations;

public class KitRegistroConfiguration : IEntityTypeConfiguration<KitRegistro>
{
    public void Configure(EntityTypeBuilder<KitRegistro> builder)
    {
        builder.ToTable("Kits");
        builder.HasKey(k => k.Sku);
        builder.Property(k => k.Sku).HasMaxLength(20);
        builder.Property(k => k.Nombre).IsRequired().HasMaxLength(120);

        builder.HasMany(k => k.Componentes)
            .WithOne()
            .HasForeignKey(c => c.KitSku)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class KitComponenteRegistroConfiguration : IEntityTypeConfiguration<KitComponenteRegistro>
{
    public void Configure(EntityTypeBuilder<KitComponenteRegistro> builder)
    {
        builder.ToTable("KitComponentes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ComponenteSku).IsRequired().HasMaxLength(20);
        builder.Property(c => c.Cantidad).IsRequired();
        builder.HasOne<LogiMarket.Domain.Entities.Producto>()
            .WithMany().HasPrincipalKey(p => p.Sku).HasForeignKey(c => c.ProductoSku).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<KitRegistro>()
            .WithMany().HasForeignKey(c => c.KitReferenciaSku).OnDelete(DeleteBehavior.Restrict);
    }
}
