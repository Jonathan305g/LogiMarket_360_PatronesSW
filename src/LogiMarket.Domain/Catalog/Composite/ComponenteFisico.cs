namespace LogiMarket.Domain.Catalog.Composite;

// Un SKU final (un producto real de bodega) con la cantidad requerida. 
// "record" da igualdad por valor e inmutabilidad sin escribir código extra.
public record ComponenteFisico(string Sku, int Cantidad);