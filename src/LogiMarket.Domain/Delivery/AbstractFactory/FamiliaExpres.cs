using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Delivery.AbstractFactory;

internal sealed class TarifaExpres : ICalculadoraTarifa
{
    public Dinero Calcular() => new(12m);
}

internal sealed class PlazoExpres : ICalculadoraPlazo
{
    public int Dias => 1;

    public DateTime CalcularFechaEstimada(DateTime desdeUtc) => desdeUtc.AddDays(Dias);
}

internal sealed class EtiquetaExpres : IConstructorEtiqueta
{
    public string Construir(Pedido pedido, DateTime fechaEstimadaUtc) =>
        $"ENVÍO EXPRÉS PRIORITARIO | Guía EXP-{pedido.Id.ToString("N")[..8].ToUpperInvariant()} | " +
        $"Unidades: {pedido.UnidadesFisicas} | Entrega estimada: {fechaEstimadaUtc:yyyy-MM-dd}";
}

// FÁBRICA CONCRETA de la familia EXPRÉS.
public sealed class FabricaEntregaExpres : IFabricaEntrega
{
    public TipoEntrega Tipo => TipoEntrega.Expres;

    public ICalculadoraTarifa CrearCalculadoraTarifa() => new TarifaExpres();
    public ICalculadoraPlazo CrearCalculadoraPlazo() => new PlazoExpres();
    public IConstructorEtiqueta CrearConstructorEtiqueta() => new EtiquetaExpres();
}