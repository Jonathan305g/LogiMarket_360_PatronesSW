using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Catalog.Visitor;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.ValueObjects;
using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.UseCases.ConfirmarPedido.Chain;
using LogiMarket.Application.UseCases.ConfirmarPedido.Facade;
using LogiMarket.Domain.Delivery.AbstractFactory;
using LogiMarket.Domain.Pricing.Strategy;
using LogiMarket.Tests.Fakes;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.Infrastructure.Persistence.Repositories;
using LogiMarket.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Infrastructure;

public sealed class RepositoriosEfTests : IDisposable
{
    private readonly string _rutaBd = Path.Combine(Path.GetTempPath(), $"logimarket_repos_{Guid.NewGuid():N}.db");

    private LogiMarketDbContext CrearContexto() => new(new DbContextOptionsBuilder<LogiMarketDbContext>()
        .UseSqlite($"Data Source={_rutaBd};Pooling=False;Default Timeout=30")
        .Options);

    private async Task<LogiMarketDbContext> CrearYSembrarAsync()
    {
        var db = CrearContexto();
        await db.Database.MigrateAsync();
        await DatosSemilla.SembrarAsync(db);
        return db;
    }

    [Fact]
    public async Task ProductoRepositorio_ObtenerPorSkus_DevuelveSoloLosSolicitados()
    {
        await using var db = await CrearYSembrarAsync();

        var productos = await new ProductoRepositorioEf(db)
            .ObtenerPorSkusAsync(new[] { "P01", "P03", "NO_EXISTE" });

        Assert.Equal(2, productos.Count);
        Assert.Equal(new[] { "P01", "P03" }, productos.Select(p => p.Sku).Order().ToArray());
    }

    [Fact]
    public async Task CatalogoRepositorio_ResuelveProductoSimple()
    {
        await using var db = await CrearYSembrarAsync();

        var elemento = await new CatalogoRepositorioEf(db).ObtenerPorSkuAsync("P01");

        Assert.IsType<ProductoSimple>(elemento);
        Assert.Equal("P01", elemento!.Sku);
    }

    [Fact]
    public async Task CatalogoRepositorio_ResuelveKitAnidadoConSusComponentes()
    {
        await using var db = await CrearYSembrarAsync();

        var elemento = await new CatalogoRepositorioEf(db).ObtenerPorSkuAsync("KIT_TELETRABAJO");
        Assert.NotNull(elemento);

        var visitor = new PesoLogisticoVisitor();
        elemento!.Aceptar(visitor, 1);

        Assert.Equal(1.00m, visitor.PesoTotalKg);
        Assert.Equal(new[] { "P01", "P02", "P03" }, elemento.ExpandirComponentes()
            .Select(c => c.Sku).Order().ToArray());
    }

    [Fact]
    public async Task CatalogoRepositorio_SkuInexistente_DevuelveNull()
    {
        await using var db = await CrearYSembrarAsync();

        Assert.Null(await new CatalogoRepositorioEf(db).ObtenerPorSkuAsync("NO_EXISTE"));
    }

    [Fact]
    public async Task CatalogoRepositorio_ListarResumen_IncluyeProductosYKitsConSuTipo()
    {
        await using var db = await CrearYSembrarAsync();

        var resumen = await new CatalogoRepositorioEf(db).ListarResumenAsync();

        Assert.Equal(7, resumen.Count);
        var p01 = resumen.Single(r => r.Sku == "P01");
        Assert.False(p01.EsKit);
        Assert.Equal(8, p01.Stock);
        var kit = resumen.Single(r => r.Sku == "KIT_OFICINA");
        Assert.True(kit.EsKit);
        Assert.Null(kit.Stock);
    }

