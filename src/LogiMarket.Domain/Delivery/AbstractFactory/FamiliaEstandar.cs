using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Delivery.AbstractFactory;

internal sealed class TarifaEstandar : ICalculadoraTarifa
{
    public Dinero Calcular() => new(5m);
}

internal sealed class PlazoEstandar : ICalculadoraPlazo
{
    public int Dias => 3;

    public DateTime CalcularFechaEstimada(DateTime desdeUtc) => desdeUtc.AddDays(Dias);
}

internal sealed class EtiquetaEstandar : IConstructorEtiqueta
{
    public string Construir(Pedido pedido, DateTime fechaEstimadaUtc) =>
        $"ENVÍO ESTÁNDAR | Guía STD-{pedido.Id.ToString("N")[..8].ToUpperInvariant()} | " +
        $"Unidades: {pedido.UnidadesFisicas} | Entrega estimada: {fechaEstimadaUtc:yyyy-MM-dd}";
}

// FÁBRICA CONCRETA de la familia ESTÁNDAR.
public sealed class FabricaEntregaEstandar : IFabricaEntrega
{
    public TipoEntrega Tipo => TipoEntrega.Estandar;

    public ICalculadoraTarifa CrearCalculadoraTarifa() => new TarifaEstandar();
    public ICalculadoraPlazo CrearCalculadoraPlazo() => new PlazoEstandar();
    public IConstructorEtiqueta CrearConstructorEtiqueta() => new EtiquetaEstandar();
}