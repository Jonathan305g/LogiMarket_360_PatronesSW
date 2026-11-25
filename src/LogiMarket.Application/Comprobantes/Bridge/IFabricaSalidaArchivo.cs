namespace LogiMarket.Application.Comprobantes.Bridge;

public interface IFabricaSalidaArchivo
{
    ISalidaDocumento Crear(string directorio);
}
