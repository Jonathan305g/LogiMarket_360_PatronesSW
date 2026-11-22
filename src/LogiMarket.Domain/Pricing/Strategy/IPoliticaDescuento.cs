namespace LogiMarket.Domain.Pricing.Strategy;

public interface IPoliticaDescuento
{
    string Nombre { get; }

    // Cuanto mayor el número, mayor la prioridad. Es explícita, no depende del orden de un if.
    int Prioridad { get; }

    // Tasa entre 0 y 1 (0.15 = 15 %).
    decimal Tasa { get; }

    // ¿Esta política corresponde a este pedido?
    bool Aplica(ContextoDescuento contexto);
}