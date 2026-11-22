using LogiMarket.Domain.Entities;

namespace LogiMarket.Domain.Delivery.AbstractFactory;

public interface IConstructorEtiqueta
{
    string Construir(Pedido pedido, DateTime fechaEstimadaUtc);
}