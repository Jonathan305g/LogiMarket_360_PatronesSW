using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Delivery.AbstractFactory;

// El CLIENTE del Abstract Factory. Nunca nombra una familia concreta.
public class PlanificadorEntrega
{
    private readonly Dictionary<TipoEntrega, IFabricaEntrega> _fabricas;

    // Recibe todas las fábricas disponibles por constructor (DIP) y las indexa por tipo.
    public PlanificadorEntrega(IEnumerable<IFabricaEntrega> fabricas)
    {
        ArgumentNullException.ThrowIfNull(fabricas);
        _fabricas = fabricas.ToDictionary(f => f.Tipo);
    }

    public PlanEntrega Planificar(Pedido pedido, DateTime desdeUtc)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        if (!_fabricas.TryGetValue(pedido.Entrega, out var fabrica))
            throw new DomainException($"No hay una fábrica de entrega para '{pedido.Entrega}'.");

        // Las tres piezas salen de LA MISMA fábrica: no hay forma de mezclar familias.
        var tarifa = fabrica.CrearCalculadoraTarifa().Calcular();
        var plazo = fabrica.CrearCalculadoraPlazo();
        var fechaEstimada = plazo.CalcularFechaEstimada(desdeUtc);
        var etiqueta = fabrica.CrearConstructorEtiqueta().Construir(pedido, fechaEstimada);

        return new PlanEntrega(pedido.Entrega, tarifa, plazo.Dias, fechaEstimada, etiqueta);
    }
}