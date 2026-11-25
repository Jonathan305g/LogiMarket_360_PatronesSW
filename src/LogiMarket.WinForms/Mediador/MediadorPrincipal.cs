namespace LogiMarket.WinForms.Mediador;

public sealed class MediadorPrincipal : IMediadorPrincipal
{
    private Control? _contenedor;
    private readonly Dictionary<TipoPanel, Func<Control>> _fabricasDePanel = new();

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
        MostrarPanel(TipoPanel.Pedidos);
    }
}
