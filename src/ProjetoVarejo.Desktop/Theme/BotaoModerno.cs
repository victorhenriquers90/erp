using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ProjetoVarejo.Desktop.Theme;

public class BotaoModerno : Button
{
    private bool _hover;
    private bool _press;

    public enum Variante { Primario, Sucesso, Ghost, Perigo, Aviso, Toggle }

    public Variante Estilo { get; set; } = Variante.Ghost;

    public bool Toggled
    {
        get => Tag is bool b && b;
        set { Tag = value; Invalidate(); }
    }

    public BotaoModerno()
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor,
            true);

        BackColor = Color.Transparent;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.BorderColor = Tema.CorBorda;
        FlatAppearance.MouseOverBackColor = Tema.CorFundo;
        FlatAppearance.MouseDownBackColor = Tema.CorFundo;
        UseVisualStyleBackColor = false;
        Font = Tema.FontPequenaBold;
        Cursor = Cursors.Hand;
        Height = 40;
        Margin = new Padding(8, 0, 0, 0);
        TabStop = true;
        AutoEllipsis = true;
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hover = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hover = false;
        _press = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _press = true;
            Invalidate();
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _press = false;
            Invalidate();
        }

        base.OnMouseUp(e);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Intencional vazio — o fundo é tratado em OnPaint com g.Clear()
        // para garantir que o anti-aliasing do arredondado blendie corretamente.
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;

        // ── Passo 1: Limpa com cor real do pai ──────────────────────────────
        // ESSENCIAL: sem isso, o anti-aliasing blendia as bordas do arco com
        // preto (GDI não inicializado), criando o contorno escuro.
        g.Clear(ResolverFundoPai());

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        var (baseBg, baseFg, border) = Cores();
        var bg = AjustarFundo(baseBg, baseFg);
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        using var path = Tema.PathArredondado(rect, Tema.RaioBotao);
        using (var fill = CriarBrushFundo(rect, bg))
            g.FillPath(fill, path);

        if (border != Color.Transparent)
        {
            using var pen = new Pen(border, IsSolido ? 0.8f : 1f);
            g.DrawPath(pen, path);
        }

        if (IsSolido)
        {
            using var bevel = new Pen(Tema.Misturar(bg, Color.Black, 0.16f), 1f);
            g.DrawLine(bevel, rect.X + Tema.RaioBotao, rect.Bottom - 1, rect.Right - Tema.RaioBotao, rect.Bottom - 1);
        }

        var textRect = new Rectangle(rect.X + 14, rect.Y, rect.Width - 28, rect.Height);
        TextRenderer.DrawText(g, Text ?? "", Font, textRect, baseFg,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding |
            TextFormatFlags.NoPrefix);

        if (Focused && ShowFocusCues)
        {
            using var focus = new Pen(Color.FromArgb(110, baseFg), 1f) { DashStyle = DashStyle.Dot };
            var focusRect = new Rectangle(rect.X + 3, rect.Y + 3, rect.Width - 6, rect.Height - 6);
            using var focusPath = Tema.PathArredondado(focusRect, Math.Max(1, Tema.RaioBotao - 2));
            g.DrawPath(focus, focusPath);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        if (Estilo == Variante.Toggle)
            Toggled = !Toggled;

        base.OnClick(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        Invalidate();
        base.OnTextChanged(e);
    }

    private (Color bg, Color fg, Color border) Cores()
    {
        if (!Enabled)
            return (Tema.CorBordaSuave, Tema.CorTextoClaro, Tema.CorBorda);

        if (Estilo == Variante.Toggle)
        {
            return Toggled
                ? (Tema.CorPrimariaSoft, Tema.CorPrimaria, Tema.CorPrimaria)
                : (Tema.CorCard, Tema.CorTextoMedio, Tema.CorBorda);
        }

        return Estilo switch
        {
            Variante.Primario => (Tema.CorPrimaria, Color.White, Color.Transparent),
            Variante.Sucesso => (Tema.CorSucesso, Color.White, Color.Transparent),
            Variante.Perigo => (Tema.CorCard, Tema.CorErro, Tema.CorErro),
            Variante.Aviso => (Tema.CorCard, Tema.CorAlerta, Tema.CorAlerta),
            _ => (Tema.CorCard, Tema.CorPrimaria, Tema.CorPrimaria)
        };
    }

    private Color ResolverFundoPai()
    {
        var ctrl = Parent;
        while (ctrl != null)
        {
            var c = ctrl.BackColor;
            if (c != Color.Transparent && c.A > 0)
                return c;
            ctrl = ctrl.Parent;
        }
        return Tema.CorFundo;
    }

    private bool IsSolido => Enabled && (Estilo is Variante.Primario or Variante.Sucesso);

    private Color AjustarFundo(Color bg, Color fg)
    {
        if (_press)
            return IsSolido ? Tema.Misturar(bg, Color.Black, 0.18f) : Tema.Misturar(bg, fg, 0.12f);
        if (_hover)
            return IsSolido ? Tema.Misturar(bg, Color.White, 0.10f) : Tema.Misturar(bg, fg, 0.06f);
        return bg;
    }

    private Brush CriarBrushFundo(Rectangle rect, Color bg)
    {
        // Guard: LinearGradientBrush falha com dimensão zero
        if (rect.Width <= 1 || rect.Height <= 1)
            return new SolidBrush(bg);

        if (IsSolido)
        {
            var topo = Tema.Misturar(bg, Color.White, 0.13f);
            var baixo = Tema.Misturar(bg, Color.Black, 0.08f);
            return new LinearGradientBrush(rect, topo, baixo, LinearGradientMode.Vertical);
        }

        var fundoTopo  = Tema.Misturar(bg, Color.White, 0.60f);
        var fundoBaixo = Tema.Misturar(bg, Tema.CorBordaSuave, 0.18f);
        return new LinearGradientBrush(rect, fundoTopo, fundoBaixo, LinearGradientMode.Vertical);
    }
}
