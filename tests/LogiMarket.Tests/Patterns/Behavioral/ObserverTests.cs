using LogiMarket.Application.Events.Observer;
using LogiMarket.Application.Metrics.Singleton;
using LogiMarket.Application.Notifications.FactoryMethod;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.ValueObjects;
using LogiMarket.Tests.Fakes;

namespace LogiMarket.Tests.Patterns.Behavioral;

public class ObserverTests
{
    // Todo el "mundo" de una prueba: los tres observadores ya suscritos.
    private sealed class Mundo
    {
        public CanalEmailEspia Email { get; } = new();
        public CanalSmsEspia Sms { get; } = new();
        public BitacoraAuditoriaEspia Bitacora { get; } = new();
        public MetricasSesion Metricas { get; } = new();
        public PublicadorEventos Publicador { get; }

        public Mundo()
        {
            Publicador = new PublicadorEventos();
            Publicador.Suscribir(new ObservadorNotificaciones(new CreadorAvisoEmail(Email), new CreadorAvisoSms(Sms)));
            Publicador.Suscribir(new ObservadorMetricas(Metricas));
            Publicador.Suscribir(new ObservadorAuditoria(Bitacora));
        }
    }

    [Fact]
    public async Task Publicar_PedidoPagado_ActualizaNotificacionMetricaYAuditoriaUnaVez()   // P30
    {
        var mundo = new Mundo();
        var evento = new PedidoPagado(Guid.NewGuid(), Guid.NewGuid(), new Dinero(59m), "REF-1", DateTime.UtcNow);

        await mundo.Publicador.PublicarAsync(evento);

        var correo = Assert.Single(mundo.Email.Enviados);          // EMAIL para pago (P11)
        Assert.Contains("59.00", correo.Cuerpo);
        Assert.Empty(mundo.Sms.Enviados);
        Assert.Equal(1, mundo.Metricas.PedidosPagados);
        var registro = Assert.Single(mundo.Bitacora.Registros);
        Assert.Equal(nameof(PedidoPagado), registro.TipoEvento);
    }

    [Fact]
    public async Task Publicar_DespachoRegistrado_AlternaASmsYActualizaSusPropiosContadores()   // P11
    {
        var mundo = new Mundo();
        var evento = new DespachoRegistrado(Guid.NewGuid(), "ENVÍO EXPRÉS PRIORITARIO | Guía EXP-ABC123", DateTime.UtcNow);

        await mundo.Publicador.PublicarAsync(evento);

        Assert.Empty(mundo.Email.Enviados);
        var sms = Assert.Single(mundo.Sms.Enviados);               // SMS para despacho: se ALTERNÓ el canal
        Assert.Contains("despachado", sms.Mensaje);
        Assert.Equal(1, mundo.Metricas.DespachosRegistrados);
        Assert.Equal(0, mundo.Metricas.PedidosPagados); // no se mezclan los contadores
    }

    [Fact]
    public async Task Publicar_DevolucionAceptada_NotificaPorEmailYAuditaElReembolso()
    {
        var mundo = new Mundo();
        var evento = new DevolucionAceptada(Guid.NewGuid(), Guid.NewGuid(), new Dinero(54m), DateTime.UtcNow);

        await mundo.Publicador.PublicarAsync(evento);

        var correo = Assert.Single(mundo.Email.Enviados);
        Assert.Contains("54.00", correo.Cuerpo);
        Assert.Equal(1, mundo.Metricas.DevolucionesAceptadas);
        Assert.Contains("54.00", Assert.Single(mundo.Bitacora.Registros).Detalle);
    }

    [Fact]
    public async Task Publicar_TresEventosDistintos_CadaUnoActualizaSoloSuContador()
    {
        var mundo = new Mundo();

        await mundo.Publicador.PublicarAsync(new PedidoPagado(Guid.NewGuid(), Guid.NewGuid(), new Dinero(59m), "R1", DateTime.UtcNow));
        await mundo.Publicador.PublicarAsync(new DespachoRegistrado(Guid.NewGuid(), "GUIA-1", DateTime.UtcNow));
        await mundo.Publicador.PublicarAsync(new DevolucionAceptada(Guid.NewGuid(), Guid.NewGuid(), new Dinero(54m), DateTime.UtcNow));

        Assert.Equal(1, mundo.Metricas.PedidosPagados);
        Assert.Equal(1, mundo.Metricas.DespachosRegistrados);
        Assert.Equal(1, mundo.Metricas.DevolucionesAceptadas);
        Assert.Equal(3, mundo.Bitacora.Registros.Count);
        Assert.Equal(2, mundo.Email.Enviados.Count); // pago + devolución
        Assert.Single(mundo.Sms.Enviados);           // despacho
    }

    // Suscriptor de prueba: demuestra que se puede AGREGAR uno nuevo sin tocar los existentes.
    private sealed class ContadorDeEventos : IObservadorEventos
    {
        public int Recibidos { get; private set; }
        public Task ManejarAsync(IEventoDominio evento, CancellationToken ct = default)
        {
            Recibidos++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Suscribir_UnObservadorNuevo_SeAgregaSinEditarLosExistentes()
    {
        var mundo = new Mundo();
        var contador = new ContadorDeEventos();
        mundo.Publicador.Suscribir(contador);

        await mundo.Publicador.PublicarAsync(new PedidoPagado(Guid.NewGuid(), Guid.NewGuid(), new Dinero(10m), "R", DateTime.UtcNow));

        Assert.Equal(1, contador.Recibidos);
        Assert.Equal(1, mundo.Metricas.PedidosPagados); // los demás siguen funcionando igual
    }
}