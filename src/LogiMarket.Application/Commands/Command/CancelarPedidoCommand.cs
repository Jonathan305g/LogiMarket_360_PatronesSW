using LogiMarket.Application.UseCases.Pedidos;
using LogiMarket.Application.Validation;

namespace LogiMarket.Application.Commands.Command;

public sealed class CancelarPedidoCommand : IComando<ResultadoOperacion>
{
    private readonly ICancelacionPedido _receptor;
    private readonly Guid _pedidoId;

    public CancelarPedidoCommand(ICancelacionPedido receptor, Guid pedidoId)
    {
        _receptor = receptor ?? throw new ArgumentNullException(nameof(receptor));
        _pedidoId = pedidoId;
    }

    public string Nombre => "CancelarPedido";

    public string Entrada => $"Pedido={_pedidoId}";

    public Task<ResultadoOperacion> EjecutarAsync(CancellationToken ct = default) =>
        _receptor.CancelarAsync(_pedidoId, ct);

    public ResumenResultado Resumir(ResultadoOperacion r) =>
        r.Exitoso
            ? new ResumenResultado(true, "Pedido cancelado")
            : new ResumenResultado(false, $"{r.Error!.Codigo}: {r.Error.Mensaje}");
}