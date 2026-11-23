using LogiMarket.Application.UseCases.Despacho;

namespace LogiMarket.Application.Commands.Command;

public sealed class DespacharPedidoCommand : IComando<ResultadoDespacho>
{
    private readonly IGestionDespacho _receptor;
    private readonly Guid _pedidoId;

    public DespacharPedidoCommand(IGestionDespacho receptor, Guid pedidoId)
    {
        _receptor = receptor ?? throw new ArgumentNullException(nameof(receptor));
        _pedidoId = pedidoId;
    }

    public string Nombre => "DespacharPedido";
    public string Entrada => $"Pedido={_pedidoId}";

    public Task<ResultadoDespacho> EjecutarAsync(CancellationToken ct = default) =>
        _receptor.DespacharAsync(_pedidoId, ct);

    public ResumenResultado Resumir(ResultadoDespacho r) =>
        r.Exitoso
            ? new ResumenResultado(true, $"Despachado: guía {r.Guia}")
            : new ResumenResultado(false, $"{r.Error!.Codigo}: {r.Error.Mensaje}");
}