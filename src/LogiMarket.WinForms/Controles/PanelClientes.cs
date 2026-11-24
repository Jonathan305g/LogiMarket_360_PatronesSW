using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Domain.Entities;
using LogiMarket.Domain.Enums;
using LogiMarket.Domain.Exceptions;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelClientes : UserControl
{
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork _uow;
    private readonly DataGridView _grilla = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };
    private readonly TextBox _txtNombre = new() { Width = 200 };
    private readonly ComboBox _cmbCategoria = new() { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _lblMensaje = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public PanelClientes(IClienteRepository clientes, IUnitOfWork uow)
    {
        _clientes = clientes;
        _uow = uow;
        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var superior = new Panel { Dock = DockStyle.Top, Height = 90 };
        superior.Controls.Add(new Label { Text = "Clientes", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(8, 8) });
        _cmbCategoria.Items.AddRange(Enum.GetNames<CategoriaCliente>());
        _cmbCategoria.SelectedIndex = 0;

        superior.Controls.Add(new Label { Text = "Nombre:", AutoSize = true, Location = new Point(8, 40) });
        _txtNombre.Location = new Point(70, 36);
        superior.Controls.Add(_txtNombre);
        superior.Controls.Add(new Label { Text = "Categoría:", AutoSize = true, Location = new Point(290, 40) });
        _cmbCategoria.Location = new Point(360, 36);
        superior.Controls.Add(_cmbCategoria);

        var agregar = new Button { Text = "Agregar cliente", Width = 130, Location = new Point(510, 34) };
        agregar.Click += async (_, _) => await AgregarClienteAsync();
        superior.Controls.Add(agregar);
        _lblMensaje.Location = new Point(8, 65);
        superior.Controls.Add(_lblMensaje);

        _grilla.Columns.Add("Nombre", "Nombre");
        _grilla.Columns.Add("Categoria", "Categoría");
        Controls.Add(_grilla);
        Controls.Add(superior);
    }

    private async Task CargarAsync()
    {
        _grilla.Rows.Clear();
        foreach (var cliente in await _clientes.ListarAsync())
            _grilla.Rows.Add(cliente.Nombre, cliente.Categoria);
    }

    private async Task AgregarClienteAsync()
    {
        _lblMensaje.Text = string.Empty;
        try
        {
            var categoria = Enum.Parse<CategoriaCliente>((string)_cmbCategoria.SelectedItem!);
            _clientes.Agregar(new Cliente(_txtNombre.Text.Trim(), categoria));
            await _uow.CommitAsync();
            _txtNombre.Clear();
            await CargarAsync();
        }
        catch (DomainException ex)
        {
            _lblMensaje.Text = ex.Message;
        }
        catch (Exception ex)
        {
            _lblMensaje.Text = $"No se pudo guardar el cliente: {ex.Message}";
        }
    }
}
