namespace LogiMarket.Application.Abstractions.Services;

public enum ProveedorPagoSimulado
{
    Legado,
    Directo
}

public interface ISelectorPasarelaPago
{
    ProveedorPagoSimulado ProveedorActual { get; }
    void Seleccionar(ProveedorPagoSimulado proveedor);
}
