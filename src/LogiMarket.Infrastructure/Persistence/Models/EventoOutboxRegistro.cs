namespace LogiMarket.Infrastructure.Persistence.Models;

// Tabla "EventosOutbox": un evento de dominio pendiente de procesar (serializado).
public class EventoOutboxRegistro
{
    public Guid Id { get; set; }
    public string Tipo { get; set; } = string.Empty;       // "PedidoPagado", "DespachoRegistrado"...
    public string DatosJson { get; set; } = string.Empty;
    public DateTime OcurrioUtc { get; set; }
    public DateTime? ProcesadoUtc { get; set; }             // null = todavía pendiente
} 