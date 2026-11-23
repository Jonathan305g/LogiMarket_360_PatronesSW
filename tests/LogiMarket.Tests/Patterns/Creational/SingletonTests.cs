using LogiMarket.Application.Metrics.Singleton;

namespace LogiMarket.Tests.Patterns.Creational;

public class SingletonTests
{
    // Simula lo que hará el contenedor de DI: UNA instancia entregada a dos consumidores.
    private sealed class ConsumidorDePantalla
    {
        private readonly MetricasSesion _metricas;
        public ConsumidorDePantalla(MetricasSesion metricas) => _metricas = metricas;
        public int PedidosPagadosQueVe => _metricas.PedidosPagados;
    }

    [Fact]
    public void DosConsumidores_ConLaMismaInstancia_VenLosMismosContadores()
    {
        var metricas = new MetricasSesion(); // lo que el contenedor de DI creará una sola vez
        var pantallaA = new ConsumidorDePantalla(metricas);
        var pantallaB = new ConsumidorDePantalla(metricas);

        metricas.RegistrarPedidoPagado();
        metricas.RegistrarPedidoPagado();

        Assert.Equal(2, pantallaA.PedidosPagadosQueVe);
        Assert.Equal(2, pantallaB.PedidosPagadosQueVe); // la MISMA instancia, mismo valor
    }

    [Fact]
    public void Contadores_SeAcumulanPorTipoDeEvento()
    {
        var metricas = new MetricasSesion();

        metricas.RegistrarPedidoPagado();
        metricas.RegistrarDespacho();
        metricas.RegistrarDespacho();
        metricas.RegistrarDevolucion();

        Assert.Equal(1, metricas.PedidosPagados);
        Assert.Equal(2, metricas.DespachosRegistrados);
        Assert.Equal(1, metricas.DevolucionesAceptadas);
    }

    [Fact]
    public void Contadores_SonSegurosAlIncrementarDesdeVariosHilosALaVez()
    {
        var metricas = new MetricasSesion();

        Parallel.For(0, 1000, _ => metricas.RegistrarPedidoPagado());

        Assert.Equal(1000, metricas.PedidosPagados); // sin pérdidas por condición de carrera
    }
}