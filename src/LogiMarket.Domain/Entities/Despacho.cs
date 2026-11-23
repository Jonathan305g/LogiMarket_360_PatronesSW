using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Entities;

// Registro de un despacho: guía simulada y cuándo se generó.
public class Despacho
{
    public Guid Id { get; private set; }
    public Guid PedidoId { get; private set; }
    public string Guia { get; private set; }
    public DateTime FechaUtc { get; private set; }

    // "public": a diferencia de Pago, aquí es Application (no Domain) quien lo crea,
    // igual que ya hace con Pago y MovimientoInventario.
    public Despacho(Guid pedidoId, string guia, DateTime fechaUtc)
    {
        if (pedidoId == Guid.Empty)
            throw new DomainException("El despacho requiere un pedido.");
        if (string.IsNullOrWhiteSpace(guia))
            throw new DomainException("El despacho requiere una guía.");

        Id = Guid.NewGuid();
        PedidoId = pedidoId;
        Guia = guia;
        FechaUtc = fechaUtc;
    }
}