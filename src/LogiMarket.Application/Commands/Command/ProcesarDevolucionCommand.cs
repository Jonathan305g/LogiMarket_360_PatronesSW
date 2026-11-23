using LogiMarket.Application.UseCases.Devoluciones;

namespace LogiMarket.Application.Commands.Command;

public sealed class ProcesarDevolucionCommand : IComando<ResultadoDevolucionAplicacion>
{
    private readonly IProcesoDevolucion _receptor;
    private readonly SolicitudDevolucionDto _solicitud;

    public ProcesarDevolucionCommand(IProcesoDevolucion receptor, SolicitudDevolucionDto solicitud)
    {
        _receptor = receptor ?? throw new ArgumentNullException(nameof(receptor));
        _solicitud = solicitud ?? throw new ArgumentNullException(nameof(solicitud));
    }

    public string Nombre => "ProcesarDevolucion";
    public string Entrada => $"Pedido={_solicitud.PedidoId}; Motivo={_solicitud.Motivo}";

    public Task<ResultadoDevolucionAplicacion> EjecutarAsync(CancellationToken ct = default) =>
        _receptor.SolicitarAsync(_solicitud, ct);

    public ResumenResultado Resumir(ResultadoDevolucionAplicacion r) =>
        r.Exitoso
            ? new ResumenResultado(true, $"Devolución aceptada: reembolso {r.Devolucion!.Reembolso}")
            : new ResumenResultado(false, $"{r.Error!.Codigo}: {r.Error.Mensaje}");
}