using LogiMarket.Application.Validation;

namespace LogiMarket.Application.UseCases.Pedidos;

public interface ICancelacionPedido
{
    Task<ResultadoOperacion> CancelarAsync(Guid pedidoId, CancellationToken ct = default);
}