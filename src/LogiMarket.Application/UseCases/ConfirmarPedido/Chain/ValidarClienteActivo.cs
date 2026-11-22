using LogiMarket.Application.Validation;

namespace LogiMarket.Application.UseCases.ConfirmarPedido.Chain;

public sealed class ValidarClienteActivo : EslabonValidacion
{
    protected override ErrorValidacion? Evaluar(ContextoConfirmacion contexto)
    {
        if (contexto.Cliente is null)
            return new("CLIENTE_NO_ENCONTRADO", "El cliente del pedido no existe.");

        if (!contexto.Cliente.Activo)
            return new("CLIENTE_INACTIVO", $"El cliente '{contexto.Cliente.Nombre}' está inactivo.");

        return null;
    }
}