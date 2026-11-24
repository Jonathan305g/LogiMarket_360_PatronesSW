using LogiMarket.Application.Abstractions.Repositories;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelCatalogo : UserControl
{
    private const int UmbralStockBajo = 5;
    private readonly ICatalogoRepository _catalogo;
    private readonly DataGridView _grilla = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };

    public PanelCatalogo(ICatalogoRepository catalogo)
    {
        _catalogo = catalogo;
        var barra = new Panel { Dock = DockStyle.Top, Height = 48 };
        barra.Controls.Add(new Label { Text = "Catálogo y kits", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(8, 12) });
        var actualizar = new Button { Text = "Actualizar", Width = 100, Location = new Point(900, 8), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        actualizar.Click += async (_, _) => await CargarAsync();
        barra.Controls.Add(actualizar);

        _grilla.Columns.Add("Sku", "SKU");
        _grilla.Columns.Add("Nombre", "Nombre");
        _grilla.Columns.Add("Tipo", "Tipo");
        _grilla.Columns.Add("Stock", "Stock");
        _grilla.CellFormatting += ResaltarStockBajo;

        Controls.Add(_grilla);
        Controls.Add(barra);
        Load += async (_, _) => await CargarAsync();
    }

    private async Task CargarAsync()
    {
        _grilla.Rows.Clear();
        foreach (var item in await _catalogo.ListarResumenAsync())
            _grilla.Rows.Add(item.Sku, item.Nombre, item.EsKit ? "Kit" : "Producto", item.Stock?.ToString() ?? "N/D");
    }

    private void ResaltarStockBajo(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0)
            return;

        var valor = _grilla.Rows[e.RowIndex].Cells["Stock"].Value?.ToString();
        if (int.TryParse(valor, out var stock) && stock <= UmbralStockBajo)
            _grilla.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.Firebrick;
    }
}
