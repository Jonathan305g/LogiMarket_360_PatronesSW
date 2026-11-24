namespace LogiMarket.WinForms.Mediador;

public sealed class MediadorPrincipal : IMediadorPrincipal
{
    private Control? _contenedor;
    private readonly Dictionary<TipoPanel, Func<Control>> _fabricasDePanel = new();

    public void RegistrarPanelPrincipal(Control contenedor) =>
        _contenedor = contenedor ?? throw new ArgumentNullException(nameof(contenedor));

    public void RegistrarFabricaDePanel(TipoPanel panel, Func<Control> fabrica) =>
        _fabricasDePanel[panel] = fabrica ?? throw new ArgumentNullException(nameof(fabrica));

    public void MostrarPanel(TipoPanel panel)
    {
        if (_contenedor is null)
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
    }

    public void NotificarPedidoConfirmado(Guid pedidoId) => MostrarPanel(TipoPanel.Pedidos);
}
