using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Auditoria;

public sealed class BitacoraAuditoriaEf(LogiMarketDbContext db) : IBitacoraAuditoria
{
    public Task RegistrarAsync(string tipoEvento, string detalle, CancellationToken ct = default) =>
        RegistrarAsync(tipoEvento, detalle, DateTime.UtcNow, ct);

    public async Task RegistrarAsync(string tipoEvento, string detalle, DateTime ocurrioUtc, CancellationToken ct = default)
    {
        db.Auditorias.Add(new AuditoriaRegistro
        {
            Id = Guid.NewGuid(),
            TipoEvento = tipoEvento,
            Detalle = detalle,
            OcurrioUtc = ocurrioUtc
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RegistroAuditoriaDto>> ListarRecientesAsync(int limite = 100, CancellationToken ct = default) =>
        await db.Auditorias.AsNoTracking()
            .OrderByDescending(r => r.OcurrioUtc)
            .Take(Math.Max(0, limite))
            .Select(r => new RegistroAuditoriaDto(r.TipoEvento, r.Detalle, r.OcurrioUtc))
            .ToListAsync(ct);
}
