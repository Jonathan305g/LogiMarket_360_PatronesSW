using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Domain.Events;

namespace LogiMarket.Application.Events.Observer;

public sealed class ObservadorAuditoria : IObservadorEventos
{
    private readonly IBitacoraAuditoria _bitacora;
    public ObservadorAuditoria(IBitacoraAuditoria bitacora) => _bitacora = bitacora;

    public Task ManejarAsync(IEventoDominio evento, CancellationToken ct = default)
    {
        var detalle = evento switch
        {
            PedidoPagado p => $"Pedido {p.PedidoId} pagado por {p.Total} (ref. {p.ReferenciaPago}).",
            DespachoRegistrado d => $"Pedido {d.PedidoId} despachado con guía {d.Guia}.",
            DevolucionAceptada v => $"Pedido {v.PedidoId} devuelto; reembolso {v.Reembolso}.",
            _ => evento.GetType().Name
        };

        _bitacora.Registrar(evento.GetType().Name, detalle, evento.OcurrioUtc);
        return Task.CompletedTask;
    }
}