namespace LogiMarket.Infrastructure.Persistence.Models;

// Auditoría de NEGOCIO (qué pasó: pagos, despachos, devoluciones).
public class AuditoriaRegistro
{
    public Guid Id { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public DateTime OcurrioUtc { get; set; }
} 