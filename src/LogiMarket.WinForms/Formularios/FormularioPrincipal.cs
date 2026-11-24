using LogiMarket.WinForms.Mediador;
using LogiMarket.WinForms.Controles;
using LogiMarket.WinForms.Sesion;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms.Formularios;

public sealed class FormularioPrincipal : Form
{
    private readonly IMediadorPrincipal _mediador;
    private readonly IServiceProvider _servicios;
    private readonly Panel _panelContenido = new() { Dock = DockStyle.Fill };

    public FormularioPrincipal(IMediadorPrincipal mediador, IServiceProvider servicios)
    {
        _mediador = mediador;
        _servicios = servicios;
        Text = "LogiMarket 360";
        Width = 1024;
        Height = 700;
        StartPosition = FormStartPosition.CenterScreen;

        var navegacion = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            Width = 200,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8)
        };

        AgregarBoton(navegacion, "Acceso", TipoPanel.Acceso);
        AgregarBoton(navegacion, "Panel general", TipoPanel.PanelGeneral);
        AgregarBoton(navegacion, "Catálogo y kits", TipoPanel.Catalogo);
        AgregarBoton(navegacion, "Clientes", TipoPanel.Clientes);
        AgregarBoton(navegacion, "Carrito", TipoPanel.Carrito);
        AgregarBoton(navegacion, "Pedidos", TipoPanel.Pedidos);
        AgregarBoton(navegacion, "Cobro", TipoPanel.Cobro);
        AgregarBoton(navegacion, "Despacho", TipoPanel.Despacho);
        AgregarBoton(navegacion, "Devoluciones", TipoPanel.Devoluciones);
        AgregarBoton(navegacion, "Inventario", TipoPanel.Inventario);
        AgregarBoton(navegacion, "Reportes", TipoPanel.Reportes);
        AgregarBoton(navegacion, "Auditoría", TipoPanel.Auditoria);

        Controls.Add(_panelContenido);
        Controls.Add(navegacion);
        _mediador.RegistrarPanelPrincipal(_panelContenido);
        RegistrarPaneles();
        _mediador.MostrarPanel(TipoPanel.Acceso);
    }

    private void AgregarBoton(Control contenedor, string texto, TipoPanel panel)
    {
        var boton = new Button { Text = texto, Width = 180, Height = 36, Margin = new Padding(2) };
        boton.Click += (_, _) => _mediador.MostrarPanel(panel);
        contenedor.Controls.Add(boton);
    }

    private void RegistrarPaneles()
    {
        foreach (var panel in Enum.GetValues<TipoPanel>())
        {
            var panelCapturado = panel;
            _mediador.RegistrarFabricaDePanel(panelCapturado,
                () => new Label
                {
                    Text = $"{panelCapturado}: pantalla pendiente de implementar",
                    AutoSize = true,
                    Padding = new Padding(16)
                });
        }

        _mediador.RegistrarFabricaDePanel(TipoPanel.Acceso,
            () => _servicios.GetRequiredService<PanelAcceso>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.PanelGeneral, () =>
        {
            var sesion = _servicios.GetRequiredService<UsuarioActualEnSesion>();
            return new Label
            {
                Text = $"Bienvenido, {sesion.NombreUsuario} ({sesion.Rol})",
                AutoSize = true,
                Padding = new Padding(16)
            };
        });
    }
}
