using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Orders.State;

// Una línea del historial: de qué estado a cuál y cuándo.
public record CambioEstado(EstadoPedido Anterior, EstadoPedido Nuevo, DateTime FechaUtc);