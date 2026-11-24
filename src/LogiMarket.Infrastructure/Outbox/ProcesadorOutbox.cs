using System.Text.Json;
using LogiMarket.Domain.Events;
using LogiMarket.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Outbox;

public sealed class ProcesadorOutbox
{
    private static readonly IReadOnlyDictionary<string, Type> TiposEventos = typeof(IEventoDominio).Assembly
        .GetTypes()
        .Where(t => typeof(IEventoDominio).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
        .ToDictionary(t => t.Name, StringComparer.Ordinal);

    private readonly LogiMarketDbContext _db;
    private readonly Func<IEventoDominio, CancellationToken, Task> _publicar;

    public ProcesadorOutbox(LogiMarketDbContext db, Func<IEventoDominio, CancellationToken, Task> publicar)
    {
        _db = db;
        _publicar = publicar;
    }

    // Adaptador para publicadores síncronos y pruebas sencillas.
    public ProcesadorOutbox(LogiMarketDbContext db, Action<IEventoDominio> publicar)
        : this(db, (evento, _) =>
        {
            publicar(evento);
            return Task.CompletedTask;
        })
    {
    }

    public async Task ProcesarPendientesAsync(CancellationToken ct = default)
    {
        var pendientes = await _db.EventosOutbox
            .Where(e => e.ProcesadoUtc == null)
            .OrderBy(e => e.OcurrioUtc)
            .ToListAsync(ct);

        foreach (var registro in pendientes)
        {
            if (!TiposEventos.TryGetValue(registro.Tipo, out var tipo))
                throw new InvalidOperationException($"Tipo de evento desconocido en el Outbox: '{registro.Tipo}'.");

            var evento = JsonSerializer.Deserialize(registro.DatosJson, tipo) as IEventoDominio
                ?? throw new InvalidOperationException($"No se pudo reconstruir el evento '{registro.Tipo}' del Outbox.");

            await _publicar(evento, ct);
            registro.ProcesadoUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }
}
