using LogiMarket.Application.Metrics.Singleton;
using LogiMarket.Domain.Events;

namespace LogiMarket.Application.Events.Observer;

public sealed class ObservadorMetricas : IObservadorEventos
{
    private readonly MetricasSesion _metricas;
    public ObservadorMetricas(MetricasSesion metricas) => _metricas = metricas;

    public Task ManejarAsync(IEventoDominio evento, CancellationToken ct = default)
    {
        switch (evento)
        {
            case PedidoPagado: _metricas.RegistrarPedidoPagado(); break;
            case DespachoRegistrado: _metricas.RegistrarDespacho(); break;
            case DevolucionAceptada: _metricas.RegistrarDevolucion(); break;
        }
        return Task.CompletedTask;
    }
}