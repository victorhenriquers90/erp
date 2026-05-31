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

    public static Panel CampoFiltro(string label, Control controle, int width)
    {
        var panel = new Panel
        {
            Width = width,
            Height = 58,
            Margin = new Padding(0, 0, 14, 0),
            BackColor = Tema.CorCard
        };

        panel.Controls.Add(Rotulo(label, 0, 0, width));
        controle.SetBounds(0, 22, width, 30);
        panel.Controls.Add(controle);
        return panel;
    }

    /// <summary>
    /// Cria TextBox moderno sem borda retangular dupla.
    /// Envolve o TextBox em um Panel que desenha borda arredondada.
    /// Retorna (wrapper, textbox).
    /// </summary>
    private static (Panel wrapper, TextBox tb) TextoArredondado(
        int left, int top, int width, bool right = false, string? placeholder = null)
    {
        const int h = 34;
        var tb = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font        = new Font(Tema.FontFamily, 10),
            BackColor   = Tema.Branco,
            TextAlign   = right ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            PlaceholderText = placeholder ?? "",
            // y=6 → centra o texto em um wrapper de 34px (fonte ~22px: top=6, bottom=6)
            Location    = new Point(8, 6),
            Width       = width - 18
        };

        var wrap = new Panel
        {
            Left      = left,
            Top       = top,
            Width     = width,
            Height    = h,
            BackColor = Tema.Branco
            // SEM Region: dentro de Cards brancos o fundo é igual (white=white),
            // e Region em Panels filhos de scrollable panels causa área preta no WinForms.
        };

        wrap.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, wrap.Width - 1, wrap.Height - 1);
            using var path = Tema.PathArredondado(rect, Tema.RaioBotao);

            // Fundo branco com cantos arredondados — elimina o canto retangular
            using var fill = new SolidBrush(Tema.Branco);
            g.FillPath(fill, path);

            // Borda: primária quando focado, discreta quando inativo
            var cor = tb.Focused ? Tema.CorPrimaria : Tema.CorBorda;
            using var pen = new Pen(cor, 1f);
            g.DrawPath(pen, path);
        };
        tb.Enter += (_, _) => wrap.Invalidate();
        tb.Leave += (_, _) => wrap.Invalidate();

        wrap.Controls.Add(tb);
        return (wrap, tb);
    }

    public static TextBox Texto(int left, int top, int width, bool right = false, string? placeholder = null)
    {
        // Mantém compatibilidade — retorna o TextBox; o wrapper fica sem parent
        var (_, tb) = TextoArredondado(0, 0, width, right, placeholder);
        tb.Left  = left;
        tb.Top   = top;
        tb.Width = width;
        tb.BorderStyle = BorderStyle.FixedSingle;   // posicionado direto (sem wrapper)
        return tb;
    }

    /// <summary>
    /// Adiciona label + TextBox arredondado no parent. Retorna o TextBox.
    /// </summary>
    public static TextBox CampoTexto(Control parent, string label, int left, int top, int width,
                                      bool right = false, string? placeholder = null)
    {
        parent.Controls.Add(Rotulo(label, left, top, width));
        var (wrap, tb) = TextoArredondado(left, top + 20, width, right, placeholder);
        parent.Controls.Add(wrap);
        return tb;
    }

    public static ComboBox CampoCombo(Control parent, string label, int left, int top, int width)
    {
        parent.Controls.Add(Rotulo(label, left, top, width));
        var cb = new ComboBox
        {
            Left = left, Top = top + 20, Width = width,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font      = new Font(Tema.FontFamily, 10),
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
        // Usa Region arredondada no Panel → clips todos os filhos ao formato pill.
        // Elimina o retângulo duplo completamente: os cantos mostram o fundo do pai.
        const int raio = 6;
        var pnl = new Panel { BackColor = Tema.CorCard, Padding = new Padding(0, 2, 10, 2) };

        void AtualizarRegion()
        {
            if (pnl.Width <= 0 || pnl.Height <= 0) return;
            pnl.Region = new Region(Tema.PathArredondado(
                new Rectangle(0, 0, pnl.Width, pnl.Height), raio));
        }
        pnl.HandleCreated += (_, _) => AtualizarRegion();
        pnl.Resize         += (_, _) => AtualizarRegion();

        pnl.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Borda — o Region já garante os cantos arredondados
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
            using var path = Tema.PathArredondado(rect, raio);
            using var pen  = new Pen(Tema.CorBorda, 1f);
            g.DrawPath(pen, path);
        };

        var lblIcone = new Label
        {
            Text      = Tema.IconBusca,
            Dock      = DockStyle.Left, Width = 40,
            Font      = new Font("Segoe MDL2 Assets", 12),
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Tema.CorCard
        };
        var tb = new TextBox
        {
            Dock        = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font        = new Font(Tema.FontFamily, 10),
            BackColor   = Tema.CorCard,
            PlaceholderText = placeholder
        };
        // Adiciona TextBox antes do ícone (docking: último adicionado = prioridade maior)
        pnl.Controls.Add(tb);
        pnl.Controls.Add(lblIcone);
        return (pnl, tb);
    }

    /// <summary>
    /// Header padrão de página (título + subtítulo).
    /// </summary>
    public static Panel HeaderPagina(string titulo, string subtitulo, int height = 72)
    {
        var header = new Panel { Dock = DockStyle.Top, Height = height, BackColor = Tema.CorFundo, Padding = new Padding(6, 2, 0, 8) };
        var acento = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Tema.CorPrimaria };
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(10, 0, 0, 0) };
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
        corpo.Controls.Add(linha);
        corpo.Controls.Add(lblSub);
        corpo.Controls.Add(lblTitulo);
        header.Controls.Add(corpo);
        header.Controls.Add(acento);
        return header;
    }

    public static Label SubtituloHeader(Panel header) =>
        header.Controls.Find("HeaderSubtitulo", true).OfType<Label>().FirstOrDefault()
        ?? throw new InvalidOperationException("Header de pagina sem subtitulo.");

    /// <summary>
    /// Header de tela modal/dialog.
    /// </summary>
    public static Panel HeaderModal(string titulo)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Tema.CorFundo, Padding = new Padding(6, 0, 0, 0) };
        var acento = new Panel { Dock = DockStyle.Left, Width = 4, BackColor = Tema.CorPrimaria };
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(10, 0, 0, 0) };
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
        corpo.Controls.Add(lblTitulo);
        corpo.Controls.Add(linha);
        p.Controls.Add(corpo);
        p.Controls.Add(acento);
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
