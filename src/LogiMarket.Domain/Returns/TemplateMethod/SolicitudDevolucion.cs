using LogiMarket.Domain.Entities;

namespace LogiMarket.Domain.Returns.TemplateMethod;

// Lo que el cliente pide: el pedido, cuándo lo pide y si el producto está sin uso.
public record SolicitudDevolucion(Pedido Pedido, DateTime FechaSolicitudUtc, bool ProductoSinUso);