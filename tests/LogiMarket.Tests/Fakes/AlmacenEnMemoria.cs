using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Events;
using LogiMarket.Domain.ValueObjects;

namespace LogiMarket.Tests.Fakes;

public sealed class AlmacenEnMemoria : IClienteRepository, IPedidoRepository, IProductoRepository,
    IPagoRepository, IMovimientoInventarioRepository, IOutbox, IUnitOfWork,
    IDespachoRepository, IDevolucionRepository   // NUEVO
{
    private readonly Dictionary<Guid, Cliente> _clientes = new();
    private readonly List<Cliente> _clientesPendientes = new();
    private readonly Dictionary<Guid, Pedido> _pedidos = new();
    private readonly List<Pedido> _pedidosPendientes = new();
    private readonly Dictionary<string, Producto> _productos = new(StringComparer.OrdinalIgnoreCase);

    // Cambios todavía no confirmados.
    private readonly List<Pago> _pagosPendientes = new();
    private readonly List<MovimientoInventario> _movimientosPendientes = new();
    private readonly List<IEventoDominio> _eventosPendientes = new();
    private readonly List<Despacho> _despachosPendientes = new();       
    private readonly List<Devolucion> _devolucionesPendientes = new();  

    // Lo que "quedó guardado en la base" tras un commit.
    public List<Pago> Pagos { get; } = new();
    public List<MovimientoInventario> Movimientos { get; } = new();
    public List<IEventoDominio> EventosOutbox { get; } = new();
    public List<Despacho> Despachos { get; } = new();        
    public List<Devolucion> Devoluciones { get; } = new();   
    public int Commits { get; private set; }

    public Exception? FalloEnCommit { get; set; }

    // ----- Preparación de datos de prueba -----
    public Cliente Sembrar(Cliente cliente) { _clientes[cliente.Id] = cliente; return cliente; }
    public Pedido Sembrar(Pedido pedido) { _pedidos[pedido.Id] = pedido; return pedido; }
    public Producto Sembrar(Producto producto) { _productos[producto.Sku] = producto; return producto; }
    public Producto ProductoPorSku(string sku) => _productos[sku];

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

    // ----- Lecturas -----
    Task<Cliente?> IClienteRepository.ObtenerPorIdAsync(Guid id) =>
        Task.FromResult(_clientes.GetValueOrDefault(id));

    Task<IReadOnlyList<Cliente>> IClienteRepository.ListarAsync() =>
        Task.FromResult<IReadOnlyList<Cliente>>(_clientes.Values.OrderBy(c => c.Nombre).ToList());

    Task<Pedido?> IPedidoRepository.ObtenerPorIdAsync(Guid id) =>
        Task.FromResult(_pedidos.GetValueOrDefault(id));

    public Task<IReadOnlyList<ResumenPedido>> ListarResumenAsync() =>
        Task.FromResult<IReadOnlyList<ResumenPedido>>(_pedidos.Values
            .Select(p => new ResumenPedido(p.Id, "(cliente)", p.Estado, p.Total.Valor))
            .ToList());

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
    public void Agregar(Cliente cliente) => _clientesPendientes.Add(cliente);
    public void Agregar(Pedido pedido) => _pedidosPendientes.Add(pedido);
    public void Agregar(MovimientoInventario movimiento) => _movimientosPendientes.Add(movimiento);
    public void Agregar(Despacho despacho) => _despachosPendientes.Add(despacho);       
    public void Agregar(Devolucion devolucion) => _devolucionesPendientes.Add(devolucion); 
    public void Registrar(IEventoDominio evento) => _eventosPendientes.Add(evento);

    // ----- Unidad de trabajo: todo o nada -----
    public Task CommitAsync()
    {
        if (FalloEnCommit is not null)
        {
            _pagosPendientes.Clear();
            _clientesPendientes.Clear();
            _pedidosPendientes.Clear();
            _movimientosPendientes.Clear();
            _eventosPendientes.Clear();
            _despachosPendientes.Clear();
            _devolucionesPendientes.Clear();
            throw FalloEnCommit;
        }

        Pagos.AddRange(_pagosPendientes);
        foreach (var cliente in _clientesPendientes)
            _clientes[cliente.Id] = cliente;
        foreach (var pedido in _pedidosPendientes)
            _pedidos[pedido.Id] = pedido;
        Movimientos.AddRange(_movimientosPendientes);
        EventosOutbox.AddRange(_eventosPendientes);
        Despachos.AddRange(_despachosPendientes);
        Devoluciones.AddRange(_devolucionesPendientes);

        _pagosPendientes.Clear();
        _clientesPendientes.Clear();
        _pedidosPendientes.Clear();
        _movimientosPendientes.Clear();
        _eventosPendientes.Clear();
        _despachosPendientes.Clear();
        _devolucionesPendientes.Clear();
        Commits++;

        return Task.CompletedTask;
    }

    Task<IReadOnlyList<MovimientoInventario>> IMovimientoInventarioRepository.ListarAsync() =>
        Task.FromResult<IReadOnlyList<MovimientoInventario>>(Movimientos.ToList());

    Task<IReadOnlyList<Despacho>> IDespachoRepository.ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Despacho>>(Despachos.ToList());

    Task<IReadOnlyList<Devolucion>> IDevolucionRepository.ListarAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Devolucion>>(Devoluciones.ToList());
}
