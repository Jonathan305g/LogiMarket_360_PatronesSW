using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Entities;

// Un cobro aprobado. Hay como máximo uno por pedido y uno por clave de idempotencia.
public class Pago
{
    public Guid Id { get; private set; }
    public Guid PedidoId { get; private set; }
    public Dinero Monto { get; private set; }
    public string Referencia { get; private set; }
    public string ClaveIdempotencia { get; private set; }
    public DateTime FechaUtc { get; private set; }

    public Pago(Guid pedidoId, Dinero monto, string referencia, string claveIdempotencia, DateTime fechaUtc)
    {
        if (pedidoId == Guid.Empty)
            throw new DomainException("El pago requiere un pedido.");
        if (monto.Valor <= 0)
            throw new DomainException("El monto del pago debe ser mayor que cero.");
        if (string.IsNullOrWhiteSpace(referencia))
            throw new DomainException("El pago requiere la referencia de la pasarela.");
        if (string.IsNullOrWhiteSpace(claveIdempotencia))
            throw new DomainException("El pago requiere una clave de idempotencia.");

        Id = Guid.NewGuid();
        PedidoId = pedidoId;
        Monto = monto;
        Referencia = referencia;
        ClaveIdempotencia = claveIdempotencia;
        FechaUtc = fechaUtc;
    }
}