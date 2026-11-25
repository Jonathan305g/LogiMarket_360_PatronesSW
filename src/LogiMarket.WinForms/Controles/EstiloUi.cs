namespace LogiMarket.WinForms.Controles;

public static class EstiloUi
{
    // Colores del sistema inspirados en los wireframes oficiales
    public static readonly Color ColorPrimario = Color.FromArgb(42, 59, 76);       // #2A3B4C (Botones oscuros y encabezados)
    public static readonly Color ColorPrimarioOscuro = Color.FromArgb(30, 41, 59); // #1E293B
    public static readonly Color ColorFondoSidebar = Color.FromArgb(248, 250, 252);// #F8FAFC
    public static readonly Color ColorFondoContenido = Color.FromArgb(255, 255, 255);
    public static readonly Color ColorBorde = Color.FromArgb(226, 232, 240);       // #E2E8F0
    public static readonly Color ColorTexto = Color.FromArgb(30, 41, 59);          // #1E293B
    public static readonly Color ColorTextoSecundario = Color.FromArgb(100, 116, 139); // #64748B
    public static readonly Color ColorExito = Color.FromArgb(21, 128, 61);         // #15803D
    public static readonly Color ColorPeligro = Color.FromArgb(220, 38, 38);       // #DC2626
    public static readonly Color ColorPill = Color.FromArgb(241, 245, 249);        // #F1F5F9

    // Tipografías
    public static readonly Font FuenteTitulo = new("Segoe UI", 14f, FontStyle.Bold);
    public static readonly Font FuenteSubtitulo = new("Segoe UI", 9f, FontStyle.Regular);
    public static readonly Font FuenteSeccion = new("Segoe UI", 11f, FontStyle.Bold);
    public static readonly Font FuenteNormal = new("Segoe UI", 9f, FontStyle.Regular);
    public static readonly Font FuenteNegrita = new("Segoe UI", 9f, FontStyle.Bold);
    public static readonly Font FuenteMonocromatica = new("Consolas", 9f, FontStyle.Regular);

    public static void AplicarBotonPrimario(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 0;
        btn.BackColor = ColorPrimario;
        btn.ForeColor = Color.White;
        btn.Font = FuenteNegrita;
        btn.Cursor = Cursors.Hand;
        btn.Height = Math.Max(btn.Height, 34);
    }

    public static void AplicarBotonSecundario(Button btn)
    {
        btn.FlatStyle = FlatStyle.Flat;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        btn.BackColor = Color.White;
        btn.ForeColor = ColorTexto;
        btn.Font = FuenteNormal;
        btn.Cursor = Cursors.Hand;
        btn.Height = Math.Max(btn.Height, 32);
    }

    public static void AplicarGrilla(DataGridView dgv)
    {
        dgv.BackgroundColor = Color.White;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.GridColor = ColorBorde;
        dgv.EnableHeadersVisualStyles = false;
        dgv.RowHeadersVisible = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.MultiSelect = false;
        dgv.AllowUserToResizeRows = false;
        dgv.Font = FuenteNormal;

        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = ColorTexto;
        dgv.ColumnHeadersDefaultCellStyle.Font = FuenteNegrita;
        dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);
        dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = ColorTexto;
        dgv.ColumnHeadersHeight = 34;
        dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        dgv.DefaultCellStyle.BackColor = Color.White;
        dgv.DefaultCellStyle.ForeColor = ColorTexto;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(226, 232, 240);
        dgv.DefaultCellStyle.SelectionForeColor = ColorPrimarioOscuro;
        dgv.DefaultCellStyle.Padding = new Padding(4, 2, 4, 2);

        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
        dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(226, 232, 240);
        dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = ColorPrimarioOscuro;
        dgv.RowTemplate.Height = 30;
    }

    public static Panel CrearTarjeta(int ancho = 0, int alto = 0, Padding? padding = null)
    {
        var panel = new Panel
        {
            BackColor = Color.White,
            Padding = padding ?? new Padding(16),
            Margin = new Padding(8)
        };
        if (ancho > 0) panel.Width = ancho;
        if (alto > 0) panel.Height = alto;

        panel.Paint += (s, e) =>
        {
            var p = (Panel)s!;
            using var pen = new Pen(ColorBorde, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        };
        return panel;
    }

    public static Panel CrearTarjetaMetrica(string titulo, string valor, int ancho = 160, int alto = 80)
    {
        var tarjeta = CrearTarjeta(ancho, alto, new Padding(12));
        var lblTitulo = new Label
        {
            Text = titulo,
            Font = FuenteNegrita,
            ForeColor = ColorTextoSecundario,
            Dock = DockStyle.Top,
            Height = 20
        };
        var lblValor = new Label
        {
            Text = valor,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = ColorPrimarioOscuro,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        tarjeta.Controls.Add(lblValor);
        tarjeta.Controls.Add(lblTitulo);
        return tarjeta;
    }

    public static Label CrearBadge(string texto, Color? backColor = null, Color? foreColor = null)
    {
        return new Label
        {
            Text = texto,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            BackColor = backColor ?? ColorPill,
            ForeColor = foreColor ?? ColorPrimarioOscuro,
            Padding = new Padding(6, 4, 6, 4),
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    public static Panel CrearEncabezado(string titulo, string subtitulo)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(16, 8, 16, 0),
            BackColor = Color.White
        };

        var lblTitulo = new Label
        {
            Text = titulo,
            Font = FuenteTitulo,
            ForeColor = ColorPrimarioOscuro,
            AutoSize = true,
            Location = new Point(14, 6)
        };
        var lblSub = new Label
        {
            Text = subtitulo,
            Font = FuenteSubtitulo,
            ForeColor = ColorTextoSecundario,
            AutoSize = true,
            Location = new Point(16, 34)
        };

        panel.Controls.Add(lblTitulo);
        panel.Controls.Add(lblSub);
        return panel;
    }
}
