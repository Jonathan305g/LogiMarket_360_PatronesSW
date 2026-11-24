using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Infrastructure.Persistence.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiMarket.Infrastructure.Persistence.Configurations;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ClienteId).IsRequired();
        builder.Property(p => p.Entrega).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.Descuento).HasConversion(new DineroConverter()).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Tarifa).HasConversion(new DineroConverter()).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Devuelto).IsRequired();

        // Puente del patrón State: se guarda el ENUM, nunca el objeto de estado.
        builder.Property<EstadoPedido>("EstadoPersistido")
            .HasColumnName("Estado").HasConversion<string>().HasMaxLength(20).IsRequired();

        // Historial de estados: tabla propia "dueña" del pedido.
        builder.OwnsMany(p => p.Historial, h =>
        {
            h.ToTable("PedidoHistorialEstado");
            h.WithOwner().HasForeignKey("PedidoId");
            h.Property<Guid>("Id");
            h.HasKey("PedidoId", "Id");
            h.Property(c => c.Anterior).HasConversion<string>().HasMaxLength(20);
            h.Property(c => c.Nuevo).HasConversion<string>().HasMaxLength(20);
        });
        builder.Navigation(p => p.Historial).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Líneas del pedido con su instantánea de kit (PedidoDetalleComponente).
        builder.OwnsMany(p => p.Detalles, d =>
        {
            d.ToTable("PedidoDetalles");
            d.WithOwner().HasForeignKey("PedidoId");
            d.HasKey(x => x.Id);

            d.Property(x => x.Sku).IsRequired().HasMaxLength(20);
            d.Property(x => x.Nombre).IsRequired().HasMaxLength(120);
            d.Property(x => x.PrecioUnitario).HasConversion(new DineroConverter()).HasColumnType("decimal(18,2)").IsRequired();
            d.Property(x => x.Cantidad).IsRequired();

            d.OwnsMany(x => x.Componentes, c =>
            {
                c.ToTable("PedidoDetalleComponentes");
                c.WithOwner().HasForeignKey("PedidoDetalleId");
                c.Property<Guid>("Id");
                c.HasKey("PedidoDetalleId", "Id");
                c.Property(x => x.Sku).IsRequired().HasMaxLength(20);
                c.Property(x => x.Cantidad).IsRequired();
            });
            d.Navigation(x => x.Componentes).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        builder.Navigation(p => p.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
