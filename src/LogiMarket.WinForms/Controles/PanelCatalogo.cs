using LogiMarket.Application.Abstractions.Repositories;
using LogiMarket.Application.UseCases.Catalogo;
using LogiMarket.Domain.Catalog.Composite;
using LogiMarket.Domain.Catalog.Visitor;

namespace LogiMarket.WinForms.Controles;

public sealed class PanelCatalogo : UserControl
{
    private const int UmbralStockBajo = 5;
    private readonly ICatalogoRepository _catalogo;
    private readonly IGestionCatalogo _gestion;
    private readonly Label _lblMensaje = new() { AutoSize = true, Font = EstiloUi.FuenteNormal };

    private readonly TextBox _txtBuscar = new() { Width = 280, Font = EstiloUi.FuenteNormal };
    private readonly DataGridView _grilla = new() { Dock = DockStyle.Fill };
    private readonly TreeView _arbolEstructura = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        Font = EstiloUi.FuenteNormal,
        ShowLines = true,
        ShowPlusMinus = true
    };
    private readonly Label _lblVisitorDesglose = new() { AutoSize = true, Font = EstiloUi.FuenteSubtitulo, ForeColor = EstiloUi.ColorTextoSecundario, Dock = DockStyle.Bottom };
    private readonly Label _lblVisitorPeso = new() { AutoSize = true, Font = EstiloUi.FuenteNegrita, ForeColor = EstiloUi.ColorPrimarioOscuro, Dock = DockStyle.Bottom, Height = 24 };

    private List<ResumenElementoCatalogo> _elementos = new();

    public PanelCatalogo(ICatalogoRepository catalogo, IGestionCatalogo gestion)
    {
        _catalogo = catalogo;
        _gestion = gestion;

        Dock = DockStyle.Fill;
        BackColor = Color.White;

        ConstruirInterfaz();
        Load += async (_, _) => await CargarAsync();
    }

    private void ConstruirInterfaz()
    {
        var encabezado = EstiloUi.CrearEncabezado("Catálogo y kits", "Productos individuales y kits compuestos sin ciclos");

        // Barra superior con filtro de búsqueda
        var barraTop = new Panel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(16, 8, 16, 0), BackColor = Color.White };
        var flujoTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };

        flujoTop.Controls.Add(new Label { Text = "Buscar producto:", Font = EstiloUi.FuenteNegrita, AutoSize = true, Margin = new Padding(0, 6, 6, 0) });
        _txtBuscar.PlaceholderText = "SKU o nombre...";
        _txtBuscar.TextChanged += (_, _) => Filtrar();
        flujoTop.Controls.Add(_txtBuscar);

        var btnNuevoProducto = new Button { Text = "Nuevo producto", Width = 116 };
        EstiloUi.AplicarBotonPrimario(btnNuevoProducto);
        btnNuevoProducto.Click += async (_, _) => await EditarProductoAsync(null);
        flujoTop.Controls.Add(btnNuevoProducto);

        var btnEditar = new Button { Text = "Editar", Width = 72 };
        EstiloUi.AplicarBotonSecundario(btnEditar);
        btnEditar.Click += async (_, _) => await EditarSeleccionAsync();
        flujoTop.Controls.Add(btnEditar);

        var btnEliminar = new Button { Text = "Eliminar", Width = 78 };
        EstiloUi.AplicarBotonSecundario(btnEliminar);
        btnEliminar.Click += async (_, _) => await EliminarSeleccionAsync();
        flujoTop.Controls.Add(btnEliminar);

        var btnKit = new Button { Text = "Nuevo kit", Width = 86 };
        EstiloUi.AplicarBotonSecundario(btnKit);
        btnKit.Click += async (_, _) => await EditarKitAsync();
        flujoTop.Controls.Add(btnKit);

        var btnActualizar = new Button { Text = "Actualizar", Width = 95, Margin = new Padding(12, 0, 0, 0) };
        EstiloUi.AplicarBotonSecundario(btnActualizar);
        btnActualizar.Click += async (_, _) => await CargarAsync();
        flujoTop.Controls.Add(btnActualizar);
        flujoTop.Controls.Add(_lblMensaje);

        barraTop.Controls.Add(flujoTop);

        // Área central dividida: Grilla (65%) + Árbol de Estructura de Kits / Visitor (35%)
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
        _grilla.Columns.Add("Sku", "SKU");
        _grilla.Columns.Add("Producto", "Producto");
        _grilla.Columns.Add("Tipo", "Tipo");
        _grilla.Columns.Add("Precio", "Precio USD");
        _grilla.Columns.Add("Stock", "Stock");

        _grilla.CellFormatting += (s, e) =>
        {
            if (e.RowIndex < 0) return;
            var col = _grilla.Columns[e.ColumnIndex].Name;
            if (col == "Stock")
            {
                var valor = _grilla.Rows[e.RowIndex].Cells["Stock"].Value?.ToString();
                if (int.TryParse(valor, out var stock) && stock <= UmbralStockBajo)
                    e.CellStyle!.ForeColor = EstiloUi.ColorPeligro;
            }
        };

        _grilla.SelectionChanged += async (_, _) => await AlSeleccionarElementoAsync();

        tarjetaGrilla.Controls.Add(_grilla);

        // Tarjeta Estructura del Kit (Composite + Visitor)
        var tarjetaEstructura = EstiloUi.CrearTarjeta(0, 0, new Padding(16));
        tarjetaEstructura.Dock = DockStyle.Fill;

        var lblTituloEstructura = new Label
        {
            Text = "Estructura del kit",
            Font = EstiloUi.FuenteSeccion,
            ForeColor = EstiloUi.ColorTexto,
            Dock = DockStyle.Top,
            Height = 30
        };

        var panelInferiorVisitor = new Panel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(0, 8, 0, 0) };
        panelInferiorVisitor.Controls.Add(_lblVisitorPeso);
        panelInferiorVisitor.Controls.Add(_lblVisitorDesglose);

        tarjetaEstructura.Controls.Add(_arbolEstructura);
        tarjetaEstructura.Controls.Add(panelInferiorVisitor);
        tarjetaEstructura.Controls.Add(lblTituloEstructura);

        panelCentral.Controls.Add(tarjetaGrilla, 0, 0);
        panelCentral.Controls.Add(tarjetaEstructura, 1, 0);

        Controls.Add(panelCentral);
        Controls.Add(barraTop);
        Controls.Add(encabezado);
    }

    private async Task CargarAsync()
    {
        _elementos = (await _catalogo.ListarResumenAsync()).ToList();
        Filtrar();
    }

    private void Filtrar()
    {
        var busqueda = _txtBuscar.Text.Trim().ToUpperInvariant();
        _grilla.Rows.Clear();

        var filtrados = _elementos.Where(e =>
            string.IsNullOrEmpty(busqueda) ||
            e.Sku.ToUpperInvariant().Contains(busqueda) ||
            e.Nombre.ToUpperInvariant().Contains(busqueda));

        foreach (var item in filtrados)
        {
            _grilla.Rows.Add(
                item.Sku,
                item.Nombre,
                item.EsKit ? "Kit" : "Producto",
                item.EsKit ? "(calculado)" : $"USD {item.Precio.GetValueOrDefault():F2}",
                item.Stock?.ToString() ?? "N/D");
        }

        if (_grilla.Rows.Count > 0)
            _grilla.Rows[0].Selected = true;
    }

    private async Task AlSeleccionarElementoAsync()
    {
        _arbolEstructura.Nodes.Clear();
        _lblVisitorDesglose.Text = string.Empty;
        _lblVisitorPeso.Text = string.Empty;

        if (_grilla.SelectedRows.Count == 0) return;
        var sku = _grilla.SelectedRows[0].Cells["Sku"].Value?.ToString();
        if (string.IsNullOrEmpty(sku)) return;

        var elemento = await _catalogo.ObtenerPorSkuAsync(sku);
        if (elemento is null) return;

        // Construir árbol según Composite
        var nodoRaiz = new TreeNode($"{elemento.Sku} · {elemento.Nombre} (USD {elemento.ObtenerPrecio().Valor:F2})");
        ConstruirNodosArbol(elemento, nodoRaiz);
        _arbolEstructura.Nodes.Add(nodoRaiz);
        nodoRaiz.ExpandAll();

        // Aplicar Visitor (Desglose Físico y Estimador de Peso)
        var visitorDesglose = new DesgloseFisicoVisitor();
        var visitorPeso = new PesoLogisticoVisitor();

        elemento.Aceptar(visitorDesglose, 1);
        elemento.Aceptar(visitorPeso, 1);

        var detalleComponentes = string.Join(", ", visitorDesglose.Desglose.Select(d => $"{d.Key} x{d.Value}"));
        _lblVisitorDesglose.Text = $"Componentes físicos: {detalleComponentes}";
        _lblVisitorPeso.Text = $"Peso total estimado: {visitorPeso.PesoTotalKg:F2} kg";
    }

    private async Task EditarSeleccionAsync()
    {
        if (_grilla.SelectedRows.Count == 0) return;
        var sku = _grilla.SelectedRows[0].Cells["Sku"].Value?.ToString();
        var item = _elementos.FirstOrDefault(e => e.Sku == sku);
        if (item is null) return;
        if (item.EsKit) await EditarKitAsync(item.Sku);
        else await EditarProductoAsync(item.Sku);
    }

    private async Task EditarProductoAsync(string? skuExistente)
    {
        using var dialogo = new Form
        {
            Text = skuExistente is null ? "Nuevo producto" : "Editar producto",
            Width = 390,
            Height = 390,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 6 };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        var sku = new TextBox { Dock = DockStyle.Fill, Text = skuExistente ?? string.Empty, ReadOnly = skuExistente is not null };
        var nombre = new TextBox { Dock = DockStyle.Fill };
        var precio = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Minimum = 0.01m, Maximum = 1000000m, Value = 1m, ThousandsSeparator = true };
        var peso = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 3, Minimum = 0, Maximum = 100000m, Value = 0m, Increment = 0.1m };
        var stock = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 1000000, Value = 0 };
        if (skuExistente is not null && await _catalogo.ObtenerPorSkuAsync(skuExistente) is ProductoSimple existente)
        {
            nombre.Text = existente.Nombre;
            precio.Value = existente.ObtenerPrecio().Valor;
            peso.Value = existente.PesoKg;
            stock.Enabled = false;
        }
        AgregarCampo(tabla, 0, "SKU", sku);
        AgregarCampo(tabla, 1, "Nombre", nombre);
        AgregarCampo(tabla, 2, "Precio USD", precio);
        AgregarCampo(tabla, 3, "Peso kg", peso);
        AgregarCampo(tabla, 4, "Stock inicial", stock);
        var guardar = new Button { Text = "Guardar", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 100 };
        tabla.Controls.Add(guardar, 1, 5);
        dialogo.Controls.Add(tabla);
        dialogo.AcceptButton = guardar;
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        var resultado = await _gestion.GuardarProductoAsync(sku.Text, nombre.Text, precio.Value, peso.Value, (int)stock.Value);
        MostrarResultado(resultado.Exitoso, resultado.Error?.Mensaje ?? "Producto guardado.");
        if (resultado.Exitoso) await CargarAsync();
    }

    private async Task EditarKitAsync(string? skuExistente = null)
    {
        using var dialogo = new Form
        {
            Text = skuExistente is null ? "Nuevo kit" : "Editar kit",
            Width = 470,
            Height = 420,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };
        var tabla = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 2, RowCount = 4 };
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        tabla.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        var sku = new TextBox { Dock = DockStyle.Fill, Text = skuExistente ?? string.Empty, ReadOnly = skuExistente is not null };
        var nombre = new TextBox { Dock = DockStyle.Fill };
        var componentes = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical,
            PlaceholderText = "Un componente por línea: SKU;cantidad" };
        if (skuExistente is not null && await _catalogo.ObtenerPorSkuAsync(skuExistente) is KitCompuesto kitExistente)
        {
            nombre.Text = kitExistente.Nombre;
            componentes.Text = string.Join(Environment.NewLine,
                kitExistente.Items.Select(i => $"{i.Elemento.Sku};{i.Cantidad}"));
        }
        AgregarCampo(tabla, 0, "SKU del kit", sku);
        AgregarCampo(tabla, 1, "Nombre", nombre);
        tabla.Controls.Add(new Label { Text = "Componentes", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
        tabla.Controls.Add(componentes, 1, 2);
        var guardar = new Button { Text = "Guardar kit", DialogResult = DialogResult.OK, Dock = DockStyle.Right, Width = 110 };
        tabla.Controls.Add(guardar, 1, 3);
        dialogo.Controls.Add(tabla);
        dialogo.AcceptButton = guardar;
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;

        var parsed = new List<ComponenteKitSolicitud>();
        foreach (var linea in componentes.Lines.Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var partes = linea.Split(';', StringSplitOptions.TrimEntries);
            if (partes.Length != 2 || !int.TryParse(partes[1], out var cantidad) || cantidad < 1)
            {
                MostrarResultado(false, $"Componente inválido: '{linea}'. Usa SKU;cantidad.");
                return;
            }
            var esKit = _elementos.FirstOrDefault(e => e.Sku.Equals(partes[0], StringComparison.OrdinalIgnoreCase))?.EsKit ?? false;
            parsed.Add(new ComponenteKitSolicitud(partes[0], cantidad, esKit));
        }
        var resultado = await _gestion.GuardarKitAsync(sku.Text, nombre.Text, parsed);
        MostrarResultado(resultado.Exitoso, resultado.Error?.Mensaje ?? "Kit guardado.");
        if (resultado.Exitoso) await CargarAsync();
    }

    private async Task EliminarSeleccionAsync()
    {
        if (_grilla.SelectedRows.Count == 0) return;
        var sku = _grilla.SelectedRows[0].Cells["Sku"].Value?.ToString();
        if (string.IsNullOrWhiteSpace(sku)) return;
        if (MessageBox.Show(this, $"¿Eliminar {sku}? No se puede eliminar si aparece en pedidos, kits o movimientos.",
                "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        var resultado = await _gestion.EliminarAsync(sku);
        MostrarResultado(resultado.Exitoso, resultado.Error?.Mensaje ?? "Elemento eliminado.");
        if (resultado.Exitoso) await CargarAsync();
    }

    private static void AgregarCampo(TableLayoutPanel tabla, int fila, string etiqueta, Control campo)
    {
        tabla.Controls.Add(new Label { Text = etiqueta, AutoSize = true, Anchor = AnchorStyles.Left }, 0, fila);
        tabla.Controls.Add(campo, 1, fila);
    }

    private void MostrarResultado(bool correcto, string mensaje)
    {
        _lblMensaje.ForeColor = correcto ? EstiloUi.ColorExito : EstiloUi.ColorPeligro;
        _lblMensaje.Text = mensaje;
    }

    private void ConstruirNodosArbol(IElementoCatalogo elemento, TreeNode nodoPadre)
    {
        if (elemento is KitCompuesto kit)
        {
            foreach (var item in kit.Items)
            {
                var comp = item.Elemento;
                var cant = item.Cantidad;
                var nodoHijo = new TreeNode($"x{cant} {comp.Sku} · {comp.Nombre}");
                ConstruirNodosArbol(comp, nodoHijo);
                nodoPadre.Nodes.Add(nodoHijo);
            }
        }
    }
}
