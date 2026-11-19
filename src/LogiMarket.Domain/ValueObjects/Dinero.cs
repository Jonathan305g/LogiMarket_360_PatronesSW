using System.Globalization;

namespace LogiMarket.Domain.ValueObjects;

public readonly record struct Dinero
{
    public decimal Valor { get; }

    public Dinero(decimal valor)
    {
        Valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    public static Dinero Cero => new(0m);

    public static Dinero operator +(Dinero a, Dinero b) => new(a.Valor + b.Valor);
    public static Dinero operator -(Dinero a, Dinero b) => new(a.Valor - b.Valor);
    public static Dinero operator *(Dinero a, decimal factor) => new(a.Valor * factor);
    public static Dinero operator *(Dinero a, int cantidad) => new(a.Valor * cantidad);

    public override string ToString() => Valor.ToString("F2", CultureInfo.InvariantCulture);
}