using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.UseCases.Devoluciones;

// Lo que pide el usuario: qué pedido, por qué motivo y en qué condición.
public record SolicitudDevolucionDto(Guid PedidoId, MotivoDevolucion Motivo, DateTime FechaSolicitudUtc, bool ProductoSinUso); 