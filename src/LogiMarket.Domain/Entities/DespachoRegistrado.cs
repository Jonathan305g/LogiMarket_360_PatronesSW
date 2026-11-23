namespace LogiMarket.Domain.Events;

public record DespachoRegistrado(Guid PedidoId, string Guia, DateTime OcurrioUtc) : IEventoDominio;