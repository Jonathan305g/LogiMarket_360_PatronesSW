using LogiMarket.Domain.Entities;
using LogiMarket.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMarket.Infrastructure.Persistence.Configurations;

public class DevolucionConfiguration : IEntityTypeConfiguration<Devolucion>
{
    public void Configure(EntityTypeBuilder<Devolucion> builder)
    {
        builder.ToTable("Devoluciones");
        builder.HasKey(d => d.Id);
        builder.HasOne<Pedido>().WithMany().HasForeignKey(d => d.PedidoId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(d => d.PedidoId).IsRequired();
        builder.HasIndex(d => d.PedidoId).IsUnique();   // devolución única por pedido (P35)
        builder.Property(d => d.Motivo).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Reembolso).HasConversion(new DineroConverter()).HasColumnType("decimal(18,2)").IsRequired();

        builder.OwnsMany(d => d.Reintegros, r =>
        {
            r.ToTable("DevolucionReintegros");
            r.WithOwner().HasForeignKey("DevolucionId");
            r.Property<Guid>("Id");
            r.HasKey("DevolucionId", "Id");
            r.Property(x => x.Sku).IsRequired().HasMaxLength(20);
            r.Property(x => x.Cantidad).IsRequired();
            r.HasOne<Producto>().WithMany().HasPrincipalKey(p => p.Sku)
                .HasForeignKey(x => x.Sku).OnDelete(DeleteBehavior.Restrict);
        });
        // (Reintegros SÍ tiene "set" privado: EF usa la propiedad, no hace falta forzar campo.)
    }
}
