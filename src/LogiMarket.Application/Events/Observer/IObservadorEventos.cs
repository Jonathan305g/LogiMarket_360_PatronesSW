using LogiMarket.Domain.Events;

namespace LogiMarket.Application.Events.Observer;

public interface IObservadorEventos
{
    Task ManejarAsync(IEventoDominio evento, CancellationToken ct = default);
}