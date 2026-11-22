using LogiMarket.Domain.Enums;

namespace LogiMarket.Domain.Delivery.AbstractFactory;

// La FÁBRICA ABSTRACTA: sabe crear las tres piezas de UNA familia.
public interface IFabricaEntrega
{
    // Qué tipo de entrega representa esta familia.
    TipoEntrega Tipo { get; }

    ICalculadoraTarifa CrearCalculadoraTarifa();
    ICalculadoraPlazo CrearCalculadoraPlazo();
    IConstructorEtiqueta CrearConstructorEtiqueta();
}