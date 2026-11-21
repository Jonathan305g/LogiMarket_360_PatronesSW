using LogiMarket.Domain.Catalog.Composite;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class IteratorTests
{
    [Fact]
    public void Iterador_KitTeletrabajo_DevuelveP01P02P03EnOrden()
    {
        var iterador = CrearKitTeletrabajo().CrearIterador();
        var recorrido = new List<ComponenteFisico>();

        while (iterador.TieneSiguiente())
            recorrido.Add(iterador.Siguiente());

        Assert.Equal(
            new[]
            {
                new ComponenteFisico("P01", 1),
                new ComponenteFisico("P02", 1),
                new ComponenteFisico("P03", 1)
            },
            recorrido);
    }

    [Fact]
    public void Iterador_KitConCantidad_PropagaCantidadesPorNivel()
    {
        var doble = new KitCompuesto("KIT_DOBLE", "Kit Doble");
        doble.Agregar(CrearKitOficina(), 2);
        var iterador = doble.CrearIterador();

        var primero = iterador.Siguiente();
        var segundo = iterador.Siguiente();

        Assert.Equal(new ComponenteFisico("P01", 2), primero);
        Assert.Equal(new ComponenteFisico("P02", 2), segundo);
    }

    [Fact]
    public void Iterador_AlTerminar_NoTieneSiguienteYSiguienteLanza()
    {
        var iterador = CrearKitOficina().CrearIterador();
        iterador.Siguiente();
        iterador.Siguiente();

        Assert.False(iterador.TieneSiguiente());
        Assert.Throws<InvalidOperationException>(() => iterador.Siguiente());
    }
}