namespace LogiMarket.Application.UseCases.ConfirmarPedido.Facade;

// Lo que pide el usuario: qué pedido confirmar y con qué clave de idempotencia.
public record ConfirmarPedidoSolicitud(Guid PedidoId, string ClaveIdempotencia);