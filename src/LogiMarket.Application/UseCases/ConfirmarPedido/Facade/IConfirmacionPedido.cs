namespace LogiMarket.Application.UseCases.ConfirmarPedido.Facade;

// Lo que los comandos necesitan del Facade. Dependen de esta interfaz, no de la clase concreta.
public interface IConfirmacionPedido
{
    Task<ResultadoConfirmacion> ConfirmarAsync(ConfirmarPedidoSolicitud solicitud, CancellationToken ct = default);
}