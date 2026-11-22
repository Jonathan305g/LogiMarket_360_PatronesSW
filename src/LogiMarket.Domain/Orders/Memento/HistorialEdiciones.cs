using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.Domain.Orders.Memento;

// Guarda las fotografías y decide cuándo volver a ellas. No sabe qué contienen.
public class HistorialEdiciones
{
    public const int Capacidad = 5;

    private readonly List<PedidoMemento> _deshacer = new();   // el último de la lista es el más reciente
    private readonly Stack<PedidoMemento> _rehacer = new();

    public bool PuedeDeshacer => _deshacer.Count > 0;
    public bool PuedeRehacer => _rehacer.Count > 0;

    // Se llama ANTES de cada edición: guarda cómo está el pedido ahora.
    public void Guardar(Pedido pedido)
    {
        _deshacer.Add(pedido.CrearMemento()); // lanza si el pedido ya no es BORRADOR

        // Máximo 5 estados: al pasarse, se descarta el más antiguo.
        if (_deshacer.Count > Capacidad)
            _deshacer.RemoveAt(0);

        // Una edición nueva invalida lo que se podía rehacer.
        _rehacer.Clear();
    }

    public void Deshacer(Pedido pedido)
    {
        if (!PuedeDeshacer)
            throw new DomainException("No hay ediciones para deshacer.");

        // Primero lo que puede fallar (pedido no borrador); recién después se tocan las pilas.
        var actual = pedido.CrearMemento();
        pedido.Restaurar(_deshacer[^1]);

        _deshacer.RemoveAt(_deshacer.Count - 1);
        _rehacer.Push(actual); // el estado de antes de deshacer se puede rehacer
    }

    public void Rehacer(Pedido pedido)
    {
        if (!PuedeRehacer)
            throw new DomainException("No hay ediciones para rehacer.");

        var actual = pedido.CrearMemento();
        pedido.Restaurar(_rehacer.Peek());

        _rehacer.Pop();
        _deshacer.Add(actual);
    }

    // Para llamar al confirmar el pedido: se vacía todo.
    public void Limpiar()
    {
        _deshacer.Clear();
        _rehacer.Clear();
    }
}