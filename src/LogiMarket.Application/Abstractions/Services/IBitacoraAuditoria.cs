namespace LogiMarket.Application.Abstractions.Services;

// Auditoría de NEGOCIO (qué pasó: pagos, despachos, devoluciones).
// Es DISTINTA de la auditoría de SEGURIDAD que ya usa el Proxy (intentos de acceso denegados).
public interface IBitacoraAuditoria
{
    void Registrar(string tipoEvento, string detalle, DateTime ocurrioUtc);
}