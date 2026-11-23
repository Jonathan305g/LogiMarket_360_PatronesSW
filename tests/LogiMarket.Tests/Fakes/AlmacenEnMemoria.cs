using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Tests.Fakes;

public sealed class AlmacenEnMemoria : IClienteRepository, IPedidoRepository, IProductoRepository,
    IPagoRepository, IMovimientoInventarioRepository, IOutbox, IUnitOfWork
{
    private readonly Dictionary<Guid, Cliente> _clientes = new();
    private readonly Dictionary<Guid, Pedido> _pedidos = new();
    private readonly Dictionary<string, Producto> _productos = new(StringComparer.OrdinalIgnoreCase);

    // Cambios todavía no confirmados.
    private readonly List<Pago> _pagosPendientes = new();
    private readonly List<MovimientoInventario> _movimientosPendientes = new();
    private readonly List<IEventoDominio> _eventosPendientes = new();

    // Lo que "quedó guardado en la base" tras un commit.
    public List<Pago> Pagos { get; } = new();
    public List<MovimientoInventario> Movimientos { get; } = new();
    public List<IEventoDominio> EventosOutbox { get; } = new();
    public int Commits { get; private set; }

    // Si se asigna, el siguiente commit falla con esta excepción y NO guarda nada.
    public Exception? FalloEnCommit { get; set; }

    // ----- Preparación de datos de prueba -----
    public Cliente Sembrar(Cliente cliente) { _clientes[cliente.Id] = cliente; return cliente; }
    public Pedido Sembrar(Pedido pedido) { _pedidos[pedido.Id] = pedido; return pedido; }
    public Producto Sembrar(Producto producto) { _productos[producto.Sku] = producto; return producto; }
    public Producto ProductoPorSku(string sku) => _productos[sku];

    // Los 5 productos oficiales con su stock inicial.
    public static AlmacenEnMemoria ConCatalogoOficial()
    {
        var almacen = new AlmacenEnMemoria();
        almacen.Sembrar(new Producto("P01", "Mouse USB", new Dinero(20m), 0.20m, 8));
        almacen.Sembrar(new Producto("P02", "Teclado", new Dinero(30m), 0.70m, 5));
        almacen.Sembrar(new Producto("P03", "Memoria USB", new Dinero(10m), 0.10m, 20));
        almacen.Sembrar(new Producto("P04", "Webcam", new Dinero(40m), 0.35m, 4));
        almacen.Sembrar(new Producto("P05", "Alfombrilla", new Dinero(5m), 0.10m, 30));
        return almacen;
    }

    // ----- Lecturas (explícitas porque comparten nombre) -----
    Task<Cliente?> IClienteRepository.ObtenerPorIdAsync(Guid id) =>
        Task.FromResult(_clientes.GetValueOrDefault(id));

    Task<Pedido?> IPedidoRepository.ObtenerPorIdAsync(Guid id) =>
        Task.FromResult(_pedidos.GetValueOrDefault(id));

    public Task<IReadOnlyList<Producto>> ObtenerPorSkusAsync(IEnumerable<string> skus)
    {
        IReadOnlyList<Producto> encontrados = skus
            .Where(_productos.ContainsKey)
            .Select(sku => _productos[sku])
            .ToList();
        return Task.FromResult(encontrados);
    }

    public Task<Pago?> ObtenerPorClaveAsync(string claveIdempotencia) =>
        Task.FromResult(Pagos.FirstOrDefault(p => p.ClaveIdempotencia == claveIdempotencia));

    // ----- Escrituras: solo dejan pendiente -----
    public void Agregar(Pago pago) => _pagosPendientes.Add(pago);
    public void Agregar(MovimientoInventario movimiento) => _movimientosPendientes.Add(movimiento);
    public void Registrar(IEventoDominio evento) => _eventosPendientes.Add(evento);

    // ----- Unidad de trabajo: todo o nada -----
    public Task CommitAsync()
    {
        if (FalloEnCommit is not null)
        {
            // Una transacción fallida descarta TODO lo pendiente.
            _pagosPendientes.Clear();
            _movimientosPendientes.Clear();
            _eventosPendientes.Clear();
            throw FalloEnCommit;
        }

        Pagos.AddRange(_pagosPendientes);
        Movimientos.AddRange(_movimientosPendientes);
        EventosOutbox.AddRange(_eventosPendientes);
        _pagosPendientes.Clear();
        _movimientosPendientes.Clear();
        _eventosPendientes.Clear();
        Commits++;

        return Task.CompletedTask;
    }
}