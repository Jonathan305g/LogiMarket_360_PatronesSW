using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Entities;

public class Cliente
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; }
    public CategoriaCliente Categoria { get; private set; }

    // El Chain of Responsibility de más adelante pregunta "¿cliente activo?".
    // Por eso guardamos el estado en vez de borrar clientes.
    public bool Activo { get; private set; }

    private Cliente() { Nombre = string.Empty; } // EF Core

    public Cliente(string nombre, CategoriaCliente categoria)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre del cliente es obligatorio.");

        Id = Guid.NewGuid();
        Nombre = nombre.Trim();
        Categoria = categoria;
        Activo = true; // todo cliente nuevo nace activo
    }

    public void Renombrar(string nuevoNombre)
    {
        if (string.IsNullOrWhiteSpace(nuevoNombre))
            throw new DomainException("El nombre del cliente es obligatorio.");

        Nombre = nuevoNombre.Trim();
    }

    public void CambiarCategoria(CategoriaCliente nueva) => Categoria = nueva;

    public void Desactivar() => Activo = false;

    public void Activar() => Activo = true;
}
