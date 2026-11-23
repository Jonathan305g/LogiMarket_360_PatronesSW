namespace LogiMarket.Application.Metrics.Singleton;

// Se registrará como ÚNICO objeto en el contenedor de DI (vida Singleton), tal como
// pide la guía. No usa un "Instance" estático: la unicidad la da la configuración de DI,
// que es la forma recomendada de este patrón en una app con contenedor de dependencias.
public sealed class MetricasSesion
{
    private readonly object _bloqueo = new();
    private int _pedidosPagados;
    private int _despachosRegistrados;
    private int _devolucionesAceptadas;

    public int PedidosPagados { get { lock (_bloqueo) return _pedidosPagados; } }
    public int DespachosRegistrados { get { lock (_bloqueo) return _despachosRegistrados; } }
    public int DevolucionesAceptadas { get { lock (_bloqueo) return _devolucionesAceptadas; } }

    public void RegistrarPedidoPagado() { lock (_bloqueo) _pedidosPagados++; }
    public void RegistrarDespacho() { lock (_bloqueo) _despachosRegistrados++; }
    public void RegistrarDevolucion() { lock (_bloqueo) _devolucionesAceptadas++; }
}