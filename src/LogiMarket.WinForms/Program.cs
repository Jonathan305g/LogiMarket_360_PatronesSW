using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Comprobantes.Bridge;
using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Commands.Command;
using LogiMarket.Application.Events.Observer;
using LogiMarket.Application.Metrics.Singleton;
using LogiMarket.Application.Notifications.FactoryMethod;
using LogiMarket.Application.Security.Proxy;
using LogiMarket.Application.UseCases.ConfirmarPedido.Facade;
using LogiMarket.Application.UseCases.ConfirmarPedido.Chain;
using LogiMarket.Application.UseCases.Acceso;
using LogiMarket.Application.UseCases.Devoluciones;
using LogiMarket.Application.UseCases.Despacho;
using LogiMarket.Application.UseCases.Pedidos;
using LogiMarket.Application.UseCases.Catalogo;
using LogiMarket.Application.UseCases.Clientes;
using LogiMarket.Application.UseCases.Inventario;
using LogiMarket.Infrastructure.Notificaciones;
using LogiMarket.Infrastructure.Comprobantes;
using LogiMarket.Infrastructure.Outbox;
using LogiMarket.Infrastructure.Payments.Adapter;
using LogiMarket.Infrastructure.Payments.Decorator;
using LogiMarket.Infrastructure.Payments.Legacy;
using LogiMarket.Infrastructure.Payments;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.Infrastructure.Persistence.Auditoria;
using LogiMarket.Infrastructure.Persistence.Repositories;
using LogiMarket.Infrastructure.Persistence.Seed;
using LogiMarket.Infrastructure.Seguridad;
using LogiMarket.Domain.Delivery.AbstractFactory;
using LogiMarket.Domain.Pricing.Strategy;
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
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var servicios = ConfigurarServicios();
        using var alcanceApp = servicios.CreateScope();
        var proveedor = alcanceApp.ServiceProvider;

        var db = proveedor.GetRequiredService<LogiMarketDbContext>();
        db.Database.Migrate();
        DatosSemilla.SembrarAsync(db).GetAwaiter().GetResult();

        if (args.Length > 0 && args[0] == "--capturas")
        {
            var rutaDestino = args.Length > 1
                ? args[1]
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "capturas");
            EjecutarGeneracionCapturas(proveedor, rutaDestino);
            return;
        }

        System.Windows.Forms.Application.Run(proveedor.GetRequiredService<FormularioPrincipal>());
    }

    private static void EjecutarGeneracionCapturas(IServiceProvider proveedor, string rutaDestino)
    {
        Directory.CreateDirectory(rutaDestino);
        var form = proveedor.GetRequiredService<FormularioPrincipal>();
        var mediador = proveedor.GetRequiredService<IMediadorPrincipal>();
        var sesion = proveedor.GetRequiredService<UsuarioActualEnSesion>();
        var usuarios = proveedor.GetRequiredService<IUsuarioRepository>();

        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(50, 50);
        form.Size = new Size(1280, 820);
        form.Show();

        void Esperar(int ms)
        {
            var fin = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < fin)
            {
                System.Windows.Forms.Application.DoEvents();
                Thread.Sleep(30);
            }
        }

        void Capturar(string nombreArchivo)
        {
            form.Refresh();
            Esperar(600);
            var ruta = Path.Combine(rutaDestino, nombreArchivo);
            
            // Intento 1: DrawToBitmap para renderizado interno fiel de controles
            using var bmp = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
            bmp.Save(ruta, System.Drawing.Imaging.ImageFormat.Png);

            // Intento 2: CopyFromScreen si el escritorio está activo
            try
            {
                var screenRuta = Path.Combine(rutaDestino, "screen_" + nombreArchivo);
                var bounds = form.Bounds;
                using var bmpScreen = new Bitmap(bounds.Width, bounds.Height);
                using var g = Graphics.FromImage(bmpScreen);
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                bmpScreen.Save(screenRuta, System.Drawing.Imaging.ImageFormat.Png);
            }
            catch
            {
                // En entornos sin escritorio activo CopyFromScreen puede no estar disponible
            }
        }

        // 1. Acceso
        mediador.MostrarPanel(TipoPanel.Acceso);
        Capturar("captura_01_acceso.png");

        // Autenticar como Admin
        var admin = usuarios.ObtenerPorNombreUsuarioAsync("admin").GetAwaiter().GetResult();
        if (admin != null)
        {
            sesion.IniciarSesion(admin);
            form.ActualizarEstadoSesion();
        }

        // Paneles restantes
        var paneles = new (TipoPanel Panel, string Nombre)[]
        {
            (TipoPanel.PanelGeneral, "captura_02_panel_general.png"),
            (TipoPanel.Catalogo, "captura_03_catalogo.png"),
            (TipoPanel.Clientes, "captura_04_clientes.png"),
            (TipoPanel.Carrito, "captura_05_carrito.png"),
            (TipoPanel.Pedidos, "captura_06_pedidos.png"),
            (TipoPanel.Cobro, "captura_07_cobro.png"),
            (TipoPanel.Despacho, "captura_08_despacho.png"),
            (TipoPanel.Devoluciones, "captura_09_devoluciones.png"),
            (TipoPanel.Inventario, "captura_10_inventario.png"),
            (TipoPanel.Reportes, "captura_11_reportes.png"),
            (TipoPanel.Auditoria, "captura_12_auditoria.png")
        };

        foreach (var (p, nombre) in paneles)
        {
            Console.WriteLine($"[Captura] Mostrando panel {p}...");
            try
            {
                mediador.MostrarPanel(p);
                Capturar(nombre);
                Console.WriteLine($"[Captura] Guardada {nombre}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Captura] Error en {p}: {ex.Message}");
            }
        }

        form.Close();
    }

    private static ServiceProvider ConfigurarServicios()
    {
        var servicios = new ServiceCollection();
        var rutaBd = Environment.GetEnvironmentVariable("LOGIMARKET_DB_PATH");
        if (string.IsNullOrWhiteSpace(rutaBd))
        {
            rutaBd = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LogiMarket360", "logimarket.db");
        }
        rutaBd = Path.GetFullPath(rutaBd);
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
        servicios.AddScoped<IAdministracionCatalogoRepository, AdministracionCatalogoRepositorioEf>();
        servicios.AddScoped<IOutbox, OutboxEf>();
        servicios.AddScoped<IUnitOfWork, UnitOfWorkEf>();

        servicios.AddScoped<IBitacoraAuditoria, BitacoraAuditoriaEf>();
        servicios.AddScoped<IAuditoriaSeguridad, AuditoriaSeguridadEf>();
        servicios.AddScoped<UsuarioActualEnSesion>();
        servicios.AddScoped<IUsuarioActual>(sp => sp.GetRequiredService<UsuarioActualEnSesion>());
        servicios.AddSingleton<IHasherContrasena, HasherContrasenaPbkdf2>();
        servicios.AddScoped<IInicioSesionCasoUso, InicioSesionCasoUso>();
        servicios.AddScoped<IGestionCatalogo, GestionCatalogoCasoUso>();
        servicios.AddScoped<IGestionClientes, GestionClientesCasoUso>();
        servicios.AddScoped<IAjustarInventarioCasoUso, AjustarInventarioCasoUso>();
        servicios.AddScoped<IFabricaSalidaArchivo, FabricaSalidaArchivo>();
        servicios.AddScoped<ICotizadorPedido, CotizadorPedidoCasoUso>();
        servicios.AddScoped<IValidarPedidoCasoUso, ValidarPedidoCasoUso>();

        // Colaboradores concretos que necesita el Facade de confirmación.
        servicios.AddScoped<EslabonValidacion>(_ => CadenaValidacionConfirmacion.Crear());
        servicios.AddScoped<IPoliticaDescuento, Mayorista15>();
        servicios.AddScoped<IPoliticaDescuento, Vip10>();
        servicios.AddScoped<IPoliticaDescuento, SinDescuento>();
        servicios.AddScoped<CalculadoraDescuento>();
        servicios.AddScoped<IFabricaEntrega, FabricaEntregaEstandar>();
        servicios.AddScoped<IFabricaEntrega, FabricaEntregaExpres>();
        servicios.AddScoped<PlanificadorEntrega>();
        servicios.AddTransient<PanelAcceso>();
        servicios.AddTransient<PanelGeneral>();
        servicios.AddTransient<PanelCatalogo>();
        servicios.AddTransient<PanelClientes>();
        servicios.AddTransient<PanelCarrito>();
        servicios.AddTransient<PanelPedidos>();
        servicios.AddTransient<PanelCobro>();
        servicios.AddTransient<PanelDespacho>();
        servicios.AddTransient<PanelDevoluciones>();
        servicios.AddTransient<PanelInventario>();
        servicios.AddTransient<PanelReportes>();
        servicios.AddTransient<PanelAuditoria>();
        servicios.AddScoped<ICanalEmail, CanalEmailSimulado>();
        servicios.AddScoped<ICanalSms, CanalSmsSimulado>();
        servicios.AddScoped<CreadorAvisoEmail>();
        servicios.AddScoped<CreadorAvisoSms>();

        servicios.AddSingleton<MetricasSesion>();
        servicios.AddScoped<PasarelaLegadaSimulada>();
        servicios.AddScoped<IPasarelaPagoDemo>(sp => sp.GetRequiredService<PasarelaLegadaSimulada>());
        servicios.AddScoped<PasarelaModernaSimulada>();
        servicios.AddScoped(sp => new SelectorPasarelaPago(
            new AuditoriaPagoDecorator(
                new AdaptadorPasarelaLegada(sp.GetRequiredService<PasarelaLegadaSimulada>()),
                sp.GetRequiredService<IBitacoraAuditoria>()),
            new AuditoriaPagoDecorator(
                sp.GetRequiredService<PasarelaModernaSimulada>(),
                sp.GetRequiredService<IBitacoraAuditoria>())));
        servicios.AddScoped<ISelectorPasarelaPago>(sp => sp.GetRequiredService<SelectorPasarelaPago>());
        servicios.AddScoped<IPasarelaPago>(sp =>
        {
            return new PagoAutorizadoProxy(
                sp.GetRequiredService<SelectorPasarelaPago>(),
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
        servicios.AddScoped<IProcesadorOutbox>(sp => sp.GetRequiredService<ProcesadorOutbox>());

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
