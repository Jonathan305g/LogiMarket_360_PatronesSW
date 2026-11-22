namespace LogiMarket.Domain.Orders.Prototype;

// Contrato de "sé copiarme a mí mismo".
public interface IPrototipo<T>
{
    T Clonar();
} 