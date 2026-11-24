using LogiMarket.Application.Abstractions.Repositories;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelInventario : UserControl
{
    private readonly IMovimientoInventarioRepository _movimientos;
    private readonly DataGridView _grilla = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true,
        AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
    };

    public PanelInventario(IMovimientoInventarioRepository movimientos)
    {
        _movimientos = movimientos;
        _grilla.Columns.Add("Sku", "SKU");
        _grilla.Columns.Add("Cantidad", "Cantidad");
        _grilla.Columns.Add("Motivo", "Motivo");
        _grilla.Columns.Add("Referencia", "Referencia");
        _grilla.Columns.Add("Fecha", "Fecha UTC");
        Controls.Add(_grilla);
        Load += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _grilla.Rows.Clear();
        foreach (var movimiento in await _movimientos.ListarAsync())
            _grilla.Rows.Add(movimiento.Sku, movimiento.Cantidad, movimiento.Motivo,
                movimiento.Referencia, movimiento.FechaUtc.ToLocalTime());
    }
}
