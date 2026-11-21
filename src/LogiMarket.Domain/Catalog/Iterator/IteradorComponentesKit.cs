using LogiMarket.Domain.Catalog.Composite;

namespace LogiMarket.Domain.Catalog.Iterator;

// "internal": solo se ve dentro de Domain. Desde fuera se obtiene con kit.CrearIterador().
internal sealed class IteradorComponentesKit : IIteradorComponentes
{
    // Pila de lo que falta por recorrer: cada entrada es un elemento y la cantidad
    // TOTAL acumulada que se necesita de él (ya multiplicada por los niveles superiores).
    private readonly Stack<(IElementoCatalogo Elemento, int Cantidad)> _pendientes = new();

    public IteradorComponentesKit(KitCompuesto kit)
    {
        EmpujarHijos(kit, 1);
    }

    public bool TieneSiguiente()
    {
        DescenderHastaUnaHoja();
        return _pendientes.Count > 0;
    }

    public ComponenteFisico Siguiente()
    {
        if (!TieneSiguiente())
            throw new InvalidOperationException("No hay más componentes que recorrer.");

        var (elemento, cantidad) = _pendientes.Pop();
        return new ComponenteFisico(elemento.Sku, cantidad);
    }

    // Mientras lo de arriba de la pila sea un kit, lo "abre": lo saca y
    // pone en su lugar sus hijos. Se detiene cuando arriba hay un producto real.
    private void DescenderHastaUnaHoja()
    {
        while (_pendientes.Count > 0 && _pendientes.Peek().Elemento is KitCompuesto kit)
        {
            var (_, cantidad) = _pendientes.Pop();
            EmpujarHijos(kit, cantidad);
        }
    }

    private void EmpujarHijos(KitCompuesto kit, int cantidadDelKit)
    {
        // Se apilan al revés: la pila saca primero el último que entró,
        // así los componentes salen en el orden original del kit.
        for (var i = kit.Items.Count - 1; i >= 0; i--)
        {
            var item = kit.Items[i];
            _pendientes.Push((item.Elemento, cantidadDelKit * item.Cantidad));
        }
    }
}