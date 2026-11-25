using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.UseCases.Clientes;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelClientes : UserControl
{
    private readonly IClienteRepository _clientes;
    private readonly IGestionClientes _gestion;

    private readonly TextBox _txtBuscar = new() { Width = 260, Font = EstiloUi.FuenteNormal };
    private readonly DataGridView _grilla = new() { Dock = DockStyle.Fill };

    private readonly TextBox _txtNombre = new() { Width = 240, Font = EstiloUi.FuenteNormal };
    private readonly ComboBox _cmbCategoria = new() { Width = 240, DropDownStyle = ComboBoxStyle.DropDownList, Font = EstiloUi.FuenteNormal };
    private readonly Button _btnGuardar = new() { Text = "Guardar cambios", Width = 130, Height = 36 };
    private readonly Button _btnCancelar = new() { Text = "Limpiar / Nuevo", Width = 110, Height = 36 };
    private readonly Label _lblMensaje = new() { AutoSize = true, Font = EstiloUi.FuenteNormal, Margin = new Padding(0, 10, 0, 0) };

    private List<Cliente> _listaClientes = new();
    private Cliente? _clienteSeleccionado;

    public PanelClientes(IClienteRepository clientes, IGestionClientes gestion)
    {
        _clientes = clientes;
        _gestion = gestion;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Gestión de clientes", "Registro, búsqueda y categorización comercial");

        // Barra superior con búsqueda y botón nuevo
        var barraTop = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(16, 8, 16, 0), BackColor = Color.White };
        var flujoTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

        flujoTop.Controls.Add(new Label { Text = "Buscar cliente:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
        _txtBuscar.PlaceholderText = "Nombre o código...";
        _txtBuscar.TextChanged += (_, _) => Filtrar();
        flujoTop.Controls.Add(_txtBuscar);

        var btnNuevo = new Button { Text = "Nuevo cliente", Width = 120, Margin = new Padding(12, 0, 0, 0) };
        EstiloUi.AplicarBotonSecundario(btnNuevo);
        btnNuevo.Click += (_, _) => LimpiarFicha();
        flujoTop.Controls.Add(btnNuevo);

        barraTop.Controls.Add(flujoTop);

        // Área central dividida: Grilla (65%) + Ficha del cliente (35%)
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

        // Tarjeta Grilla
        var tarjetaGrilla = EstiloUi.CrearTarjeta(0, 0, new Padding(12));
        tarjetaGrilla.Dock = DockStyle.Fill;

        EstiloUi.AplicarGrilla(_grilla);
        _grilla.Columns.Add("Codigo", "Código");
        _grilla.Columns.Add("Nombre", "Nombre");
        _grilla.Columns.Add("Categoria", "Categoría");
        _grilla.Columns.Add("Estado", "Estado");

        _grilla.SelectionChanged += (_, _) => AlSeleccionarCliente();

        tarjetaGrilla.Controls.Add(_grilla);

        // Tarjeta Ficha del cliente (Wireframe 04)
        var tarjetaFicha = EstiloUi.CrearTarjeta(0, 0, new Padding(20));
        tarjetaFicha.Dock = DockStyle.Fill;

        var lblTituloFicha = new Label
        {
            Text = "Ficha del cliente",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 30
        };

        var flujoFicha = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0, 8, 0, 0)
        };

        flujoFicha.Controls.Add(new Label { Text = "Nombre:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 4, 0, 4) });
        flujoFicha.Controls.Add(_txtNombre);

        flujoFicha.Controls.Add(new Label { Text = "Categoría:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 12, 0, 4) });
        _cmbCategoria.Items.AddRange(Enum.GetNames<CategoriaCliente>());
        _cmbCategoria.SelectedIndex = 0;
        flujoFicha.Controls.Add(_cmbCategoria);

        var flujoBotones = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 20, 0, 0) };
        EstiloUi.AplicarBotonPrimario(_btnGuardar);
        EstiloUi.AplicarBotonSecundario(_btnCancelar);

        _btnGuardar.Click += async (_, _) => await GuardarClienteAsync();
        _btnCancelar.Click += (_, _) => LimpiarFicha();

        flujoBotones.Controls.Add(_btnGuardar);
        flujoBotones.Controls.Add(_btnCancelar);

        flujoFicha.Controls.Add(flujoBotones);
        flujoFicha.Controls.Add(_lblMensaje);

        tarjetaFicha.Controls.Add(flujoFicha);
        tarjetaFicha.Controls.Add(lblTituloFicha);

        panelCentral.Controls.Add(tarjetaGrilla, 0, 0);
        panelCentral.Controls.Add(tarjetaFicha, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(barraTop);
        Controls.Add(encabezado);
    }

    private async Task CargarAsync()
    {
        _listaClientes = (await _clientes.ListarAsync()).ToList();
        Filtrar();
    }

    private void Filtrar()
    {
        var busqueda = _txtBuscar.Text.Trim().ToUpperInvariant();
        _grilla.Rows.Clear();

        var filtrados = _listaClientes.Where(c =>
            string.IsNullOrEmpty(busqueda) ||
            c.Nombre.ToUpperInvariant().Contains(busqueda) ||
            $"C{c.Id.ToString()[..3]}".ToUpperInvariant().Contains(busqueda));

        int indice = 1;
        foreach (var c in filtrados)
        {
            _grilla.Rows.Add(
                $"C{indice++:D3}",
                c.Nombre,
                c.Categoria.ToString().ToUpperInvariant(),
                c.Activo ? "ACTIVO" : "INACTIVO");
        }

        if (_grilla.Rows.Count > 0)
            _grilla.Rows[0].Selected = true;
    }

    private void AlSeleccionarCliente()
    {
        _lblMensaje.Text = string.Empty;
        if (_grilla.SelectedRows.Count == 0)
        {
            LimpiarFicha();
            return;
        }

        var nombre = _grilla.SelectedRows[0].Cells["Nombre"].Value?.ToString();
        _clienteSeleccionado = _listaClientes.FirstOrDefault(c => c.Nombre == nombre);
        if (_clienteSeleccionado is null) return;

        _txtNombre.Text = _clienteSeleccionado.Nombre;
        _cmbCategoria.SelectedItem = _clienteSeleccionado.Categoria.ToString();
    }

    private void LimpiarFicha()
    {
        _clienteSeleccionado = null;
        _txtNombre.Clear();
        _cmbCategoria.SelectedIndex = 0;
        _lblMensaje.Text = string.Empty;
        _txtNombre.Focus();
    }

    private async Task GuardarClienteAsync()
    {
        _lblMensaje.Text = string.Empty;
        var nombre = _txtNombre.Text.Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            MostrarError("El nombre del cliente es obligatorio.");
            return;
        }

        var categoria = Enum.Parse<CategoriaCliente>((string)_cmbCategoria.SelectedItem!);

        try
        {
            if (_clienteSeleccionado is null)
            {
                var resultado = await _gestion.GuardarAsync(null, nombre, categoria);
                if (!resultado.Exitoso) { MostrarError(resultado.Error!.Mensaje); return; }
                _lblMensaje.ForeColor = EstiloUi.ColorExito;
                _lblMensaje.Text = "Cliente registrado con éxito.";
            }
            else
            {
                var resultado = await _gestion.GuardarAsync(_clienteSeleccionado.Id, nombre, categoria);
                if (!resultado.Exitoso) { MostrarError(resultado.Error!.Mensaje); return; }
                _lblMensaje.ForeColor = EstiloUi.ColorExito;
                _lblMensaje.Text = "Cliente actualizado.";
            }

            await CargarAsync();
        }
        catch (DomainException ex)
        {
            MostrarError(ex.Message);
        }
        catch (Exception ex)
        {
            MostrarError($"Error: {ex.Message}");
        }
    }

    private void MostrarError(string mensaje)
    {
        _lblMensaje.ForeColor = EstiloUi.ColorPeligro;
        _lblMensaje.Text = mensaje;
    }
}
