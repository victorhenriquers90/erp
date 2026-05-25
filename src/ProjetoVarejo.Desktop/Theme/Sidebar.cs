using System.Drawing.Drawing2D;

namespace ProjetoVarejo.Desktop.Theme;

public class SidebarItem
{
    public string Icone { get; set; } = "";
    public string Texto { get; set; } = "";
    public Action OnClick { get; set; } = () => { };
}

public class SidebarSecao
{
    public string Titulo { get; set; } = "";
    public List<SidebarItem> Itens { get; set; } = new();
}

public class Sidebar : Panel
{
    private readonly List<SidebarBotao> _botoes = new();
    private SidebarBotao? _ativo;

    public Sidebar(List<SidebarSecao> secoes, string? marcaTexto = null, string? marcaIcone = null)
    {
        Dock = DockStyle.Left;
        Width = Tema.LarguraSidebar;
        BackColor = Tema.SidebarFundo;
        AutoScroll = true;

        if (marcaTexto != null)
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Tema.SidebarFundo, Padding = new Padding(16, 12, 12, 10) };
            var icone = new Label
            {
                Text = string.IsNullOrWhiteSpace(marcaIcone) ? "PV" : marcaIcone,
                Dock = DockStyle.Left,
                Width = 42,
                Font = string.IsNullOrWhiteSpace(marcaIcone)
                    ? new Font(Tema.FontFamily, 10, FontStyle.Bold)
                    : new Font("Segoe MDL2 Assets", 20),
                ForeColor = Tema.Branco,
                BackColor = Tema.CorPrimaria,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0, 0, 10, 0)
            };
            var marca = new Panel { Dock = DockStyle.Fill, BackColor = Tema.SidebarFundo, Padding = new Padding(10, 2, 0, 0) };
            var texto = new Label
            {
                Text = marcaTexto,
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font(Tema.FontFamily, 13, FontStyle.Bold),
                ForeColor = Tema.SidebarTextoAtivo,
                TextAlign = ContentAlignment.BottomLeft
            };
            var subtitulo = new Label
            {
                Text = "ERP de varejo",
                Dock = DockStyle.Top,
                Height = 20,
                Font = Tema.FontPequena,
                ForeColor = Tema.SidebarTexto,
                TextAlign = ContentAlignment.TopLeft
            };
            marca.Controls.Add(subtitulo);
            marca.Controls.Add(texto);
            header.Controls.Add(marca);
            header.Controls.Add(icone);

            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Color.FromArgb(42, 56, 75) };
            Controls.Add(sep);
            Controls.Add(header);
        }

        // Empilhar invertido (Dock.Top adiciona de cima pra baixo na ordem reversa)
        var pilha = new List<Control>();
        foreach (var secao in secoes)
        {
            if (pilha.Count > 0)
                pilha.Add(new Panel { Dock = DockStyle.Top, Height = 12, BackColor = Tema.SidebarFundo });

            if (!string.IsNullOrWhiteSpace(secao.Titulo))
            {
                var lblSecao = new Label
                {
                    Text = secao.Titulo.ToUpper(),
                    Dock = DockStyle.Top,
                    Height = 28,
                    Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
                    ForeColor = Tema.SidebarSecao,
                    BackColor = Tema.SidebarFundo,
                    Padding = new Padding(22, 8, 0, 0),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                pilha.Add(lblSecao);
            }

            foreach (var item in secao.Itens)
            {
                var btn = new SidebarBotao(item);
                btn.Click += (s, e) =>
                {
                    DestacarAtivo(btn);
                    item.OnClick();
                };
                _botoes.Add(btn);
                pilha.Add(btn);
            }
        }

        for (int i = pilha.Count - 1; i >= 0; i--)
            Controls.Add(pilha[i]);

        if (_botoes.Count > 0)
            DestacarAtivo(_botoes[0]);
    }

    public void DestacarAtivo(SidebarBotao? btn)
    {
        if (_ativo != null) _ativo.Ativo = false;
        _ativo = btn;
        if (btn != null) btn.Ativo = true;
    }
}

public class SidebarBotao : Control
{
    private readonly SidebarItem _item;
    private bool _hover;
    private bool _ativo;

    public bool Ativo
    {
        get => _ativo;
        set { _ativo = value; Invalidate(); }
    }

    public SidebarBotao(SidebarItem item)
    {
        _item = item;
        Dock = DockStyle.Top;
        Height = 40;
        Cursor = Cursors.Hand;
        BackColor = Tema.SidebarFundo;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color fundoCor = _ativo ? Tema.SidebarAtivo : (_hover ? Tema.SidebarHover : Tema.SidebarFundo);
        if (_ativo || _hover)
        {
            var pillRect = new Rectangle(12, 3, Width - 24, Height - 6);
            using var path = Tema.PathArredondado(pillRect, Tema.RaioBotao);
            using var brush = new SolidBrush(fundoCor);
            g.FillPath(brush, path);
        }

        if (_ativo)
        {
            using var brush = new SolidBrush(Tema.CorPrimariaLight);
            g.FillRectangle(brush, 0, 7, 3, Height - 14);
        }

        var corTexto = _ativo ? Tema.SidebarTextoAtivo : Tema.SidebarTexto;
        using var fontIcone = new Font("Segoe MDL2 Assets", 13);
        var iconRect = new Rectangle(22, 0, 30, Height);
        TextRenderer.DrawText(g, _item.Icone, fontIcone, iconRect, corTexto,
            TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

        using var fontTexto = new Font(Tema.FontFamily, 10, _ativo ? FontStyle.Bold : FontStyle.Regular);
        var textRect = new Rectangle(58, 0, Width - 68, Height);
        TextRenderer.DrawText(g, _item.Texto, fontTexto, textRect, corTexto,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }
}
