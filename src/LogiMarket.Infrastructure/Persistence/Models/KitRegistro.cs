namespace LogiMarket.Infrastructure.Persistence.Models;

// Tabla "Kits": SKU y nombre del kit. Es persistencia pura, no el Composite de Domain.
public class KitRegistro
{
    public string Sku { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public List<KitComponenteRegistro> Componentes { get; set; } = new();
}