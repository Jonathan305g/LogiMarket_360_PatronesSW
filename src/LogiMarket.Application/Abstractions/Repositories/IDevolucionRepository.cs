using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Abstractions.Repositories;

public interface IDevolucionRepository
{
    void Agregar(Devolucion devolucion);
    Task<IReadOnlyList<Devolucion>> ListarAsync(CancellationToken ct = default);
}
