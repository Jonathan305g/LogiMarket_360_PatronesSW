using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Events;

// Hecho ocurrido: una devolución fue aceptada. Es inmutable (record).
public record DevolucionAceptada(Guid PedidoId, Guid DevolucionId, Dinero Reembolso, DateTime OcurrioUtc)
    : IEventoDominio;