using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Catalog.Flyweight;

// Una fila de la vista. Lo propio de cada fila (estado EXTRÍNSECO) va aquí;
// lo común se referencia, no se copia.
public record FilaCatalogoVisual(MetadatosSku Metadatos, int Stock, int Cantidad, Dinero PrecioVenta);