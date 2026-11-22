using LogiMarket.Application.Validation;

namespace LogiMarket.Application.UseCases.ConfirmarPedido.Chain;

public sealed class ValidarStock : EslabonValidacion
{
    protected override ErrorValidacion? Evaluar(ContextoConfirmacion contexto)
    {
        // Se compara por SKU FÍSICO: un kit pide sus componentes ya expandidos.
        var faltantes = contexto.Pedido.ComponentesFisicosTotales()
            .Select(c => new
            {
                c.Sku,
                Requerido = c.Cantidad,
                Disponible = contexto.StockDisponible.GetValueOrDefault(c.Sku)
            })
            .Where(x => x.Disponible < x.Requerido)
            .Select(x => $"{x.Sku}: requerido {x.Requerido}, disponible {x.Disponible}")
            .ToList();

        return faltantes.Count == 0
            ? null
            : new ErrorValidacion("STOCK_INSUFICIENTE", $"Stock insuficiente → {string.Join("; ", faltantes)}.");
    }
}