namespace LogiMarket.Application.Abstractions.Repositories;

// Confirma de una sola vez TODO lo que se agregó o modificó: o se guarda todo, o nada.
public interface IUnitOfWork
{
    Task CommitAsync();
}