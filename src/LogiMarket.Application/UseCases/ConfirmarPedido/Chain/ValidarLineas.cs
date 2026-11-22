using LogiMarket.Application.Validation;

namespace LogiMarket.Application.UseCases.ConfirmarPedido.Chain;

public sealed class ValidarLineas : EslabonValidacion
{
    protected override ErrorValidacion? Evaluar(ContextoConfirmacion contexto)
    {
        var pedido = contexto.Pedido;

        // Defensa extra: el Builder y el dominio ya lo impiden, pero esta cadena no
        // da por sentado que los datos que le llegan son correctos.
        if (pedido.Detalles.Count == 0)
            return new("SIN_LINEAS", "El pedido no tiene líneas.");

        if (pedido.Detalles.Any(d => d.Cantidad <= 0))
            return new("CANTIDAD_INVALIDA", "Todas las cantidades deben ser mayores que cero.");

        // Cada SKU físico del pedido debe seguir existiendo en el catálogo.
        var inexistentes = pedido.ComponentesFisicosTotales()
            .Where(c => !contexto.StockDisponible.ContainsKey(c.Sku))
            .Select(c => c.Sku)
            .ToList();

        if (inexistentes.Count > 0)
            return new("PRODUCTO_NO_DISPONIBLE",
                $"Productos que ya no existen en el catálogo: {string.Join(", ", inexistentes)}.");

        return null;
    }
}