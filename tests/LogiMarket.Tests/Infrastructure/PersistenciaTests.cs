using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Orders.Builder;
using LogiMarket.Domain.ValueObjects;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.Infrastructure.Persistence.Seed;
using LogiMarket.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using static LogiMarket.Tests.Fakes.CatalogoDePrueba;

namespace LogiMarket.Tests.Infrastructure;

public class PersistenciaTests : IDisposable
{
    private readonly string _rutaBd;

    public PersistenciaTests()
    {
        _rutaBd = Path.Combine(Path.GetTempPath(), $"logimarket_test_{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        if (File.Exists(_rutaBd)) File.Delete(_rutaBd);
    }

    private LogiMarketDbContext CrearContexto() =>
        new(new DbContextOptionsBuilder<LogiMarketDbContext>().UseSqlite($"Data Source={_rutaBd};Pooling=False").Options);

    [Fact]
    public async Task Migrar_DesdeArchivoInexistente_CreaLasTablas()   // P01
    {
        Assert.False(File.Exists(_rutaBd));

        await using var db = CrearContexto();
        await db.Database.MigrateAsync();

        Assert.True(File.Exists(_rutaBd));
        Assert.Empty(await db.Productos.ToListAsync());
    }

    [Fact]
    public async Task Sembrar_DosVeces_NoDuplicaProductosKitsNiUsuarios()   // P01
    {
        await using var db = CrearContexto();
        await db.Database.MigrateAsync();

        await DatosSemilla.SembrarAsync(db);
        await DatosSemilla.SembrarAsync(db); // relanzar el seed

        Assert.Equal(5, await db.Productos.CountAsync());
        Assert.Equal(2, await db.Kits.CountAsync());
        Assert.Equal(4, await db.Usuarios.CountAsync());
    }

    [Fact]
    public async Task Sembrar_ActualizaElHashLegadoDeLaCuentaAdminSinBorrarLaBase()
    {
        await using var db = CrearContexto();
        await db.Database.MigrateAsync();
        var hashAnterior = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("admin")));
        db.Usuarios.Add(new Usuario("admin", hashAnterior, RolUsuario.Admin));
        await db.SaveChangesAsync();

        await DatosSemilla.SembrarAsync(db);

        var admin = await db.Usuarios.SingleAsync(u => u.NombreUsuario == "admin");
        Assert.True(new HasherContrasenaPbkdf2().Verificar("Admin123!", admin.HashContrasena));
    }

    [Fact]
    public async Task ProductoConSkuDuplicado_LoRechazaElIndiceUnico()
    {
        await using var db = CrearContexto();
        await db.Database.MigrateAsync();
        db.Productos.Add(new Producto("P01", "Mouse", new Dinero(20m), 0.2m, 8));
        await db.SaveChangesAsync();

        db.Productos.Add(new Producto("P01", "Otro Mouse", new Dinero(99m), 0.2m, 1));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task PagoConClaveRepetida_LoRechazaElIndiceUnico()   // base del control de duplicados (P07)
    {
        await using var db = CrearContexto();
        await db.Database.MigrateAsync();
        var pedido1 = new PedidoBuilder().ConCliente(new Cliente("Ana", CategoriaCliente.Vip)).AgregarLinea(P01(), 1).Construir();
        var pedido2 = new PedidoBuilder().ConCliente(new Cliente("Luis", CategoriaCliente.Normal)).AgregarLinea(P02(), 1).Construir();
        db.Pedidos.AddRange(pedido1, pedido2);
        db.Pagos.Add(new Pago(pedido1.Id, new Dinero(20m), "REF-1", "CLAVE-X", DateTime.UtcNow));
        await db.SaveChangesAsync();

        db.Pagos.Add(new Pago(pedido2.Id, new Dinero(30m), "REF-2", "CLAVE-X", DateTime.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Pedido_GuardadoYReleido_RestauraEstadoDetallesYHistorial()   // P10, base de persistencia del State
    {
        Guid pedidoId;

        // --- Sesión 1: se crea, se avanza por el ciclo y se cierra el contexto ---
        await using (var db1 = CrearContexto())
        {
            await db1.Database.MigrateAsync();

            var cliente = new Cliente("Ana", CategoriaCliente.Vip);
            db1.Clientes.Add(cliente);

            var pedido = new PedidoBuilder()
                .ConCliente(cliente).ConEntrega(TipoEntrega.Estandar)
                .AgregarLinea(CrearKitTeletrabajo(), 1)
                .Construir();

            pedido.Validar();
            pedido.Valorar(new Dinero(6m), new Dinero(5m));
            pedido.Pagar();

            db1.Pedidos.Add(pedido);
            await db1.SaveChangesAsync();
            pedidoId = pedido.Id;
        } // el contexto se cierra aquí: simula "cerrar la aplicación"

        // --- Sesión 2: un contexto NUEVO, como si se hubiera reabierto la aplicación ---
        await using var db2 = CrearContexto();
        var releido = await db2.Pedidos
            .Include(p => p.Detalles).ThenInclude(d => d.Componentes)
            .Include(p => p.Historial)
            .FirstAsync(p => p.Id == pedidoId);

        Assert.Equal(EstadoPedido.Pagado, releido.Estado);       // el State se reconstruyó bien
        Assert.Equal(59m, releido.Total.Valor);                   // 60 - 6 + 5
        Assert.Equal(54m, releido.SubtotalConDescuento.Valor);
        var linea = Assert.Single(releido.Detalles);
        Assert.Equal("KIT_TELETRABAJO", linea.Sku);
        Assert.Equal(3, linea.Componentes.Count);                 // P01, P02, P03 sobrevivieron
        Assert.Equal(2, releido.Historial.Count);                 // Borrador->Validado->Pagado
    }
}
