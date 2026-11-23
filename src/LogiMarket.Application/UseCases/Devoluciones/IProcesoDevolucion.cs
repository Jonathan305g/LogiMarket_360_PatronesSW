namespace LogiMarket.Application.UseCases.Devoluciones;

public interface IProcesoDevolucion
{
    Task<ResultadoDevolucionAplicacion> SolicitarAsync(SolicitudDevolucionDto solicitud, CancellationToken ct = default); 
}