using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Infrastructure.Payments.Legacy;

namespace LogiMarket.Infrastructure.Payments;

/// <summary>Permite alternar entre dos proveedores simulados dentro de la sesión actual.</summary>
public sealed class SelectorPasarelaPago(IPasarelaPago legado, IPasarelaPago directo) : IPasarelaPago, ISelectorPasarelaPago
{
    private int _proveedor;

    public ProveedorPagoSimulado ProveedorActual => (ProveedorPagoSimulado)Volatile.Read(ref _proveedor);

    public void Seleccionar(ProveedorPagoSimulado proveedor) =>
        Interlocked.Exchange(ref _proveedor, (int)proveedor);

    public Task<ResultadoCobro> CobrarAsync(
        decimal monto, string claveIdempotencia, CancellationToken ct = default) =>
        (ProveedorActual == ProveedorPagoSimulado.Legado ? legado : directo)
        .CobrarAsync(monto, claveIdempotencia, ct);
}