    [Fact]
    public async Task AdministracionCatalogo_ProductoSeCreaEditaYEliminaSiNoTieneReferencias()
    {
        await using var db = await CrearYSembrarAsync();
        var repo = new AdministracionCatalogoRepositorioEf(db);
        var uow = new UnitOfWorkEf(db);
        var producto = new Producto("P06", "Base", new Dinero(2m), 0.1m, 3);
        repo.AgregarProducto(producto);
        await uow.CommitAsync();
        db.ChangeTracker.Clear();

        var editable = await repo.ObtenerProductoParaEdicionAsync("P06");
        editable!.ActualizarDatos("Base nueva", new Dinero(4m), 0.2m);
        await uow.CommitAsync();
        db.ChangeTracker.Clear();
        var guardado = await db.Productos.SingleAsync(p => p.Sku == "P06");
        Assert.Equal("Base nueva", guardado.Nombre);
        Assert.Equal(4m, guardado.Precio.Valor);

        Assert.True(await repo.EliminarProductoAsync("P06"));
        await uow.CommitAsync();
        Assert.False(await db.Productos.AnyAsync(p => p.Sku == "P06"));
    }

    [Fact]
    public async Task AdministracionCatalogo_NoEliminaProductoReferenciadoPorKit()
    {
        await using var db = await CrearYSembrarAsync();
        var repo = new AdministracionCatalogoRepositorioEf(db);
        await repo.GuardarKitAsync("KIT_PRUEBA", "Kit prueba",
            new[] { new ComponenteKitSolicitud("P01", 1, false) });
        await new UnitOfWorkEf(db).CommitAsync();

        Assert.False(await repo.EliminarProductoAsync("P01"));
    }

    [Fact]
    public async Task AdministracionCatalogo_PuedeActualizarComponentesDeKitSinPerderComposicion()
    {
        await using var db = await CrearYSembrarAsync();
        var repo = new AdministracionCatalogoRepositorioEf(db);
        await repo.GuardarKitAsync("KIT_EDITABLE", "Kit editable",
            new[] { new ComponenteKitSolicitud("P01", 2, false), new ComponenteKitSolicitud("KIT_OFICINA", 1, true) });
        await new UnitOfWorkEf(db).CommitAsync();
        db.ChangeTracker.Clear();

        var kit = Assert.IsType<KitCompuesto>(await new CatalogoRepositorioEf(db).ObtenerPorSkuAsync("KIT_EDITABLE"));
        Assert.Equal(2, kit.Items.Count);
        Assert.Equal(90m, kit.ObtenerPrecio().Valor);
    }

    [Fact]
    public async Task GestionCatalogo_AdminCreaProductoYKitYRechazaCiclo()
    {
        await using var db = await CrearYSembrarAsync();
        var gestion = new LogiMarket.Application.UseCases.Catalogo.GestionCatalogoCasoUso(
            new AdministracionCatalogoRepositorioEf(db),
            new CatalogoRepositorioEf(db),
            new UnitOfWorkEf(db),
            new UsuarioActualFalso(RolUsuario.Admin));

        var alta = await gestion.GuardarProductoAsync("P88", "Soporte", 12m, 0.4m, 2);
        var kit = await gestion.GuardarKitAsync("KIT_SOPORTE", "Kit soporte",
            new[] { new ComponenteKitSolicitud("P88", 1, false), new ComponenteKitSolicitud("KIT_OFICINA", 1, true) });
        var ciclo = await gestion.GuardarKitAsync("KIT_OFICINA", "Kit Oficina",
            new[] { new ComponenteKitSolicitud("KIT_TELETRABAJO", 1, true) });

        Assert.True(alta.Exitoso);
        Assert.True(kit.Exitoso);
        Assert.False(ciclo.Exitoso);
        Assert.Equal("KIT_INVALIDO", ciclo.Error!.Codigo);
        var resuelto = Assert.IsType<KitCompuesto>(await new CatalogoRepositorioEf(db).ObtenerPorSkuAsync("KIT_SOPORTE"));
        Assert.Equal(62m, resuelto.ObtenerPrecio().Valor);
    }

