namespace LogiMarket.Application.Abstractions.Services;

// Auditoría de NEGOCIO (qué pasó: pagos, despachos, devoluciones).
// Es DISTINTA de la auditoría de SEGURIDAD que ya usa el Proxy (intentos de acceso denegados).
public interface IBitacoraAuditoria
{
    Task RegistrarAsync(string tipoEvento, string detalle, CancellationToken ct = default);

    Task RegistrarAsync(string tipoEvento, string detalle, DateTime ocurrioUtc, CancellationToken ct = default) =>
        RegistrarAsync(tipoEvento, detalle, ct);

    Task<IReadOnlyList<RegistroAuditoriaDto>> ListarRecientesAsync(int limite = 100, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<RegistroAuditoriaDto>>(Array.Empty<RegistroAuditoriaDto>());
}
