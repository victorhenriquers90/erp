namespace ProjetoVarejo.Desktop.Theme;

/// <summary>
/// Factory para campos de formulário padronizados.
/// Reduz código duplicado e garante consistência visual.
/// </summary>
public static class Inputs
{
    public static Label Rotulo(string texto, int left, int top, int width = 200) => new()
    {
        Text = texto.ToUpperInvariant(),
        Left = left, Top = top, Width = width, Height = 18,
        Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
        ForeColor = Tema.CorTextoMedio,
        BackColor = Color.Transparent
    };

    public static TextBox Texto(int left, int top, int width, bool right = false, string? placeholder = null) => new()
    {
        Left = left, Top = top, Width = width, Height = Tema.AlturaInput,
        Font = new Font(Tema.FontFamily, 10),
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Tema.Branco,
        TextAlign = right ? HorizontalAlignment.Right : HorizontalAlignment.Left,
        PlaceholderText = placeholder ?? ""
    };

    /// <summary>
    /// Adiciona um label + textbox no parent. Retorna o textbox.
    /// </summary>
    public static TextBox CampoTexto(Control parent, string label, int left, int top, int width, bool right = false, string? placeholder = null)
    {
        parent.Controls.Add(Rotulo(label, left, top, width));
        var tb = Texto(left, top + 20, width, right, placeholder);
        parent.Controls.Add(tb);
        return tb;
    }

    public static ComboBox CampoCombo(Control parent, string label, int left, int top, int width)
    {
        parent.Controls.Add(Rotulo(label, left, top, width));
        var cb = new ComboBox
        {
            Left = left, Top = top + 20, Width = width,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(Tema.FontFamily, 10),
            FlatStyle = FlatStyle.Flat,
            BackColor = Tema.Branco
        };
        parent.Controls.Add(cb);
        return cb;
    }

    public static DateTimePicker CampoData(Control parent, string label, int left, int top, int width, DateTime? valor = null)
    {
        parent.Controls.Add(Rotulo(label, left, top, width));
        var dt = new DateTimePicker
        {
            Left = left, Top = top + 20, Width = width,
            Format = DateTimePickerFormat.Short,
            Font = new Font(Tema.FontFamily, 10),
            Value = valor ?? DateTime.Today
        };
        parent.Controls.Add(dt);
        return dt;
    }

    public static CheckBox CampoCheck(Control parent, string label, int left, int top, int width = 200, bool valor = false)
    {
        var ck = new CheckBox
        {
            Text = label,
            Left = left, Top = top, Width = width, Height = 24,
            Font = Tema.FontCorpo,
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent,
            Checked = valor
        };
        parent.Controls.Add(ck);
        return ck;
    }

    /// <summary>
    /// Separador de seção: título uppercase + linha sutil.
    /// </summary>
    public static void Secao(Control parent, string titulo, ref int y, int left = 0, int width = 600)
    {
        if (y > 0) y += 10;
        parent.Controls.Add(new Label
        {
            Text = titulo.ToUpper(),
            Left = left, Top = y, Width = width, Height = 22,
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        });
        parent.Controls.Add(new Panel
        {
            Left = left, Top = y + 22, Width = width - 20, Height = 1,
            BackColor = Tema.CorBorda
        });
        y += 32;
    }

    /// <summary>
    /// Cria barra de busca arredondada (estilo Topbar).
    /// </summary>
    public static (Panel container, TextBox textbox) BarraBusca(string placeholder = "Buscar...")
    {
        var pnl = new Panel { BackColor = Tema.CorCard };
        pnl.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = Tema.PathArredondado(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), Tema.RaioBotao);
            using var brush = new SolidBrush(Tema.CorCard);
            g.FillPath(brush, path);
            using var pen = new Pen(Tema.CorBorda, 1);
            g.DrawPath(pen, path);
        };
        var lblIcone = new Label
        {
            Text = Tema.IconBusca,
            Dock = DockStyle.Left, Width = 40,
            Font = new Font("Segoe MDL2 Assets", 12),
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        var tb = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font(Tema.FontFamily, 10),
            BackColor = Tema.CorCard,
            PlaceholderText = placeholder
        };
        var inner = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard, Padding = new Padding(0, 12, 14, 0) };
        inner.Controls.Add(tb);
        pnl.Controls.Add(inner);
        pnl.Controls.Add(lblIcone);
        return (pnl, tb);
    }

    /// <summary>
    /// Header padrão de página (título + subtítulo).
    /// </summary>
    public static Panel HeaderPagina(string titulo, string subtitulo, int height = 72)
    {
        var header = new Panel { Dock = DockStyle.Top, Height = height, BackColor = Tema.CorFundo, Padding = new Padding(0, 2, 0, 8) };
        var lblTitulo = new Label
        {
            Name = "HeaderTitulo",
            Text = titulo,
            Dock = DockStyle.Top, Height = 32,
            Font = Tema.FontTituloGrande,
            ForeColor = Tema.CorTextoEscuro,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var lblSub = new Label
        {
            Name = "HeaderSubtitulo",
            Text = subtitulo,
            Dock = DockStyle.Top, Height = 22,
            Font = Tema.FontCorpo,
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var linha = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Tema.CorBordaSuave };
        header.Controls.Add(linha);
        header.Controls.Add(lblSub);
        header.Controls.Add(lblTitulo);
        return header;
    }

    public static Label SubtituloHeader(Panel header) =>
        header.Controls.Find("HeaderSubtitulo", false).OfType<Label>().FirstOrDefault()
        ?? throw new InvalidOperationException("Header de pagina sem subtitulo.");

    /// <summary>
    /// Header de tela modal/dialog.
    /// </summary>
    public static Panel HeaderModal(string titulo)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Tema.CorFundo };
        var lblTitulo = new Label
        {
            Text = titulo,
            Dock = DockStyle.Fill,
            Font = Tema.FontTitulo,
            ForeColor = Tema.CorTextoEscuro,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 8, 0, 0)
        };
        var linha = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Tema.CorBordaSuave };
        p.Controls.Add(lblTitulo);
        p.Controls.Add(linha);
        return p;
    }

    /// <summary>
    /// Rodapé com botões padrão Salvar/Cancelar (à direita).
    /// </summary>
    public static (Panel container, Button salvar, Button cancelar) RodapeSalvarCancelar(string textoSalvar = "Salvar")
    {
        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Tema.CorFundo, Padding = new Padding(0, 14, 0, 14) };
        var btnSalvar = Botoes.Primario(textoSalvar, 156, 36); btnSalvar.Dock = DockStyle.Right;
        var spacer = new Panel { Dock = DockStyle.Right, Width = 10 };
        var btnCancelar = Botoes.Ghost("Cancelar", 124, 36); btnCancelar.Dock = DockStyle.Right;
        rodape.Controls.Add(btnSalvar);
        rodape.Controls.Add(spacer);
        rodape.Controls.Add(btnCancelar);
        return (rodape, btnSalvar, btnCancelar);
    }
}
