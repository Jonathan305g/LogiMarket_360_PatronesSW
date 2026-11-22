using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Pricing.Strategy;

public sealed class Vip10 : IPoliticaDescuento
{
    public string Nombre => "Vip10";
    public int Prioridad => 10;
    public decimal Tasa => 0.10m;

    public bool Aplica(ContextoDescuento contexto) =>
        contexto.Categoria == CategoriaCliente.Vip;
}