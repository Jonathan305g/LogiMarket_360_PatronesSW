using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.ValueObjects;
using LogiMarket.Infrastructure.Outbox;
using LogiMarket.Infrastructure.Persistence;
using LogiMarket.Infrastructure.Persistence.Auditoria;
using LogiMarket.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Tests.Infrastructure;

public sealed class AuditoriaYOutboxTests : IDisposable
{
    private readonly string _rutaBd = Path.Combine(Path.GetTempPath(), $"logimarket_auditoria_{Guid.NewGuid():N}.db");

    private LogiMarketDbContext CrearContexto() => new(new DbContextOptionsBuilder<LogiMarketDbContext>()
        .UseSqlite($"Data Source={_rutaBd};Pooling=False")
        .Options);

    private async Task<LogiMarketDbContext> CrearYMigrarAsync()
    {
        var db = CrearContexto();
        await db.Database.MigrateAsync();
        return db;
    }

    [Fact]
    public async Task BitacoraAuditoria_RegistrarAsync_GuardaLaFilaDeInmediato()
    {
        await using var db = await CrearYMigrarAsync();
        var bitacora = new BitacoraAuditoriaEf(db);

        await bitacora.RegistrarAsync("CobroPasarela", "INICIO correlacion=abc monto=59.00");

        var fila = await db.Auditorias.SingleAsync();
        Assert.Equal("CobroPasarela", fila.TipoEvento);
        Assert.Single(await bitacora.ListarRecientesAsync());
    }

    [Fact]
    public async Task AuditoriaSeguridad_RegistrarAsync_GuardaEnTablaSeparada()
    {
        await using var db = await CrearYMigrarAsync();
        var auditoria = new AuditoriaSeguridadEf(db);

        await auditoria.RegistrarAsync("ventas1", "Ventas", "Cobrar");

        var fila = await db.AuditoriasSeguridad.SingleAsync();
        Assert.Equal("Ventas", fila.Rol);
        Assert.Empty(db.Auditorias);
        Assert.Single(await auditoria.ListarRecientesAsync());
    }

    [Fact]
    public async Task Procesador_SiFallaUnEvento_LosAnterioresQuedanMarcadosYSeReintentaElPendiente()
    {
        await using var db = await CrearYMigrarAsync();
        var ahora = DateTime.UtcNow;
        var outbox = new OutboxEf(db);
        outbox.Registrar(new PedidoPagado(Guid.NewGuid(), Guid.NewGuid(), new Dinero(25m), "REF-1", ahora));
        outbox.Registrar(new DespachoRegistrado(Guid.NewGuid(), "GUIA-1", ahora.AddTicks(1)));
        await new UnitOfWorkEf(db).CommitAsync();

        var recibidos = new List<IEventoDominio>();
        var llamadas = 0;
        var procesadorQueFalla = new ProcesadorOutbox(db, evento =>
        {
            llamadas++;
            if (llamadas == 2)
                throw new InvalidOperationException("Fallo simulado del notificador.");
            recibidos.Add(evento);
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => procesadorQueFalla.ProcesarPendientesAsync());

        Assert.Single(recibidos);
        Assert.Equal(1, await db.EventosOutbox.CountAsync(e => e.ProcesadoUtc == null));

        var procesadorRecuperado = new ProcesadorOutbox(db, evento => recibidos.Add(evento));
        await procesadorRecuperado.ProcesarPendientesAsync();

        Assert.Equal(2, recibidos.Count);
        Assert.Empty(await db.EventosOutbox.Where(e => e.ProcesadoUtc == null).ToListAsync());
    }

    public void Dispose()
    {
        if (File.Exists(_rutaBd))
            File.Delete(_rutaBd);
    }
}
