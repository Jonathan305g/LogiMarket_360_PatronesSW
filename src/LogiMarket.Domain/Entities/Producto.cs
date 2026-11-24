using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Domain.Entities;

public class Producto
{
    // Identidad: lo que distingue a un producto de otro, aunque tengan el mismo nombre.
    public Guid Id { get; private set; }

    // "private set": solo se pueden asignar desde DENTRO de esta clase.
    // Así nadie escribe producto.Stock = -5 desde afuera y se salta las reglas.
    public string Sku { get; private set; }
    public string Nombre { get; private set; }
    public Dinero Precio { get; private set; }
    public decimal PesoKg { get; private set; }
    public int Stock { get; private set; }

    private Producto() { Sku = string.Empty; Nombre = string.Empty; } // EF Core

    public Producto(string sku, string nombre, Dinero precio, decimal pesoKg, int stock)
    {
        // Se valida TODO antes de asignar nada. Si algo falla, el objeto nunca llega
        // a existir: un Producto inválido es imposible.
        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("El SKU es obligatorio.");

        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre es obligatorio.");

        if (precio.Valor <= 0)
            throw new DomainException("El precio debe ser mayor que cero.");

        if (pesoKg < 0)
            throw new DomainException("El peso no puede ser negativo.");

        if (stock < 0)
            throw new DomainException("El stock no puede ser negativo.");

        Id = Guid.NewGuid();
        Sku = sku.Trim();
        Nombre = nombre.Trim();
        Precio = precio;
        PesoKg = pesoKg;
        Stock = stock;
    }

    public void DescontarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad a descontar debe ser mayor que cero.");

        // Esta comprobación va ANTES de restar. Si falla, el stock queda intacto
        // (sin efectos colaterales, como exige la guía en los rechazos).
        if (cantidad > Stock)
            throw new DomainException($"Stock insuficiente para {Sku}: disponible {Stock}, solicitado {cantidad}.");

        Stock -= cantidad;
    }

    public void ReintegrarStock(int cantidad)
    {
        if (cantidad <= 0)
            throw new DomainException("La cantidad a reintegrar debe ser mayor que cero.");

        Stock += cantidad;
    }

    public void CambiarPrecio(Dinero nuevoPrecio)
    {
        if (nuevoPrecio.Valor <= 0)
            throw new DomainException("El precio debe ser mayor que cero.");

        Precio = nuevoPrecio;
    }
}