    [Fact]
    public async Task ConfirmarConcurrente_StockDeUnaUnidad_NoPermiteSobreventa()
    {
        await using (var db = await CrearYSembrarAsync())
        {
            var cliente = new Cliente("Concurrente", CategoriaCliente.Normal);
            var producto = new Producto("P99", "Última unidad", new Dinero(10m), 0.1m, 1);
            db.Clientes.Add(cliente);
            db.Productos.Add(producto);
            db.Pedidos.Add(new PedidoBuilder().ConCliente(cliente).AgregarLinea(new ProductoSimple(producto), 1).Construir());
            db.Pedidos.Add(new PedidoBuilder().ConCliente(cliente).AgregarLinea(new ProductoSimple(producto), 1).Construir());
            await db.SaveChangesAsync();
        }

        List<Guid> ids;
        await using (var db = CrearContexto())
            ids = await db.Pedidos.Select(p => p.Id).ToListAsync();
        var pasarela = new PasarelaConcurrente();
        var confirmaciones = ids.Select((id, indice) => ConfirmarConContextoNuevoAsync(id, indice == 0 ? "A" : "B", pasarela));
        var resultados = await Task.WhenAll(confirmaciones);

        Assert.Single(resultados, r => r.Exitoso);
        Assert.Single(resultados, r => !r.Exitoso);
        await using var verificacion = CrearContexto();
        Assert.Equal(0, (await verificacion.Productos.SingleAsync(p => p.Sku == "P99")).Stock);
        Assert.Single(await verificacion.Pagos.ToListAsync());
        Assert.Single(await verificacion.Movimientos.ToListAsync());
        Assert.Single(await verificacion.EventosOutbox.ToListAsync());
        var resumenes = await new PedidoRepositorioEf(verificacion).ListarResumenAsync();
        Assert.Single(resumenes, p => p.Estado == EstadoPedido.Pagado);
    }

    private async Task<ResultadoConfirmacion> ConfirmarConContextoNuevoAsync(Guid pedidoId, string clave, IPasarelaPago pasarela)
    {
        await using var db = CrearContexto();
        var facade = new ConfirmacionPedidoFacade(
            CadenaValidacionConfirmacion.Crear(),
            new ClienteRepositorioEf(db),
            new PedidoRepositorioEf(db),
            new ProductoRepositorioEf(db),
            new PagoRepositorioEf(db),
            new MovimientoInventarioRepositorioEf(db),
            new OutboxEf(db),
            new UnitOfWorkEf(db),
            new CalculadoraDescuento(new IPoliticaDescuento[] { new Mayorista15(), new Vip10(), new SinDescuento() }),
            new PlanificadorEntrega(new IFabricaEntrega[] { new FabricaEntregaEstandar(), new FabricaEntregaExpres() }),
            pasarela,
            new UsuarioActualFalso(RolUsuario.Ventas));
        return await facade.ConfirmarAsync(new ConfirmarPedidoSolicitud(pedidoId, clave));
    }

    private sealed class PasarelaConcurrente : IPasarelaPago
    {
        private readonly TaskCompletionSource _ambasSolicitudes = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _solicitudes;

        public async Task<ResultadoCobro> CobrarAsync(decimal monto, string claveIdempotencia, CancellationToken ct = default)
        {
            if (Interlocked.Increment(ref _solicitudes) == 2) _ambasSolicitudes.TrySetResult();
            await _ambasSolicitudes.Task.WaitAsync(ct);
            if (claveIdempotencia == "B") await Task.Delay(100, ct);
            return ResultadoCobro.Exito($"REF-{claveIdempotencia}");
        }
    }

    [Fact]
    public async Task ClienteRepositorio_AgregarYListar_FuncionaTrasCommit()
    {
        await using var db = await CrearYSembrarAsync();
        var repo = new ClienteRepositorioEf(db);
        var uow = new UnitOfWorkEf(db);

        repo.Agregar(new Cliente("Carlos Ruiz", CategoriaCliente.Normal));
        await uow.CommitAsync();

        var creado = Assert.Single(await repo.ListarAsync(), c => c.Nombre == "Carlos Ruiz");
        Assert.Equal(CategoriaCliente.Normal, creado.Categoria);
        Assert.NotNull(await repo.ObtenerPorIdAsync(creado.Id));
    }

