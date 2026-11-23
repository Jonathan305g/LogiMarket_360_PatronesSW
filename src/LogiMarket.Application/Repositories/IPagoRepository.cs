using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Abstractions.Repositories;

public interface IPagoRepository
{
    Task<Pago?> ObtenerPorClaveAsync(string claveIdempotencia);

    // "Agregar" solo deja el pago pendiente; se guarda al confirmar la unidad de trabajo.
    void Agregar(Pago pago);
}