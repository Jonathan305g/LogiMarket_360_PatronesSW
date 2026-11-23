using LogiMarket.Application.UseCases.ConfirmarPedido.Facade;

namespace LogiMarket.Application.Commands.Command;

public sealed class ConfirmarPedidoCommand : IComando<ResultadoConfirmacion>
{
    private readonly IConfirmacionPedido _receptor;   // quien hace el trabajo real
    private readonly ConfirmarPedidoSolicitud _solicitud;

    public ConfirmarPedidoCommand(IConfirmacionPedido receptor, ConfirmarPedidoSolicitud solicitud)
    {
        _receptor = receptor ?? throw new ArgumentNullException(nameof(receptor));
        _solicitud = solicitud ?? throw new ArgumentNullException(nameof(solicitud));
    }

    public string Nombre => "ConfirmarPedido";

    public string Entrada => $"Pedido={_solicitud.PedidoId}; Clave={_solicitud.ClaveIdempotencia}";

    // El comando NO contiene lógica de negocio: solo delega en el receptor.
    public Task<ResultadoConfirmacion> EjecutarAsync(CancellationToken ct = default) =>
        _receptor.ConfirmarAsync(_solicitud, ct);

    public ResumenResultado Resumir(ResultadoConfirmacion r) =>
        r.Exitoso
            ? new ResumenResultado(true, r.EsRepeticion
                ? $"Repetición: se devolvió el pago original {r.Pago!.Referencia}"
                : $"Confirmado: pago {r.Pago!.Referencia} por {r.Pago.Monto}")
            : new ResumenResultado(false, $"{r.Error!.Codigo}: {r.Error.Mensaje}");
}