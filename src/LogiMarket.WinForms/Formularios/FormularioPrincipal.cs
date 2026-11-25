using LogiMarket.WinForms.Controles;
using LogiMarket.WinForms.Mediador;
using LogiMarket.WinForms.Sesion;
using Microsoft.Extensions.DependencyInjection;

namespace LogiMarket.WinForms.Formularios;

public sealed class FormularioPrincipal : Form
{
    private readonly IMediadorPrincipal _mediador;
    private readonly IServiceProvider _servicios;
    private readonly UsuarioActualEnSesion _sesion;

    private readonly Panel _panelContenido = new()
    {
        Dock = DockStyle.Fill,
        BackColor = Color.White
    };

    private readonly Label _lblUsuarioSesion = new()
    {
        Text = "INVITADO · Sesión local",
        Font = EstiloUi.FuenteNegrita,
        ForeColor = EstiloUi.ColorTextoSecundario,
        AutoSize = true,
        Anchor = AnchorStyles.Top | AnchorStyles.Right
    };

    private readonly Dictionary<TipoPanel, Button> _botonesNavegacion = new();

    public FormularioPrincipal(IMediadorPrincipal mediador, IServiceProvider servicios)
    {
        _mediador = mediador;
        _servicios = servicios;
        _sesion = _servicios.GetRequiredService<UsuarioActualEnSesion>();

        Text = "LOGIMARKET 360 — Control Comercial y Logístico";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(1024, 680);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        var barraSuperior = CrearBarraSuperior();
        var barraLateral = CrearBarraLateral();

        Controls.Add(_panelContenido);
        Controls.Add(barraLateral);
        Controls.Add(barraSuperior);

        _mediador.RegistrarPanelPrincipal(_panelContenido);
        _mediador.PanelCambiado += OnPanelCambiado;
        RegistrarPaneles();

        _mediador.MostrarPanel(TipoPanel.Acceso);
    }

    private Panel CrearBarraSuperior()
    {
        var top = new Panel
        {
            Dock = DockStyle.Top,
            Height = 54,
            BackColor = Color.White,
            Padding = new Padding(16, 0, 16, 0)
        };

        top.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            using var pen = new Pen(EstiloUi.ColorBorde, 1f);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        };

        var lblLogo = new Label
        {
            Text = "LOGIMARKET 360",
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = EstiloUi.ColorPrimarioOscuro,
            AutoSize = true,
            Location = new Point(16, 16)
        };

        var lblSubtitulo = new Label
        {
            Text = "Control comercial · Escritorio",
            Font = EstiloUi.FuenteSubtitulo,
            ForeColor = EstiloUi.ColorTextoSecundario,
            AutoSize = true,
            // Deja espacio suficiente al título de la marca; ambos labels se solapaban.
            Location = new Point(215, 19)
        };

