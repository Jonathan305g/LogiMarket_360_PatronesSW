namespace LogiMarket.WinForms.Mediador;

public sealed class MediadorPrincipal : IMediadorPrincipal
{
    private Control? _contenedor;
    private readonly Dictionary<TipoPanel, Func<Control>> _fabricasDePanel = new();
    private readonly Dictionary<TipoPanel, Func<Task>> _actualizaciones = new();

    public event Action<TipoPanel>? PanelCambiado;
    public event Action<Guid>? PedidoConfirmado;
    public Func<TipoPanel, bool>? ValidadorPanel { get; set; }

    public void RegistrarPanelPrincipal(Control contenedor) =>
        _contenedor = contenedor ?? throw new ArgumentNullException(nameof(contenedor));

    public void RegistrarFabricaDePanel(TipoPanel panel, Func<Control> fabrica) =>
        _fabricasDePanel[panel] = fabrica ?? throw new ArgumentNullException(nameof(fabrica));

    public void MostrarPanel(TipoPanel panel)
    {
        if (_contenedor is null)
            return;
        if (ValidadorPanel is not null && !ValidadorPanel(panel))
            return;
        if (!_fabricasDePanel.TryGetValue(panel, out var fabrica))
            throw new InvalidOperationException($"No hay un panel registrado para '{panel}'.");

        while (_contenedor.Controls.Count > 0)
        {
            var actual = _contenedor.Controls[0];
            _contenedor.Controls.RemoveAt(0);
            actual.Dispose();
        }

        var nuevo = fabrica();
        nuevo.Dock = DockStyle.Fill;
        _contenedor.Controls.Add(nuevo);

        PanelCambiado?.Invoke(panel);
    }

    public void NotificarPedidoConfirmado(Guid pedidoId)
    {
        PedidoConfirmado?.Invoke(pedidoId);
        _ = ActualizarPanelesAsync(
            TipoPanel.PanelGeneral,
            TipoPanel.Catalogo,
            TipoPanel.Pedidos,
            TipoPanel.Inventario);
        MostrarPanel(TipoPanel.Pedidos);
    }

    public void RegistrarActualizacion(TipoPanel panel, Func<Task> accion)
    {
        ArgumentNullException.ThrowIfNull(accion);
        _actualizaciones[panel] = accion;
    }

    public void QuitarActualizacion(TipoPanel panel) =>
        _actualizaciones.Remove(panel);

    public async Task ActualizarPanelesAsync(params TipoPanel[] paneles)
    {
        foreach (var panel in paneles)
        {
            if (_actualizaciones.TryGetValue(panel, out var accion))
            {
                try { await accion(); }
                catch { /* El panel informa su propio estado; el mediador continúa con los demás. */ }
            }
        }
    }
}
