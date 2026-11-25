namespace LogiMarket.Infrastructure.Persistence.Models;

// Tabla "KitComponentes": qué contiene un kit. Si EsKit es true, ComponenteSku es
// OTRO kit (anidado); si es false, es un producto final.
public class KitComponenteRegistro
{
    public int Id { get; set; }
    public string KitSku { get; set; } = string.Empty;
    public string ComponenteSku { get; set; } = string.Empty;
    public bool EsKit { get; set; }
    public string? ProductoSku { get; set; }
    public string? KitReferenciaSku { get; set; }
    public int Cantidad { get; set; }
}
