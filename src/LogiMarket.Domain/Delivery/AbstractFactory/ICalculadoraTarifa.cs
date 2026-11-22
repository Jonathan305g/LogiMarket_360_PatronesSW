using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Delivery.AbstractFactory;

public interface ICalculadoraTarifa
{
    Dinero Calcular();
}