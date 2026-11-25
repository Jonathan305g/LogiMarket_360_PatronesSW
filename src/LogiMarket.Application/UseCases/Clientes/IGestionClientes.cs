using LogiMarket.Application.Validation;
using LogiMarket.Domain.Enums;

namespace LogiMarket.Application.UseCases.Clientes;

public interface IGestionClientes
{
    Task<ResultadoOperacion> GuardarAsync(Guid? id, string nombre, CategoriaCliente categoria, CancellationToken ct = default);
}
