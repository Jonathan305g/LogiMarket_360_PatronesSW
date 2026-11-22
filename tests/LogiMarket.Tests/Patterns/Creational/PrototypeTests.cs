using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Tests.Fakes;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Creational;

public class PrototypeTests
{
    [Fact]
    public void Clonar_GeneraPedidoNuevoEnBorradorSinHistorial()
    {
        var plantilla = PedidosDePrueba.Borrador();

        var copia = plantilla.Clonar();

        Assert.NotEqual(plantilla.Id, copia.Id);
        Assert.Equal(plantilla.ClienteId, copia.ClienteId);
        Assert.Equal(EstadoPedido.Borrador, copia.Estado);
        Assert.Empty(copia.Historial);
        Assert.Equal(60m, copia.Subtotal.Valor);
    }

    [Fact]
    public void Clonar_EditarCantidadEnUnaCopia_NoAfectaPlantillaNiOtraCopia()
    {
        var plantilla = PedidosDePrueba.Borrador();
        var copia1 = plantilla.Clonar();
        var copia2 = plantilla.Clonar();

        copia2.CambiarCantidad("KIT_TELETRABAJO", 3);

        Assert.Equal(1, plantilla.Detalles[0].Cantidad);
        Assert.Equal(1, copia1.Detalles[0].Cantidad);
        Assert.Equal(3, copia2.Detalles[0].Cantidad);
        Assert.Equal(180m, copia2.Subtotal.Valor);
    }

    [Fact]
    public void Clonar_AgregarLineaEnLaCopia_NoModificaLaPlantilla()
    {
        var plantilla = PedidosDePrueba.Borrador();
        var copia = plantilla.Clonar();

        copia.AgregarLinea(P04(), 1);

        Assert.Single(plantilla.Detalles);
        Assert.Equal(2, copia.Detalles.Count);
    }

    [Fact]
    public void Clonar_ColeccionesYLineasSonObjetosDistintos()
    {
        var plantilla = PedidosDePrueba.Borrador();
        var copia = plantilla.Clonar();

        // Copia profunda: ni la lista de líneas, ni cada línea, ni su lista de componentes se comparten.
        Assert.NotSame(plantilla.Detalles, copia.Detalles);
        Assert.NotSame(plantilla.Detalles[0], copia.Detalles[0]);
        Assert.NotSame(plantilla.Detalles[0].Componentes, copia.Detalles[0].Componentes);
    }

    [Fact]
    public void Clonar_PedidoYaValidado_SeRechaza()
    {
        var pedido = PedidosDePrueba.Borrador();
        pedido.Validar();

        Assert.Throws<DomainException>(() => pedido.Clonar());
    }
}