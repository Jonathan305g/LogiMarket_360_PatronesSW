namespace LogiMarket.Domain.Pricing.Strategy;

public sealed class SinDescuento : IPoliticaDescuento
{
    public string Nombre => "SinDescuento";
    public int Prioridad => 0;
    public decimal Tasa => 0m;

    // Siempre aplica: es la política de respaldo cuando ninguna otra corresponde.
    public bool Aplica(ContextoDescuento contexto) => true;
}