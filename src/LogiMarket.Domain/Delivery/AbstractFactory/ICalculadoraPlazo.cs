namespace LogiMarket.Domain.Delivery.AbstractFactory;

public interface ICalculadoraPlazo
{
    int Dias { get; }

    // Fecha estimada de entrega contando desde el momento indicado.
    DateTime CalcularFechaEstimada(DateTime desdeUtc);
}