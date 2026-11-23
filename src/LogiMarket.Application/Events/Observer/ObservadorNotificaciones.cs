using LogiMarket.Application.Notifications.FactoryMethod;
using LogiMarket.Domain.Events;

namespace LogiMarket.Application.Events.Observer;

// Decide, SEGÚN EL TIPO DE EVENTO, qué Creador de aviso usar y con qué mensaje.
// Esta selección vive aquí, fuera de WinForms, como exige la guía.
public sealed class ObservadorNotificaciones : IObservadorEventos
{
    private readonly CreadorAvisoEmail _email;
    private readonly CreadorAvisoSms _sms;

    public ObservadorNotificaciones(CreadorAvisoEmail email, CreadorAvisoSms sms)
    {
        _email = email;
        _sms = sms;
    }

    public Task ManejarAsync(IEventoDominio evento, CancellationToken ct = default)
    {
        var (creador, destinatario, asunto, cuerpo) = Describir(evento);
        return creador.NotificarAsync(destinatario, asunto, cuerpo, ct);
    }

    // Pago -> EMAIL (constancia formal). Despacho -> SMS (aviso corto). Devolución -> EMAIL.
    // Así se demuestra "alternar EMAIL/SMS tras evento" (caso P11) sin ningún if en WinForms.
    private (CreadorAviso Creador, string Destinatario, string Asunto, string Cuerpo) Describir(IEventoDominio evento) => evento switch
    {
        PedidoPagado p => (_email, $"cliente-{p.ClienteId:N}@logimarket.test", "Pago confirmado",
            $"Tu pedido {p.PedidoId} fue pagado por {p.Total}. Referencia: {p.ReferenciaPago}."),

        DespachoRegistrado d => (_sms, $"+593-SIM-{d.PedidoId.ToString("N")[..8]}", "",
            $"Pedido {d.PedidoId} despachado. Guía: {d.Guia}."),

        DevolucionAceptada v => (_email, $"pedido-{v.PedidoId:N}@logimarket.test", "Devolución aceptada",
            $"Se aceptó la devolución del pedido {v.PedidoId}. Reembolso: {v.Reembolso}."),

        _ => throw new ArgumentOutOfRangeException(nameof(evento), $"No hay aviso configurado para {evento.GetType().Name}.")
    };
}