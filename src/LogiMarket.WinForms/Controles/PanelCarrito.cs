using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;
using LogiMarket.Domain.Orders.Builder;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelCarrito : UserControl
{
    private readonly IClienteRepository _clientes;
    private readonly ICatalogoRepository _catalogo;
    private readonly IPedidoRepository _pedidos;
    private readonly IUnitOfWork _uow;
    private PedidoBuilder? _builder;

    private readonly ComboBox _cmbCliente = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _cmbEntrega = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _botonIniciar = new() { Text = "Iniciar pedido", Width = 120 };
    private readonly TextBox _txtSku = new() { Width = 140, Enabled = false };
    private readonly NumericUpDown _numCantidad = new() { Width = 70, Minimum = 1, Maximum = 999, Value = 1, Enabled = false };
    private readonly Button _botonAgregarLinea = new() { Text = "Agregar línea", Width = 110, Enabled = false };
    private readonly Button _botonGuardar = new() { Text = "Guardar pedido", Width = 120, Enabled = false };
    private readonly ListBox _listaLineas = new() { Dock = DockStyle.Fill };
    private readonly Label _lblMensaje = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public PanelCarrito(IClienteRepository clientes, ICatalogoRepository catalogo, IPedidoRepository pedidos, IUnitOfWork uow)
    {
        _clientes = clientes;
        _catalogo = catalogo;
        _pedidos = pedidos;
        _uow = uow;
        ConstruirInterfaz();
        Load += async (_, _) => await CargarClientesAsync();
    }

    private void ConstruirInterfaz()
    {
        var superior = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 140,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(8)
        };
        _cmbEntrega.Items.AddRange(Enum.GetNames<TipoEntrega>());
        _cmbEntrega.SelectedIndex = 0;

        var filaCliente = new FlowLayoutPanel { AutoSize = true };
        filaCliente.Controls.Add(new Label { Text = "Cliente:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        filaCliente.Controls.Add(_cmbCliente);
        filaCliente.Controls.Add(new Label { Text = "Entrega:", AutoSize = true, Margin = new Padding(12, 6, 4, 0) });
        filaCliente.Controls.Add(_cmbEntrega);
        _botonIniciar.Click += (_, _) => IniciarPedido();
        filaCliente.Controls.Add(_botonIniciar);

        var filaLinea = new FlowLayoutPanel { AutoSize = true };
        filaLinea.Controls.Add(new Label { Text = "SKU:", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        filaLinea.Controls.Add(_txtSku);
        filaLinea.Controls.Add(new Label { Text = "Cant.:", AutoSize = true, Margin = new Padding(8, 6, 4, 0) });
        filaLinea.Controls.Add(_numCantidad);
        _botonAgregarLinea.Click += async (_, _) => await AgregarLineaAsync();
        filaLinea.Controls.Add(_botonAgregarLinea);
        _botonGuardar.Click += async (_, _) => await GuardarPedidoAsync();
        filaLinea.Controls.Add(_botonGuardar);

        superior.Controls.Add(filaCliente);
        superior.Controls.Add(filaLinea);
        superior.Controls.Add(_lblMensaje);
        Controls.Add(_listaLineas);
        Controls.Add(superior);
    }

    private async Task CargarClientesAsync()
    {
        _cmbCliente.Items.Clear();
        foreach (var cliente in await _clientes.ListarAsync())
            _cmbCliente.Items.Add(cliente);
        _cmbCliente.DisplayMember = nameof(Cliente.Nombre);
        if (_cmbCliente.Items.Count > 0)
            _cmbCliente.SelectedIndex = 0;
        else
            _lblMensaje.Text = "Primero crea un cliente en la pantalla Clientes.";
    }

    private void IniciarPedido()
    {
        _lblMensaje.Text = string.Empty;
        if (_cmbCliente.SelectedItem is not Cliente cliente)
        {
            _lblMensaje.Text = "Selecciona un cliente.";
            return;
        }

        var entrega = Enum.Parse<TipoEntrega>((string)_cmbEntrega.SelectedItem!);
        _builder = new PedidoBuilder().ConCliente(cliente).ConEntrega(entrega);
        _listaLineas.Items.Clear();
        _cmbCliente.Enabled = false;
        _cmbEntrega.Enabled = false;
        _botonIniciar.Enabled = false;
        _txtSku.Enabled = true;
        _numCantidad.Enabled = true;
        _botonAgregarLinea.Enabled = true;
        _botonGuardar.Enabled = true;
    }

    private async Task AgregarLineaAsync()
    {
        _lblMensaje.Text = string.Empty;
        var sku = _txtSku.Text.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(sku))
        {
            _lblMensaje.Text = "Escribe el SKU de un producto o kit.";
            return;
        }

        try
        {
            var elemento = await _catalogo.ObtenerPorSkuAsync(sku);
            if (elemento is null)
            {
                _lblMensaje.Text = $"No existe ningún producto o kit con SKU '{sku}'.";
                return;
            }

            var cantidad = (int)_numCantidad.Value;
            _builder!.AgregarLinea(elemento, cantidad);
            _listaLineas.Items.Add($"{elemento.Sku} - {elemento.Nombre} x{cantidad}");
            _txtSku.Clear();
        }
        catch (DomainException ex)
        {
            _lblMensaje.Text = ex.Message;
        }
        catch (Exception ex)
        {
            _lblMensaje.Text = $"No se pudo agregar la línea: {ex.Message}";
        }
    }

    private async Task GuardarPedidoAsync()
    {
        _lblMensaje.Text = string.Empty;
        try
        {
            var pedido = _builder!.Construir();
            _pedidos.Agregar(pedido);
            await _uow.CommitAsync();
            MessageBox.Show(this, $"Pedido creado en borrador.\nId: {pedido.Id}", "Pedido guardado",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            ReiniciarFormulario();
        }
        catch (DomainException ex)
        {
            _lblMensaje.Text = ex.Message;
        }
        catch (Exception ex)
        {
            _lblMensaje.Text = $"No se pudo guardar el pedido: {ex.Message}";
        }
    }

    private void ReiniciarFormulario()
    {
        _builder = null;
        _listaLineas.Items.Clear();
        _cmbCliente.Enabled = true;
        _cmbEntrega.Enabled = true;
        _botonIniciar.Enabled = true;
        _txtSku.Enabled = false;
        _numCantidad.Enabled = false;
        _botonAgregarLinea.Enabled = false;
        _botonGuardar.Enabled = false;
    }
}
