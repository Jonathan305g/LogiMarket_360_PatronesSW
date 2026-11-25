using LogiMarket.Application.Abstractions.Gateways;
using LogiMarket.Application.Abstractions.Services;
using LogiMarket.Application.Metrics.Singleton;
using LogiMarket.Application.Security;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Sesion;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelAuditoria : UserControl
{
    private readonly IBitacoraAuditoria _bitacora;
    private readonly IAuditoriaSeguridad _auditoriaSeguridad;
    private readonly MetricasSesion _metricas;
    private readonly IPasarelaPago _pasarela;
    private readonly IProcesadorOutbox _procesadorOutbox;
    private readonly IPasarelaPagoDemo _pasarelaDemo;
    private readonly ISelectorPasarelaPago _selectorPasarela;
    private readonly UsuarioActualEnSesion _sesion;

    private readonly DataGridView _grillaNegocio = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _grillaSeguridad = new() { Dock = DockStyle.Fill };
    private readonly Label _lblMetricas = new() { Dock = DockStyle.Bottom, Height = 42, Font = EstiloUi.FuenteNormal, ForeColor = EstiloUi.ColorTextoSecundario };

    private readonly CheckBox _chkRechazoCobro = new() { Text = "Inducir rechazo de cobro (Proxy)", AutoSize = true, Font = EstiloUi.FuenteNormal };
    private readonly CheckBox _chkFalloOutbox = new() { Text = "Inducir fallo de Outbox", AutoSize = true, Font = EstiloUi.FuenteNormal };
    private readonly Button _btnAplicarEscenario = new() { Text = "Ejecutar prueba de cobro", Width = 260, Height = 36 };
    private readonly Button _btnProbarProxy = new() { Text = "Probar Proxy (Rol Consulta)", Width = 260, Height = 36 };
    private readonly Button _btnReprocesarOutbox = new() { Text = "Reprocesar eventos Outbox", Width = 260, Height = 36 };
    private readonly Label _lblResultadoSim = new() { AutoSize = true, Font = EstiloUi.FuenteNormal, Margin = new Padding(0, 10, 0, 0) };

    public PanelAuditoria(
        IBitacoraAuditoria bitacora,
        IAuditoriaSeguridad auditoriaSeguridad,
        MetricasSesion metricas,
        IPasarelaPago pasarela,
        IProcesadorOutbox procesadorOutbox,
        IPasarelaPagoDemo pasarelaDemo,
        ISelectorPasarelaPago selectorPasarela,
        UsuarioActualEnSesion sesion)
    {
        _bitacora = bitacora;
        _auditoriaSeguridad = auditoriaSeguridad;
        _metricas = metricas;
        _pasarela = pasarela;
        _procesadorOutbox = procesadorOutbox;
        _pasarelaDemo = pasarelaDemo;
        _selectorPasarela = selectorPasarela;
        _sesion = sesion;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Auditoría y simulación", "Eventos, registros y escenarios controlados de prueba");

        // Área central dividida: Registros de eventos (65%) + Modo de demostración (35%)
        var panelCentral = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(16, 8, 16, 16),
            BackColor = Color.White
        };
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
        panelCentral.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

        // Tarjeta Registros de Eventos
        var tarjetaRegistros = EstiloUi.CrearTarjeta(0, 0, new Padding(12));
        tarjetaRegistros.Dock = DockStyle.Fill;

        var headerRegistros = new Panel { Dock = DockStyle.Top, Height = 34 };
        var lblTituloReg = new Label
        {
            Text = "Registro de eventos (Negocio y Seguridad)",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Left,
            AutoSize = true
        };
        var btnRefrescar = new Button { Text = "Actualizar", Dock = DockStyle.Right, Width = 90 };
        EstiloUi.AplicarBotonSecundario(btnRefrescar);
        btnRefrescar.Click += async (_, _) => await CargarAsync();
        headerRegistros.Controls.Add(lblTituloReg);
        headerRegistros.Controls.Add(btnRefrescar);

        var pestañas = new TabControl { Dock = DockStyle.Fill, Font = EstiloUi.FuenteNormal };
        var tabNegocio = new TabPage("Eventos de Negocio (Bitácora)") { BackColor = Color.White };
        var tabSeguridad = new TabPage("Auditoría de Seguridad (Proxy)") { BackColor = Color.White };

        EstiloUi.AplicarGrilla(_grillaNegocio);
        _grillaNegocio.Columns.Add("Hora", "Hora");
        _grillaNegocio.Columns.Add("Evento", "Evento");
        _grillaNegocio.Columns.Add("Resultado", "Detalle");
        tabNegocio.Controls.Add(_grillaNegocio);

        EstiloUi.AplicarGrilla(_grillaSeguridad);
        _grillaSeguridad.Columns.Add("Hora", "Hora");
        _grillaSeguridad.Columns.Add("Usuario", "Usuario");
        _grillaSeguridad.Columns.Add("Rol", "Rol");
        _grillaSeguridad.Columns.Add("Operacion", "Operación");
        tabSeguridad.Controls.Add(_grillaSeguridad);

        pestañas.TabPages.Add(tabNegocio);
        pestañas.TabPages.Add(tabSeguridad);

        tarjetaRegistros.Controls.Add(pestañas);
        tarjetaRegistros.Controls.Add(_lblMetricas);
        tarjetaRegistros.Controls.Add(headerRegistros);

        // Tarjeta Modo de Demostración (Wireframe 12)
        var tarjetaDemo = EstiloUi.CrearTarjeta(0, 0, new Padding(18));
        tarjetaDemo.Dock = DockStyle.Fill;

        var lblTituloDemo = new Label
        {
            Text = "Modo de demostración",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 28
        };

        var flujoDemo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        flujoDemo.Controls.Add(_chkRechazoCobro);
        _chkFalloOutbox.Margin = new Padding(0, 8, 0, 16);
        flujoDemo.Controls.Add(_chkFalloOutbox);

        EstiloUi.AplicarBotonPrimario(_btnAplicarEscenario);
        _btnAplicarEscenario.Margin = new Padding(0, 0, 0, 8);
        _btnAplicarEscenario.Click += async (_, _) => await EjecutarCobroDemoAsync();
        flujoDemo.Controls.Add(_btnAplicarEscenario);

        EstiloUi.AplicarBotonSecundario(_btnProbarProxy);
        _btnProbarProxy.Margin = new Padding(0, 0, 0, 8);
        _btnProbarProxy.Click += async (_, _) => await ProbarProxyConsultaAsync();
        flujoDemo.Controls.Add(_btnProbarProxy);

        EstiloUi.AplicarBotonSecundario(_btnReprocesarOutbox);
        _btnReprocesarOutbox.Click += async (_, _) => await ReprocesarOutboxAsync();
        flujoDemo.Controls.Add(_btnReprocesarOutbox);

        flujoDemo.Controls.Add(_lblResultadoSim);

        tarjetaDemo.Controls.Add(flujoDemo);
        tarjetaDemo.Controls.Add(lblTituloDemo);

        panelCentral.Controls.Add(tarjetaRegistros, 0, 0);
        panelCentral.Controls.Add(tarjetaDemo, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(encabezado);
    }

    private async Task CargarAsync()
    {
        _grillaNegocio.Rows.Clear();
        foreach (var r in await _bitacora.ListarRecientesAsync())
        {
            _grillaNegocio.Rows.Add(
                r.OcurrioUtc.ToLocalTime().ToString("HH:mm:ss"),
                r.TipoEvento,
                r.Detalle);
        }

        _grillaSeguridad.Rows.Clear();
        foreach (var r in await _auditoriaSeguridad.ListarRecientesAsync())
        {
            _grillaSeguridad.Rows.Add(
                r.OcurrioUtc.ToLocalTime().ToString("HH:mm:ss"),
                r.Usuario,
                r.Rol,
                r.Operacion);
        }

        var props = typeof(MetricasSesion).GetProperties()
            .Select(p => $"{p.Name}: {p.GetValue(_metricas)}");
        _lblMetricas.Text = $"Métricas Singleton:   " + string.Join("   |   ", props);
    }

    private async Task EjecutarCobroDemoAsync()
    {
        _lblResultadoSim.Text = string.Empty;
        try
        {
            _selectorPasarela.Seleccionar(ProveedorPagoSimulado.Legado);
            if (_chkRechazoCobro.Checked)
                _pasarelaDemo.RechazarSiguienteCobro();

            var clave = $"DEMO-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
            var resultado = await _pasarela.CobrarAsync(59.00m, clave);

            if (resultado.Exitoso)
            {
                _lblResultadoSim.ForeColor = EstiloUi.ColorExito;
                _lblResultadoSim.Text = $"Cobro aprobado: Ref={resultado.Referencia}";
            }
            else
            {
                MostrarSimError($"Rechazado: {resultado.MotivoRechazo}");
            }

            await CargarAsync();
        }
        catch (Exception ex)
        {
            MostrarSimError(ex.Message);
            await CargarAsync();
        }
    }

    private async Task ProbarProxyConsultaAsync()
    {
        _lblResultadoSim.Text = string.Empty;
        var rolAnterior = _sesion.Rol;
        try
        {
            // Simular rol Consulta
            _sesion.ActualizarRol(RolUsuario.Consulta);
            var clave = $"DEMO-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
            await _pasarela.CobrarAsync(50.00m, clave);
        }
        catch (AccesoDenegadoException ex)
        {
            _lblResultadoSim.ForeColor = EstiloUi.ColorPeligro;
            _lblResultadoSim.Text = $"Proxy rechazó el cobro: {ex.Message}";
        }
        finally
        {
            _sesion.ActualizarRol(rolAnterior);
            await CargarAsync();
        }
    }

    private async Task ReprocesarOutboxAsync()
    {
        _lblResultadoSim.Text = string.Empty;
        try
        {
            if (_chkFalloOutbox.Checked)
            {
                _procesadorOutbox.FallarSiguientePublicacion();
                _chkFalloOutbox.Checked = false;
            }
            await _procesadorOutbox.ProcesarPendientesAsync();
            _lblResultadoSim.ForeColor = EstiloUi.ColorExito;
            _lblResultadoSim.Text = "Eventos Outbox procesados.";
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MostrarSimError(ex.Message);
        }
    }

    private void MostrarSimError(string mensaje)
    {
        _lblResultadoSim.ForeColor = EstiloUi.ColorPeligro;
        _lblResultadoSim.Text = mensaje;
    }
}
