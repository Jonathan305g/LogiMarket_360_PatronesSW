using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Domain.Enums;
using LogiMarket.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace LogiMarket.Infrastructure.Persistence.Auditoria;

public sealed class AuditoriaSeguridadEf(LogiMarketDbContext db) : IAuditoriaSeguridad
{
    public void RegistrarIntentoNoAutorizado(string usuario, RolUsuario rol, string operacion) =>
        RegistrarAsync(usuario, rol.ToString(), operacion).GetAwaiter().GetResult();

    public async Task RegistrarAsync(string usuario, string rol, string operacion, CancellationToken ct = default)
    {
        db.AuditoriasSeguridad.Add(new AuditoriaSeguridadRegistro
        {
            Id = Guid.NewGuid(),
            Usuario = usuario,
            Rol = rol,
            Operacion = operacion,
            OcurrioUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RegistroAuditoriaSeguridadDto>> ListarRecientesAsync(int limite = 100, CancellationToken ct = default) =>
        await db.AuditoriasSeguridad.AsNoTracking()
            .OrderByDescending(r => r.OcurrioUtc)
            .Take(Math.Max(0, limite))
            .Select(r => new RegistroAuditoriaSeguridadDto(r.Usuario, r.Rol, r.Operacion, r.OcurrioUtc))
            .ToListAsync(ct);
}
