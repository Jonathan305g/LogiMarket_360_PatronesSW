using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Entities;

// Cada cambio de stock deja un registro: negativo = sale, positivo = entra.
public class MovimientoInventario
{
    public Guid Id { get; private set; }
    public string Sku { get; private set; }
    public int Cantidad { get; private set; }
    public MotivoMovimiento Motivo { get; private set; }
    public Guid Referencia { get; private set; } // el pedido que lo originó
    public DateTime FechaUtc { get; private set; }

    private MovimientoInventario() { Sku = string.Empty; } // EF Core

    public MovimientoInventario(string sku, int cantidad, MotivoMovimiento motivo, Guid referencia, DateTime fechaUtc)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("El movimiento requiere un SKU.");
        if (cantidad == 0)
            throw new DomainException("Un movimiento de inventario no puede ser de cantidad cero.");
        if (referencia == Guid.Empty)
            throw new DomainException("El movimiento requiere una referencia.");

        Id = Guid.NewGuid();
        Sku = sku;
        Cantidad = cantidad;
        Motivo = motivo;
        Referencia = referencia;
        FechaUtc = fechaUtc;
    }
}
