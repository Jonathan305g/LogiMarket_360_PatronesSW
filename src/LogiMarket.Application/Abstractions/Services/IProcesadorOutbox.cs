namespace LogiMarket.Application.Abstractions.Services;

public interface IProcesadorOutbox
{
    Task<int> ContarPendientesAsync(CancellationToken ct = default);
    Task ProcesarPendientesAsync(CancellationToken ct = default);
    void FallarSiguientePublicacion();
}
