using LogiMarket.Application.Comprobantes.Bridge;

namespace LogiMarket.Infrastructure.Comprobantes;

public sealed class FabricaSalidaArchivo : IFabricaSalidaArchivo
{
    public ISalidaDocumento Crear(string directorio) => new ArchivoTXT(directorio);
}