        var panelDerecho = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 14, 0, 0)
        };

        _lblUsuarioSesion.Margin = new Padding(0, 4, 12, 0);

        var btnCerrarSesion = new Button
        {
            Text = "Cerrar sesión",
            Width = 100,
            Height = 28,
            Margin = new Padding(0, 0, 0, 0)
        };
        EstiloUi.AplicarBotonSecundario(btnCerrarSesion);
        btnCerrarSesion.Click += (_, _) =>
        {
            _sesion.CerrarSesion();
            ActualizarEstadoSesion();
            _mediador.MostrarPanel(TipoPanel.Acceso);
        };

        panelDerecho.Controls.Add(_lblUsuarioSesion);
        panelDerecho.Controls.Add(btnCerrarSesion);

        top.Controls.Add(lblSubtitulo);
        top.Controls.Add(lblLogo);
        top.Controls.Add(panelDerecho);

        return top;
    }

    private Panel CrearBarraLateral()
    {
        var lateral = new Panel
        {
            Dock = DockStyle.Left,
            Width = 210,
            BackColor = EstiloUi.ColorFondoSidebar,
            Padding = new Padding(8, 12, 8, 12)
        };

        lateral.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            using var pen = new Pen(EstiloUi.ColorBorde, 1f);
            e.Graphics.DrawLine(pen, p.Width - 1, 0, p.Width - 1, p.Height);
        };

        var flujoBotones = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        AgregarItemNavegacion(flujoBotones, "Panel general", TipoPanel.PanelGeneral);
        AgregarItemNavegacion(flujoBotones, "Catálogo y kits", TipoPanel.Catalogo);
        AgregarItemNavegacion(flujoBotones, "Clientes", TipoPanel.Clientes);
        AgregarItemNavegacion(flujoBotones, "Carrito / borrador", TipoPanel.Carrito);
        AgregarItemNavegacion(flujoBotones, "Pedidos e historial", TipoPanel.Pedidos);
        AgregarItemNavegacion(flujoBotones, "Cobro", TipoPanel.Cobro);
        AgregarItemNavegacion(flujoBotones, "Despacho", TipoPanel.Despacho);
        AgregarItemNavegacion(flujoBotones, "Devoluciones", TipoPanel.Devoluciones);
        AgregarItemNavegacion(flujoBotones, "Inventario", TipoPanel.Inventario);
        AgregarItemNavegacion(flujoBotones, "Reportes", TipoPanel.Reportes);
        AgregarItemNavegacion(flujoBotones, "Auditoría / pruebas", TipoPanel.Auditoria);
        AgregarItemNavegacion(flujoBotones, "Acceso (Login)", TipoPanel.Acceso);

        var pieSidebar = new Label
        {
            Text = "LogiMarket · Escritorio",
            Dock = DockStyle.Bottom,
            Height = 30,
            Font = EstiloUi.FuenteSubtitulo,
            ForeColor = EstiloUi.ColorTextoSecundario,
            TextAlign = ContentAlignment.MiddleCenter
        };

        lateral.Controls.Add(flujoBotones);
        lateral.Controls.Add(pieSidebar);
        return lateral;
    }

    private void AgregarItemNavegacion(Control contenedor, string texto, TipoPanel panel)
    {
        var boton = new Button
        {
            Text = texto,
            Width = 190,
            Height = 36,
            Margin = new Padding(2, 2, 2, 2),
            FlatStyle = FlatStyle.Flat,
            Font = EstiloUi.FuenteNormal,
            ForeColor = EstiloUi.ColorTexto,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        boton.FlatAppearance.BorderSize = 0;
        boton.FlatAppearance.MouseOverBackColor = Color.FromArgb(241, 245, 249);

        boton.Click += (_, _) => _mediador.MostrarPanel(panel);
        _botonesNavegacion[panel] = boton;
        contenedor.Controls.Add(boton);
    }

    private void OnPanelCambiado(TipoPanel panel)
    {
        ActualizarEstadoSesion();
        ActualizarNavegacionPorRol();
        foreach (var (tipo, btn) in _botonesNavegacion)
        {
            if (tipo == panel)
            {
                btn.BackColor = Color.FromArgb(226, 232, 240);
                btn.Font = EstiloUi.FuenteNegrita;
                btn.ForeColor = EstiloUi.ColorPrimarioOscuro;
            }
            else
            {
                btn.BackColor = Color.Transparent;
                btn.Font = EstiloUi.FuenteNormal;
                btn.ForeColor = EstiloUi.ColorTexto;
            }
        }
    }

    private void ActualizarNavegacionPorRol()
    {
        foreach (var (panel, boton) in _botonesNavegacion)
        {
            boton.Visible = panel == TipoPanel.Acceso && !_sesion.EstaAutenticado
                || _sesion.EstaAutenticado && PuedeAbrirPanel(_sesion.Rol, panel);
        }
    }

    private static bool PuedeAbrirPanel(LogiMarket.Domain.Enums.RolUsuario rol, TipoPanel panel) =>
        panel == TipoPanel.PanelGeneral
        || panel == TipoPanel.Catalogo
        || panel == TipoPanel.Pedidos
        || panel == TipoPanel.Reportes
        || panel == TipoPanel.Inventario
        || panel == TipoPanel.Auditoria
        || rol == LogiMarket.Domain.Enums.RolUsuario.Admin
        || rol == LogiMarket.Domain.Enums.RolUsuario.Ventas
            && panel is TipoPanel.Clientes or TipoPanel.Carrito or TipoPanel.Cobro or TipoPanel.Devoluciones
        || rol == LogiMarket.Domain.Enums.RolUsuario.Bodega
            && panel == TipoPanel.Despacho;

    public void ActualizarEstadoSesion()
    {
        if (_sesion.Rol != LogiMarket.Domain.Enums.RolUsuario.Consulta || !string.IsNullOrEmpty(_sesion.NombreUsuario) && _sesion.NombreUsuario != "Invitado")
        {
            _lblUsuarioSesion.Text = $"{_sesion.Rol.ToString().ToUpperInvariant()} · Sesión local";
            _lblUsuarioSesion.ForeColor = EstiloUi.ColorPrimarioOscuro;
        }
        else
        {
            _lblUsuarioSesion.Text = "CONSULTA · Sesión local";
            _lblUsuarioSesion.ForeColor = EstiloUi.ColorTextoSecundario;
        }
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
        _mediador.RegistrarFabricaDePanel(TipoPanel.PanelGeneral,
            () => _servicios.GetRequiredService<PanelGeneral>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Catalogo,
            () => _servicios.GetRequiredService<PanelCatalogo>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Clientes,
            () => _servicios.GetRequiredService<PanelClientes>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Carrito,
            () => _servicios.GetRequiredService<PanelCarrito>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Pedidos,
            () => _servicios.GetRequiredService<PanelPedidos>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Cobro,
            () => _servicios.GetRequiredService<PanelCobro>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Despacho,
            () => _servicios.GetRequiredService<PanelDespacho>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Devoluciones,
            () => _servicios.GetRequiredService<PanelDevoluciones>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Inventario,
            () => _servicios.GetRequiredService<PanelInventario>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Reportes,
            () => _servicios.GetRequiredService<PanelReportes>());
        _mediador.RegistrarFabricaDePanel(TipoPanel.Auditoria,
            () => _servicios.GetRequiredService<PanelAuditoria>());
    }
}
