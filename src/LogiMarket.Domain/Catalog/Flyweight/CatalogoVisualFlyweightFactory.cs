using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Catalog.Flyweight;

public class CatalogoVisualFlyweightFactory
{
    private readonly Dictionary<string, MetadatosSku> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _bloqueo = new();

    // Cuántos objetos de metadatos existen realmente (el "recuento de objetos").
    public int InstanciasCreadas
    {
        get { lock (_bloqueo) return _cache.Count; }
    }

    // Si ya existe un flyweight para ese SKU lo devuelve; si no, lo crea y lo guarda.
    // Si ya existía, los demás datos recibidos se ignoran (gana el primero registrado).
    public MetadatosSku Obtener(string sku, string nombre, string categoria, string descripcion)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("El SKU es obligatorio.");

        var clave = sku.Trim();

        lock (_bloqueo)
        {
            if (!_cache.TryGetValue(clave, out var metadatos))
            {
                metadatos = new MetadatosSku(clave, nombre, categoria, descripcion);
                _cache[clave] = metadatos;
            }

            return metadatos;
        }
    }
}