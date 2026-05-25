using System.Drawing.Drawing2D;

namespace ProjetoVarejo.Desktop.Theme;

/// <summary>
/// Topbar moderna com search central, notificações, avatar dropdown.
/// </summary>
public class Topbar : Panel
{
    private readonly Label _avatar;
    private readonly Label _badge;
    private readonly Label _lblUsuario;
    private readonly Label _lblEmpresa;
    public TextBox SearchBox { get; }

    public Action? OnSearch { get; set; }
    public Action? OnLogout { get; set; }
    public Action? OnNotificacoes { get; set; }

    public Topbar(string nomeUsuario, string nomeEmpresa, int notificacoes = 0)
    {
        Dock = DockStyle.Top;
        Height = Tema.AlturaTopbar;
        BackColor = Tema.ShellBarFundo;

        // === Empresa (esquerda) ===
        _lblEmpresa = new Label
        {
            Text = string.IsNullOrWhiteSpace(nomeEmpresa) ? Tema.NomeProduto : nomeEmpresa,
            Dock = DockStyle.Left,
            Width = 310,
            Font = new Font(Tema.FontFamily, 10, FontStyle.Bold),
            ForeColor = Tema.ShellBarTexto,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0)
        };

        // === Lado direito: avatar + notif ===
        var right = new Panel { Dock = DockStyle.Right, Width = 316, BackColor = Tema.ShellBarFundo };

        // Avatar
        _avatar = new Label
        {
            Text = ObterIniciais(nomeUsuario),
            Width = 38, Height = 38,
            Font = new Font(Tema.FontFamily, 10, FontStyle.Bold),
            ForeColor = Tema.Branco,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Top = 10, Left = right.Width - 56
        };
        _avatar.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _avatar.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Tema.CorPrimariaLight);
            g.FillEllipse(brush, 0, 0, _avatar.Width - 1, _avatar.Height - 1);
            TextRenderer.DrawText(g, _avatar.Text, _avatar.Font, _avatar.ClientRectangle, _avatar.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        _avatar.Click += (s, e) => MostrarMenuUsuario();

        // Nome usuário
        _lblUsuario = new Label
        {
            Text = nomeUsuario,
            Top = 18, Left = right.Width - 226, Width = 158,
            Font = new Font(Tema.FontFamily, 9, FontStyle.Bold),
            ForeColor = Tema.ShellBarTexto,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleRight,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };

        // Notificação (bell + badge)
        var bell = new Label
        {
            Text = Tema.IconNotificacao,
            Width = 38, Height = 38,
            Font = new Font("Segoe MDL2 Assets", 14),
            ForeColor = Tema.ShellBarTextoSuave,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Top = 9, Left = right.Width - 110,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        bell.MouseEnter += (s, e) => bell.ForeColor = Tema.ShellBarTexto;
        bell.MouseLeave += (s, e) => bell.ForeColor = Tema.ShellBarTextoSuave;
        bell.Click += (s, e) => OnNotificacoes?.Invoke();

        _badge = new Label
        {
            Text = notificacoes > 9 ? "9+" : notificacoes.ToString(),
            Width = 18, Height = 18,
            Font = new Font(Tema.FontFamily, 7, FontStyle.Bold),
            ForeColor = Tema.Branco,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Top = 10, Left = right.Width - 88,
            Visible = notificacoes > 0,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        _badge.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Tema.CorErro);
            g.FillEllipse(brush, 0, 0, _badge.Width - 1, _badge.Height - 1);
            TextRenderer.DrawText(g, _badge.Text, _badge.Font, _badge.ClientRectangle, _badge.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        right.Controls.Add(_badge);
        right.Controls.Add(_avatar);
        right.Controls.Add(_lblUsuario);
        right.Controls.Add(bell);

        // === Search central ===
        var pnlSearch = new Panel { Dock = DockStyle.Fill, BackColor = Tema.ShellBarFundo, Padding = new Padding(22, 11, 22, 11) };
        var searchContainer = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Tema.Branco,
            Padding = new Padding(2)
        };
        searchContainer.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = Tema.PathArredondado(new Rectangle(0, 0, searchContainer.Width - 1, searchContainer.Height - 1), Tema.RaioBotao);
            using var brush = new SolidBrush(Tema.Branco);
            g.FillPath(brush, path);
            using var pen = new Pen(Color.FromArgb(96, 148, 194), 1);
            g.DrawPath(pen, path);
        };
        var lblBusca = new Label
        {
            Text = Tema.IconBusca,
            Dock = DockStyle.Left,
            Width = 36,
            Font = new Font("Segoe MDL2 Assets", 12),
            ForeColor = Tema.CorPrimaria,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        SearchBox = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font(Tema.FontFamily, 10),
            BackColor = Tema.Branco,
            ForeColor = Tema.CorTextoEscuro,
            PlaceholderText = "Buscar produto, cliente, venda..."
        };
        SearchBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) OnSearch?.Invoke(); };

        var searchInner = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Branco, Padding = new Padding(0, 7, 14, 0) };
        searchInner.Controls.Add(SearchBox);
        searchContainer.Controls.Add(searchInner);
        searchContainer.Controls.Add(lblBusca);
        pnlSearch.Controls.Add(searchContainer);

        Controls.Add(pnlSearch);
        Controls.Add(right);
        Controls.Add(_lblEmpresa);

        // Borda inferior
        Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(19, 78, 132), 1);
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        };
    }

    private static string ObterIniciais(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) return "?";
        var partes = nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 1) return partes[0][..Math.Min(2, partes[0].Length)].ToUpper();
        return $"{partes[0][0]}{partes[^1][0]}".ToUpper();
    }

    private void MostrarMenuUsuario()
    {
        var menu = new ContextMenuStrip
        {
            Font = new Font(Tema.FontFamily, 10),
            ShowImageMargin = false,
            BackColor = Tema.Branco,
            Renderer = new ToolStripProfessionalRenderer(new MenuColorsModerno())
        };
        var miPerfil = new ToolStripMenuItem("  Meu perfil") { ForeColor = Tema.CorTextoEscuro };
        var miSair = new ToolStripMenuItem("  Sair") { ForeColor = Tema.CorErro };
        miSair.Click += (s, e) => OnLogout?.Invoke();
        menu.Items.Add(miPerfil);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(miSair);
        menu.Show(_avatar, new Point(_avatar.Width - 180, _avatar.Height + 4));
    }

    private class MenuColorsModerno : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Tema.CorPrimariaSoft;
        public override Color MenuItemSelectedGradientBegin => Tema.CorPrimariaSoft;
        public override Color MenuItemSelectedGradientEnd => Tema.CorPrimariaSoft;
        public override Color MenuItemBorder => Tema.CorPrimariaSoft;
        public override Color ToolStripDropDownBackground => Tema.Branco;
        public override Color ImageMarginGradientBegin => Tema.Branco;
        public override Color ImageMarginGradientMiddle => Tema.Branco;
        public override Color ImageMarginGradientEnd => Tema.Branco;
    }
}
