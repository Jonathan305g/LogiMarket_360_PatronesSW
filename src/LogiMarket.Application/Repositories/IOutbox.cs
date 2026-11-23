using LogiMarket.Domain.Events;

namespace LogiMarket.Application.Abstractions.Repositories;

// Eventos pendientes de publicar. Se guardan en la MISMA transacción que el negocio.
public interface IOutbox
{
    void Registrar(IEventoDominio evento);
}