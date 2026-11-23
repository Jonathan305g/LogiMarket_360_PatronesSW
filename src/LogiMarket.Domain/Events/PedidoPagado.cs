using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Events;

// Hecho ocurrido: un pedido quedó pagado. Saldrá del Outbox hacia los observadores.
public record PedidoPagado(Guid PedidoId, Guid ClienteId, Dinero Total, string ReferenciaPago, DateTime OcurrioUtc)
    : IEventoDominio;