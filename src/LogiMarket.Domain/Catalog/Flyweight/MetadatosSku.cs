using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Catalog.Flyweight;

// Estado INTRÍNSECO: lo que es igual para todas las filas del mismo SKU.
// Inmutable, para poder compartirlo con seguridad.
public sealed class MetadatosSku
{
    public string Sku { get; }
    public string Nombre { get; }
    public string Categoria { get; }
    public string Descripcion { get; }

    // "internal": solo la fábrica de flyweights puede crearlos.
    internal MetadatosSku(string sku, string nombre, string categoria, string descripcion)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("El SKU es obligatorio.");

        Sku = sku;
        Nombre = nombre;
        Categoria = categoria;
        Descripcion = descripcion;
    }
} 