using LogiMarket.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMarket.Infrastructure.Persistence.Configurations;

public class DespachoConfiguration : IEntityTypeConfiguration<Despacho>
{
    public void Configure(EntityTypeBuilder<Despacho> builder)
    {
        builder.ToTable("Despachos");
        builder.HasKey(d => d.Id);
        builder.HasOne<Pedido>().WithMany().HasForeignKey(d => d.PedidoId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(d => d.PedidoId).IsRequired();
        builder.HasIndex(d => d.PedidoId).IsUnique();
        builder.Property(d => d.Guia).IsRequired().HasMaxLength(200);
    }
}
