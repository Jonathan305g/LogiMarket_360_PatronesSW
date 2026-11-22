namespace LogiMarket.Domain.Pricing.Strategy;

public sealed class Mayorista15 : IPoliticaDescuento
{
    private const int UmbralUnidades = 10;

    public string Nombre => "Mayorista15";
    public int Prioridad => 20; // mayor que Vip10: así MAYORISTA gana cuando coinciden
    public decimal Tasa => 0.15m;

    // Cuenta unidades físicas finales y no mira la categoría del cliente.
    public bool Aplica(ContextoDescuento contexto) =>
        contexto.UnidadesFisicas >= UmbralUnidades;
}