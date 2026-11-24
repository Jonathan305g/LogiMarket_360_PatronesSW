using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Catalog.Visitor;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.ValueObjects;
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
        .UseSqlite($"Data Source={_rutaBd};Pooling=False")
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

    public void Dispose()
    {
        if (File.Exists(_rutaBd))
            File.Delete(_rutaBd);
    }
}
