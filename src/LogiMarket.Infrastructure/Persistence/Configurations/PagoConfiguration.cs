using LogiMarket.Domain.Entities;
using LogiMarket.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMarket.Infrastructure.Persistence.Configurations;

public class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pagos");
        builder.HasKey(p => p.Id);
        builder.HasOne<Pedido>().WithMany().HasForeignKey(p => p.PedidoId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(p => p.PedidoId).IsRequired();
        builder.Property(p => p.Monto).HasConversion(new DineroConverter()).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.Referencia).IsRequired().HasMaxLength(60);
        builder.Property(p => p.ClaveIdempotencia).IsRequired().HasMaxLength(80);
        builder.HasIndex(p => p.ClaveIdempotencia).IsUnique();   // impide cobros duplicados (P07)
        builder.HasIndex(p => p.PedidoId).IsUnique();            // un solo pago por pedido
    }
}
