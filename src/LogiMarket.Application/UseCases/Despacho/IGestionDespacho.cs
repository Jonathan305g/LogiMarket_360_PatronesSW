using LogiMarket.Application.Validation;

namespace LogiMarket.Application.UseCases.Despacho;

public interface IGestionDespacho
{
    Task<ResultadoDespacho> DespacharAsync(Guid pedidoId, CancellationToken ct = default);
    Task<ResultadoOperacion> EntregarAsync(Guid pedidoId, CancellationToken ct = default);
}