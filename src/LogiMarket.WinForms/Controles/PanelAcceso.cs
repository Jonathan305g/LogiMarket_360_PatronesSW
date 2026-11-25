using LogiMarket.Application.UseCases.Acceso;
using LogiMarket.WinForms.Mediador;
using LogiMarket.WinForms.Sesion;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelAcceso : UserControl
{
    private readonly IInicioSesionCasoUso _inicioSesion;
    private readonly UsuarioActualEnSesion _sesion;
    private readonly IMediadorPrincipal _mediador;

    private readonly TextBox _txtUsuario = new() { Width = 300, Font = EstiloUi.FuenteNormal };
    private readonly TextBox _txtContrasena = new() { Width = 300, UseSystemPasswordChar = true, Font = EstiloUi.FuenteNormal };
    private readonly Label _lblError = new() { ForeColor = EstiloUi.ColorPeligro, AutoSize = true, Font = EstiloUi.FuenteNormal };

    public PanelAcceso(IInicioSesionCasoUso inicioSesion, UsuarioActualEnSesion sesion, IMediadorPrincipal mediador)
    {
        _inicioSesion = inicioSesion;
        _sesion = sesion;
        _mediador = mediador;

        Dock = DockStyle.Fill;
        BackColor = EstiloUi.ColorFondoSidebar;

        ConstruirInterfaz();
    }

    private void ConstruirInterfaz()
    {
        // Tarjeta central estilo Wireframe 01
        var tarjeta = EstiloUi.CrearTarjeta(460, 480, new Padding(32));
        tarjeta.Anchor = AnchorStyles.None;

        var lblTitulo = new Label
        {
            Text = "ACCESO AL SISTEMA",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = EstiloUi.ColorPrimarioOscuro,
            Dock = DockStyle.Top,
            Height = 36,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblSubtitulo = new Label
        {
            Text = "Credenciales de demostración y perfil de acceso",
            Font = EstiloUi.FuenteSubtitulo,
            ForeColor = EstiloUi.ColorTextoSecundario,
            Dock = DockStyle.Top,
            Height = 24,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var flujoCampos = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 220,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(36, 16, 36, 0)
        };

        var lblUsuario = new Label { Text = "Usuario", Font = EstiloUi.FuenteNegrita, ForeColor = EstiloUi.ColorTexto, AutoSize = true, Margin = new Padding(0, 8, 0, 4) };
        _txtUsuario.Text = "admin";
        _txtUsuario.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await IntentarIngresarAsync();
            }
        };
        var lblContrasena = new Label { Text = "Contraseña", Font = EstiloUi.FuenteNegrita, ForeColor = EstiloUi.ColorTexto, AutoSize = true, Margin = new Padding(0, 12, 0, 4) };
        _txtContrasena.Text = "Admin123!";
        _txtContrasena.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                await IntentarIngresarAsync();
            }
        };

        var btnIngresar = new Button
        {
            Text = "Ingresar",
            Width = 300,
            Height = 38,
            Margin = new Padding(0, 18, 0, 4)
        };
        EstiloUi.AplicarBotonPrimario(btnIngresar);
        btnIngresar.Click += async (_, _) => await IntentarIngresarAsync();

        flujoCampos.Controls.Add(lblUsuario);
        flujoCampos.Controls.Add(_txtUsuario);
        flujoCampos.Controls.Add(lblContrasena);
        flujoCampos.Controls.Add(_txtContrasena);
        flujoCampos.Controls.Add(btnIngresar);
        flujoCampos.Controls.Add(_lblError);

        // Panel inferior con roles y botones rápidos
        var panelInferior = new Panel { Dock = DockStyle.Bottom, Height = 130, Padding = new Padding(12) };
        var lblRoles = new Label
        {
            Text = "Roles: ADMIN · VENTAS · BODEGA · CONSULTA",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
            ForeColor = EstiloUi.ColorTextoSecundario,
            Dock = DockStyle.Top,
            Height = 22,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var flujoAtajos = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(24, 8, 0, 0)
        };

        AgregarAtajo(flujoAtajos, "Admin", "admin", "Admin123!");
        AgregarAtajo(flujoAtajos, "Ventas", "ventas", "Ventas123!");
        AgregarAtajo(flujoAtajos, "Bodega", "bodega", "Bodega123!");
        AgregarAtajo(flujoAtajos, "Consulta", "consulta", "Consulta123!");

        panelInferior.Controls.Add(flujoAtajos);
        panelInferior.Controls.Add(lblRoles);

        tarjeta.Controls.Add(panelInferior);
        tarjeta.Controls.Add(flujoCampos);
        tarjeta.Controls.Add(lblSubtitulo);
        tarjeta.Controls.Add(lblTitulo);

        // Centrado de la tarjeta en el UserControl
        var contenedor = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3
        };
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        contenedor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        contenedor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contenedor.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        contenedor.Controls.Add(tarjeta, 1, 1);
        Controls.Add(contenedor);
    }

    private void AgregarAtajo(Control contenedor, string etiqueta, string usuario, string clave)
    {
        var btn = new Button
        {
            Text = etiqueta,
            Width = 80,
            Height = 28,
            Margin = new Padding(4)
        };
        EstiloUi.AplicarBotonSecundario(btn);
        btn.Click += (_, _) =>
        {
            _txtUsuario.Text = usuario;
            _txtContrasena.Text = clave;
            _lblError.Text = string.Empty;
        };
        contenedor.Controls.Add(btn);
    }

    private async Task IntentarIngresarAsync()
    {
        _lblError.Text = string.Empty;
        _txtUsuario.Enabled = false;
        _txtContrasena.Enabled = false;
        try
        {
            var resultado = await _inicioSesion.IniciarSesionAsync(_txtUsuario.Text, _txtContrasena.Text);
            if (!resultado.Exitoso)
            {
                _lblError.Text = resultado.Error ?? "Credenciales inválidas.";
                return;
            }

            _sesion.IniciarSesion(resultado.Usuario!);
            _mediador.MostrarPanel(TipoPanel.PanelGeneral);
        }
        catch (Exception ex)
        {
            _lblError.Text = $"No se pudo iniciar sesión: {ex.Message}";
        }
        finally
        {
            _txtUsuario.Enabled = true;
            _txtContrasena.Enabled = true;
        }
    }
}
