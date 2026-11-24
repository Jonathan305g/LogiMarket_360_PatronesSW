namespace LogiMarket.Application.Abstractions.Services;

public sealed record RegistroAuditoriaDto(string TipoEvento, string Detalle, DateTime OcurrioUtc);

public sealed record RegistroAuditoriaSeguridadDto(string Usuario, string Rol, string Operacion, DateTime OcurrioUtc);