    [Fact]
    public async Task OutboxYUnitOfWork_GuardanElEventoConSuTipoYDatos()
    {
        await using var db = await CrearYSembrarAsync();
        var evento = new PedidoPagado(Guid.NewGuid(), Guid.NewGuid(), new Dinero(25m), "REF-OUTBOX", DateTime.UtcNow);

        new OutboxEf(db).Registrar(evento);
        await new UnitOfWorkEf(db).CommitAsync();

        var guardado = await db.EventosOutbox.SingleAsync();
        Assert.Equal(nameof(PedidoPagado), guardado.Tipo);
        Assert.Null(guardado.ProcesadoUtc);
        Assert.Contains(evento.PedidoId.ToString(), guardado.DatosJson);
    }

    [Fact]
    public async Task PagoRepositorio_EncuentraElPagoPorClaveDeIdempotencia()
    {
        await using var db = await CrearYSembrarAsync();
        var cliente = new Cliente("Ana", CategoriaCliente.Vip);
        var producto = await db.Productos.FirstAsync(p => p.Sku == "P01");
        var pedido = new PedidoBuilder().ConCliente(cliente).AgregarLinea(new ProductoSimple(producto), 1).Construir();
        db.Clientes.Add(cliente);
        db.Pedidos.Add(pedido);
        db.Pagos.Add(new Pago(pedido.Id, new Dinero(20m), "REF-X", "CLAVE-REPO-1", DateTime.UtcNow));
        await db.SaveChangesAsync();

        var pago = await new PagoRepositorioEf(db).ObtenerPorClaveAsync("CLAVE-REPO-1");

        Assert.NotNull(pago);
        Assert.Equal("REF-X", pago!.Referencia);
    }

    [Fact]
    public async Task PedidoRepositorio_CargaDetallesComponentesYHistorial()
    {
        await using var db = await CrearYSembrarAsync();
        var cliente = new Cliente("Ana", CategoriaCliente.Vip);
        var pedido = new PedidoBuilder().ConCliente(cliente).AgregarLinea(P01(), 1).Construir();
        db.Clientes.Add(cliente);
        pedido.Validar();
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var releido = await new PedidoRepositorioEf(db).ObtenerPorIdAsync(pedido.Id);

        Assert.NotNull(releido);
        Assert.Single(releido!.Detalles);
        Assert.Single(releido.Detalles[0].Componentes);
        Assert.Single(releido.Historial);
    }

    [Fact]
    public async Task PedidoRepositorio_Agregar_PersisteYSePuedeReleer()
    {
        await using var db = await CrearYSembrarAsync();
        var cliente = new Cliente("Ana", CategoriaCliente.Vip);
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        var producto = await db.Productos.FirstAsync(p => p.Sku == "P01");
        var pedido = new PedidoBuilder().ConCliente(cliente).AgregarLinea(new ProductoSimple(producto), 2).Construir();

        var repositorio = new PedidoRepositorioEf(db);
        repositorio.Agregar(pedido);
        await new UnitOfWorkEf(db).CommitAsync();

        var releido = await repositorio.ObtenerPorIdAsync(pedido.Id);
        Assert.NotNull(releido);
        Assert.Single(releido!.Detalles);
        Assert.Equal(2, releido.Detalles[0].Cantidad);
    }

    [Fact]
    public async Task PedidoRepositorio_ListarResumen_DevuelveEstadoClienteYTotal()
    {
        await using var db = await CrearYSembrarAsync();
        var cliente = new Cliente("Ana", CategoriaCliente.Vip);
        var pedido = new PedidoBuilder().ConCliente(cliente).AgregarLinea(P01(), 2).Construir();
        db.Pedidos.Add(pedido);
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resumen = await new PedidoRepositorioEf(db).ListarResumenAsync();

        var fila = Assert.Single(resumen);
        Assert.Equal(cliente.Nombre, fila.Cliente);
        Assert.Equal(EstadoPedido.Borrador, fila.Estado);
        Assert.Equal(40m, fila.Total);
    }

    public void Dispose()
    {
        if (File.Exists(_rutaBd))
            File.Delete(_rutaBd);
    }
}
