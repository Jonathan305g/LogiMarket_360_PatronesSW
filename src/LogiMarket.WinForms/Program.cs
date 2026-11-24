using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Commands.Command;
using LogiMarket.Application.Events.Observer;
using LogiMarket.Application.Metrics.Singleton;
using LogiMarket.Application.Notifications.FactoryMethod;
using LogiMarket.Application.Security.Proxy;
using LogiMarket.Application.UseCases.ConfirmarPedido.Facade;
using LogiMarket.Application.UseCases.Acceso;
using LogiMarket.Application.UseCases.Devoluciones;
using LogiMarket.Application.UseCases.Despacho;
using LogiMarket.Application.UseCases.Pedidos;
using LogiMarket.Infrastructure.Notificaciones;
using LogiMarket.Infrastructure.Outbox;
using LogiMarket.Infrastructure.Payments.Adapter;
using LogiMarket.Infrastructure.Payments.Decorator;
using LogiMarket.Infrastructure.Payments.Legacy;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.Infrastructure.Persistence.Auditoria;
using LogiMarket.Infrastructure.Persistence.Repositories;
using LogiMarket.Infrastructure.Persistence.Seed;
using LogiMarket.Infrastructure.Seguridad;
using LogiMarket.WinForms.Controles;
using LogiMarket.WinForms.Formularios;
using LogiMarket.WinForms.Mediador;
using LogiMarket.WinForms.Sesion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var servicios = ConfigurarServicios();
        using var alcanceApp = servicios.CreateScope();
        var proveedor = alcanceApp.ServiceProvider;

        var db = proveedor.GetRequiredService<LogiMarketDbContext>();
        db.Database.Migrate();
        DatosSemilla.SembrarAsync(db).GetAwaiter().GetResult();

        System.Windows.Forms.Application.Run(proveedor.GetRequiredService<FormularioPrincipal>());
    }

    private static ServiceProvider ConfigurarServicios()
    {
        var servicios = new ServiceCollection();
        var rutaBd = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LogiMarket360", "logimarket.db");
        Directory.CreateDirectory(Path.GetDirectoryName(rutaBd)!);
        servicios.AddDbContext<LogiMarketDbContext>(opciones => opciones.UseSqlite($"Data Source={rutaBd}"));

        servicios.AddScoped<IClienteRepository, ClienteRepositorioEf>();
        servicios.AddScoped<IPedidoRepository, PedidoRepositorioEf>();
        servicios.AddScoped<IProductoRepository, ProductoRepositorioEf>();
        servicios.AddScoped<IPagoRepository, PagoRepositorioEf>();
        servicios.AddScoped<IMovimientoInventarioRepository, MovimientoInventarioRepositorioEf>();
        servicios.AddScoped<IDespachoRepository, DespachoRepositorioEf>();
        servicios.AddScoped<IDevolucionRepository, DevolucionRepositorioEf>();
        servicios.AddScoped<IUsuarioRepository, UsuarioRepositorioEf>();
        servicios.AddScoped<ICatalogoRepository, CatalogoRepositorioEf>();
        servicios.AddScoped<IOutbox, OutboxEf>();
        servicios.AddScoped<IUnitOfWork, UnitOfWorkEf>();

        servicios.AddScoped<IBitacoraAuditoria, BitacoraAuditoriaEf>();
        servicios.AddScoped<IAuditoriaSeguridad, AuditoriaSeguridadEf>();
        servicios.AddScoped<UsuarioActualEnSesion>();
        servicios.AddScoped<IUsuarioActual>(sp => sp.GetRequiredService<UsuarioActualEnSesion>());
        servicios.AddSingleton<IHasherContrasena, HasherContrasenaPbkdf2>();
        servicios.AddScoped<IInicioSesionCasoUso, InicioSesionCasoUso>();
        servicios.AddTransient<PanelAcceso>();
        servicios.AddTransient<PanelCatalogo>();
        servicios.AddScoped<ICanalEmail, CanalEmailSimulado>();
        servicios.AddScoped<ICanalSms, CanalSmsSimulado>();
        servicios.AddScoped<CreadorAvisoEmail>();
        servicios.AddScoped<CreadorAvisoSms>();

        servicios.AddSingleton<MetricasSesion>();
        servicios.AddScoped<PasarelaLegadaSimulada>();
        servicios.AddScoped<IPasarelaPago>(sp =>
        {
            var adapter = new AdaptadorPasarelaLegada(sp.GetRequiredService<PasarelaLegadaSimulada>());
            var decorator = new AuditoriaPagoDecorator(adapter, sp.GetRequiredService<IBitacoraAuditoria>());
            return new PagoAutorizadoProxy(
                decorator,
                sp.GetRequiredService<IUsuarioActual>(),
                sp.GetRequiredService<IAuditoriaSeguridad>());
        });

        servicios.AddScoped<IObservadorEventos, ObservadorNotificaciones>();
        servicios.AddScoped<IObservadorEventos, ObservadorMetricas>();
        servicios.AddScoped<IObservadorEventos, ObservadorAuditoria>();
        servicios.AddScoped(sp =>
        {
            var publicador = new PublicadorEventos();
            foreach (var observador in sp.GetServices<IObservadorEventos>())
                publicador.Suscribir(observador);
            return publicador;
        });
        servicios.AddScoped(sp => new ProcesadorOutbox(
            sp.GetRequiredService<LogiMarketDbContext>(),
            (evento, ct) => sp.GetRequiredService<PublicadorEventos>().PublicarAsync(evento, ct)));

        servicios.AddScoped<IConfirmacionPedido, ConfirmacionPedidoFacade>();
        servicios.AddScoped<ICancelacionPedido, CancelarPedidoCasoUso>();
        servicios.AddScoped<IGestionDespacho, GestionDespachoCasoUso>();
        servicios.AddScoped<IProcesoDevolucion, ProcesoDevolucionCasoUso>();
        servicios.AddScoped<InvocadorComandos>();

        servicios.AddSingleton<IMediadorPrincipal, MediadorPrincipal>();
        servicios.AddTransient<FormularioPrincipal>();
        return servicios.BuildServiceProvider(validateScopes: true);
    }
}
