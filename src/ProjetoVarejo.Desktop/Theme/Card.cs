using System.Drawing.Drawing2D;

namespace ProjetoVarejo.Desktop.Theme;

/// <summary>
/// Container com cantos arredondados, sombra suave e borda.
/// Estilo card moderno tipo Conta Azul / Bling.
/// </summary>
public class Card : Panel
{
    public int Raio { get; set; } = Tema.RaioCard;
    public bool ComSombra { get; set; } = true;
    public Color CorBorda { get; set; } = Tema.CorBorda;

    public Card()
    {
        BackColor = Color.Transparent;
        DoubleBuffered = true;
        Padding = new Padding(Tema.Espacamento);
        // Espaço extra para a sombra
        Margin = new Padding(4);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // Área do card (encolhe para deixar espaço pra sombra)
        var inset = ComSombra ? 4 : 0;
        var rect = new Rectangle(inset, inset, Width - inset * 2 - 1, Height - inset * 2 - 1);

        if (ComSombra)
        {
            for (var i = 4; i >= 1; i--)
            {
                var sombra = new Rectangle(rect.X - i, rect.Y + i, rect.Width + i * 2, rect.Height + i);
                using var shadowPath = Tema.PathArredondado(sombra, Raio + i);
                using var shadowBrush = new SolidBrush(Color.FromArgb(3 + i * 3, 0, 0, 0));
                g.FillPath(shadowBrush, shadowPath);
            }
        }

        using var path = Tema.PathArredondado(rect, Raio);
        using (var brush = new LinearGradientBrush(rect, Color.White, Tema.CorCardAlt, LinearGradientMode.Vertical))
            g.FillPath(brush, path);

        using (var highlight = new Pen(Color.FromArgb(170, Color.White), 1))
            g.DrawLine(highlight, rect.X + Raio, rect.Y + 1, rect.Right - Raio, rect.Y + 1);

        using var pen = new Pen(CorBorda, 1);
        g.DrawPath(pen, path);

        base.OnPaint(e);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Pinta com a cor real do fundo (pai ou CorFundo como fallback)
        // Evita área preta quando Parent.BackColor é Transparent ou não está disponível
        var bgColor = Parent?.BackColor ?? Tema.CorFundo;
        if (bgColor == Color.Transparent || bgColor.A == 0)
            bgColor = Tema.CorFundo;
        using var brush = new SolidBrush(bgColor);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}
