using LogiMarket.Application.Security;
using LogiMarket.Application.Validation;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.UseCases.ConfirmarPedido.Chain;

public sealed class ValidarAutorizacionYCondiciones : EslabonValidacion
{
    protected override ErrorValidacion? Evaluar(ContextoConfirmacion contexto)
    {
        // Solo ADMIN y VENTAS confirman pedidos.
        if (!PoliticaDeAcceso.PuedeCobrar(contexto.Rol))
            return new("NO_AUTORIZADO", $"El rol {contexto.Rol} no puede confirmar pedidos.");

        // Solo se confirma un pedido que todavía no fue pagado ni avanzó.
        if (contexto.Pedido.Estado is not (EstadoPedido.Borrador or EstadoPedido.Validado))
            return new("ESTADO_INVALIDO", $"No se puede confirmar un pedido en estado {contexto.Pedido.Estado}.");

        return null;
    }
}