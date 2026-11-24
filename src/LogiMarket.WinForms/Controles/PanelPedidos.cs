using LogiMarket.Application.Abstractions.Repositories;
using System.Text;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelPedidos : UserControl
{
    private readonly IPedidoRepository _pedidos;
    private bool _cargando;
    private readonly DataGridView _grilla = new()
    {
        Dock = DockStyle.Left, Width = 520, ReadOnly = true,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false
    };
    private readonly TextBox _txtDetalle = new()
    {
        Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical
    };

    public PanelPedidos(IPedidoRepository pedidos)
    {
        _pedidos = pedidos;
        _grilla.Columns.Add("Id", "Id");
        _grilla.Columns.Add("Cliente", "Cliente");
        _grilla.Columns.Add("Estado", "Estado");
        _grilla.Columns.Add("Total", "Total");
        _grilla.SelectionChanged += async (_, _) => await MostrarDetalleAsync();
        Controls.Add(_txtDetalle);
        Controls.Add(_grilla);
        Load += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _cargando = true;
        _grilla.Rows.Clear();
        foreach (var resumen in await _pedidos.ListarResumenAsync())
            _grilla.Rows.Add(resumen.Id, resumen.Cliente, resumen.Estado, resumen.Total.ToString("F2"));
        _cargando = false;
        await MostrarDetalleAsync();
    }

    private async Task MostrarDetalleAsync()
    {
        if (_cargando || _grilla.SelectedRows.Count == 0)
        {
            _txtDetalle.Clear();
            return;
        }

        try
        {
            var id = (Guid)_grilla.SelectedRows[0].Cells["Id"].Value!;
            var pedido = await _pedidos.ObtenerPorIdAsync(id);
            if (pedido is null)
                return;

            var sb = new StringBuilder();
            sb.AppendLine($"Pedido {pedido.Id}");
            sb.AppendLine($"Estado: {pedido.Estado}");
            sb.AppendLine($"Total: {pedido.Total}");
            sb.AppendLine();
            sb.AppendLine("Líneas:");
            foreach (var detalle in pedido.Detalles)
                sb.AppendLine($"  {detalle.Sku} {detalle.Nombre} x{detalle.Cantidad} @ {detalle.PrecioUnitario}");
            sb.AppendLine();
            sb.AppendLine("Historial:");
            foreach (var cambio in pedido.Historial)
                sb.AppendLine($"  {cambio.Anterior} -> {cambio.Nuevo} ({cambio.FechaUtc:u})");
            _txtDetalle.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            _txtDetalle.Text = $"No se pudo cargar el pedido: {ex.Message}";
        }
    }
}
