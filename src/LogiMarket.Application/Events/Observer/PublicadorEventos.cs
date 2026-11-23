using LogiMarket.Domain.Events;

namespace LogiMarket.Application.Events.Observer;

// El SUJETO (Publisher): mantiene a los suscriptores y los notifica a todos, en orden.
public sealed class PublicadorEventos
{
    private readonly List<IObservadorEventos> _observadores = new();

    public void Suscribir(IObservadorEventos observador) =>
        _observadores.Add(observador ?? throw new ArgumentNullException(nameof(observador)));

    public async Task PublicarAsync(IEventoDominio evento, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(evento);

        foreach (var observador in _observadores)
            await observador.ManejarAsync(evento, ct);
    }
}