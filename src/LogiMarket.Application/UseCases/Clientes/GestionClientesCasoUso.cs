using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Validation;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Application.UseCases.Clientes;

public sealed class GestionClientesCasoUso(
    IClienteRepository clientes,
    IUnitOfWork unidadDeTrabajo,
    IUsuarioActual usuario) : IGestionClientes
{
    public async Task<ResultadoOperacion> GuardarAsync(
        Guid? id, string nombre, CategoriaCliente categoria, CancellationToken ct = default)
    {
        if (usuario.Rol is not (RolUsuario.Admin or RolUsuario.Ventas))
            return ResultadoOperacion.Fallo("NO_AUTORIZADO", $"El rol {usuario.Rol} no puede gestionar clientes.");

        try
        {
            if (id is null)
            {
                clientes.Agregar(new Cliente(nombre, categoria));
            }
            else
            {
                var cliente = await clientes.ObtenerPorIdAsync(id.Value);
                if (cliente is null) return ResultadoOperacion.Fallo("CLIENTE_NO_ENCONTRADO", "El cliente ya no existe.");
                cliente.Renombrar(nombre);
                cliente.CambiarCategoria(categoria);
            }

            await unidadDeTrabajo.CommitAsync();
            return ResultadoOperacion.Ok();
        }
        catch (DomainException ex)
        {
            return ResultadoOperacion.Fallo("CLIENTE_INVALIDO", ex.Message);
        }
    }
}
