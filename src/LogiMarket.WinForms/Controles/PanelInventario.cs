using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.UseCases.Inventario;
using LogiMarket.Domain.Enums;
using LogiMarket.WinForms.Sesion;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelInventario : UserControl
{
    private const int UmbralStockBajo = 5;
    private readonly ICatalogoRepository _catalogo;
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly IAjustarInventarioCasoUso _ajustarInventario;

    private readonly DataGridView _grillaStock = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _grillaMovimientos = new() { Dock = DockStyle.Fill };

    private readonly ComboBox _cmbSku = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly NumericUpDown _numCantidad = new() { Width = 90, Minimum = -100, Maximum = 100, Value = 5, Font = EstiloUi.FuenteNormal };
    private readonly Button _btnRegistrar = new() { Text = "Registrar ajuste", Width = 240, Height = 38 };
    private readonly Label _lblMensaje = new() { AutoSize = true, Font = EstiloUi.FuenteNormal, Margin = new Padding(0, 10, 0, 0) };

    public PanelInventario(
        ICatalogoRepository catalogo,
        IMovimientoInventarioRepository movimientos,
        IAjustarInventarioCasoUso ajustarInventario,
        UsuarioActualEnSesion sesion)
    {
        _catalogo = catalogo;
        _movimientos = movimientos;
        _ajustarInventario = ajustarInventario;
        _btnRegistrar.Enabled = sesion.Rol is RolUsuario.Admin or RolUsuario.Bodega;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Inventario y movimientos", "Existencias, referencias y ajustes auditados");

        // Split vertical: Mitad superior (Stock + Ajuste) y Mitad inferior (Movimientos recientes)
        var divisorVertical = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(16, 8, 16, 16),
            BackColor = Color.White
        };
        divisorVertical.RowStyles.Add(new RowStyle(SizeType.Percent, 55f));
        divisorVertical.RowStyles.Add(new RowStyle(SizeType.Percent, 45f));

        // Parte superior dividida: Grilla Stock (65%) + Formulario Ajuste (35%)
        var panelSuperior = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        panelSuperior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
        panelSuperior.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

        // Tarjeta Existencias
        var tarjetaStock = EstiloUi.CrearTarjeta(0, 0, new Padding(12));
        tarjetaStock.Dock = DockStyle.Fill;

        var headerStock = new Panel { Dock = DockStyle.Top, Height = 34 };
        var lblTituloStock = new Label
        {
            Text = "Existencias por SKU",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Left,
            AutoSize = true
        };
        var btnRefrescar = new Button { Text = "Actualizar", Dock = DockStyle.Right, Width = 90 };
        EstiloUi.AplicarBotonSecundario(btnRefrescar);
        btnRefrescar.Click += async (_, _) => await CargarAsync();
        headerStock.Controls.Add(lblTituloStock);
        headerStock.Controls.Add(btnRefrescar);

        EstiloUi.AplicarGrilla(_grillaStock);
        _grillaStock.Columns.Add("Sku", "SKU");
        _grillaStock.Columns.Add("Producto", "Producto");
        _grillaStock.Columns.Add("Disponible", "Disponible");
        _grillaStock.Columns.Add("Alerta", "Alerta");

        _grillaStock.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            if (_grillaStock.Columns[e.ColumnIndex].Name == "Alerta")
            {
                var val = e.Value?.ToString();
                if (val == "BAJO")
                {
                    e.CellStyle!.ForeColor = EstiloUi.ColorPeligro;
                    e.CellStyle.Font = EstiloUi.FuenteNegrita;
                }
                else if (val == "OK")
                {
                    e.CellStyle!.ForeColor = EstiloUi.ColorExito;
                    e.CellStyle.Font = EstiloUi.FuenteNegrita;
                }
            }
        };

        tarjetaStock.Controls.Add(_grillaStock);
        tarjetaStock.Controls.Add(headerStock);

        // Tarjeta Movimiento Nuevo (Wireframe 10)
        var tarjetaAjuste = EstiloUi.CrearTarjeta(0, 0, new Padding(18));
        tarjetaAjuste.Dock = DockStyle.Fill;

        var lblTituloAjuste = new Label
        {
            Text = "Movimiento nuevo",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 28
        };

        var flujoAjuste = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        flujoAjuste.Controls.Add(new Label { Text = "SKU:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
        flujoAjuste.Controls.Add(_cmbSku);

        flujoAjuste.Controls.Add(new Label { Text = "Cantidad a ajustar (+ / -):", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 10, 0, 4) });
        flujoAjuste.Controls.Add(_numCantidad);

        EstiloUi.AplicarBotonPrimario(_btnRegistrar);
        _btnRegistrar.Margin = new Padding(0, 18, 0, 6);
        _btnRegistrar.Click += async (_, _) => await RegistrarAjusteAsync();
        flujoAjuste.Controls.Add(_btnRegistrar);
        flujoAjuste.Controls.Add(_lblMensaje);

        tarjetaAjuste.Controls.Add(flujoAjuste);
        tarjetaAjuste.Controls.Add(lblTituloAjuste);

        panelSuperior.Controls.Add(tarjetaStock, 0, 0);
        panelSuperior.Controls.Add(tarjetaAjuste, 1, 0);

        // Tarjeta Movimientos Recientes (Parte inferior)
        var tarjetaMovimientos = EstiloUi.CrearTarjeta(0, 0, new Padding(12));
        tarjetaMovimientos.Dock = DockStyle.Fill;

        var lblTituloMov = new Label
        {
            Text = "Movimientos recientes auditados",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 30
        };

        EstiloUi.AplicarGrilla(_grillaMovimientos);
        _grillaMovimientos.Columns.Add("Sku", "SKU");
        _grillaMovimientos.Columns.Add("Cantidad", "Cantidad");
        _grillaMovimientos.Columns.Add("Motivo", "Motivo");
        _grillaMovimientos.Columns.Add("Referencia", "Referencia");
        _grillaMovimientos.Columns.Add("Fecha", "Fecha");

        tarjetaMovimientos.Controls.Add(_grillaMovimientos);
        tarjetaMovimientos.Controls.Add(lblTituloMov);

        divisorVertical.Controls.Add(panelSuperior, 0, 0);
        divisorVertical.Controls.Add(tarjetaMovimientos, 0, 1);

        Controls.Add(divisorVertical);
        Controls.Add(encabezado);
    }

    private async Task CargarAsync()
    {
        _grillaStock.Rows.Clear();
        _cmbSku.Items.Clear();

        var catalogo = await _catalogo.ListarResumenAsync();
        foreach (var item in catalogo.Where(c => !c.EsKit))
        {
            var stock = item.Stock ?? 0;
            var alerta = stock <= UmbralStockBajo ? "BAJO" : "OK";
            _grillaStock.Rows.Add(item.Sku, item.Nombre, stock, alerta);
            _cmbSku.Items.Add($"{item.Sku} - {item.Nombre}");
        }

        if (_cmbSku.Items.Count > 0)
            _cmbSku.SelectedIndex = 0;

        _grillaMovimientos.Rows.Clear();
        foreach (var m in await _movimientos.ListarAsync())
        {
            var cantTexto = m.Cantidad > 0 ? $"+{m.Cantidad}" : m.Cantidad.ToString();
            _grillaMovimientos.Rows.Add(
                m.Sku,
                cantTexto,
                m.Motivo.ToString().ToUpperInvariant(),
                m.Referencia.ToString()[..8].ToUpperInvariant(),
                m.FechaUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
        }
    }

    private async Task RegistrarAjusteAsync()
    {
        _lblMensaje.Text = string.Empty;
        if (_cmbSku.SelectedItem is null)
        {
            MostrarError("Selecciona un SKU.");
            return;
        }

        var sku = _cmbSku.SelectedItem.ToString()!.Split(" - ")[0].Trim();
        var cantidad = (int)_numCantidad.Value;

        try
        {
            var resultado = await _ajustarInventario.EjecutarAsync(new SolicitudAjusteInventario(sku, cantidad));
            if (!resultado.Exitoso)
            {
                MostrarError(resultado.Error?.Mensaje ?? "No se pudo registrar el ajuste.");
                return;
            }

            _lblMensaje.ForeColor = EstiloUi.ColorExito;
            _lblMensaje.Text = $"Ajuste registrado: {sku} ({cantidad:+0;-0})";
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MostrarError(ex.Message);
        }
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = EstiloUi.ColorPeligro;
        _lblMensaje.Text = mensaje;
    }
}
