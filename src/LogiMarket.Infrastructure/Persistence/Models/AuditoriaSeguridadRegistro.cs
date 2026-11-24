namespace LogiMarket.Infrastructure.Persistence.Models;

// Auditoría de SEGURIDAD (intentos denegados por el Proxy): tabla SEPARADA de la anterior.
public class AuditoriaSeguridadRegistro
{
    public Guid Id { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Operacion { get; set; } = string.Empty;
    public DateTime OcurrioUtc { get; set; }
}