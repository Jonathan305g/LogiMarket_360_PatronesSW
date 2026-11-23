using LogiMarket.Domain.Entities;

namespace LogiMarket.Application.Abstractions.Repositories;

public interface IDespachoRepository
{
    void Agregar(Despacho despacho);
}