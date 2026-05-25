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
        var inset = ComSombra ? 2 : 0;
        var rect = new Rectangle(inset, inset, Width - inset * 2 - 1, Height - inset * 2 - 1);

        if (ComSombra)
        {
            Tema.DesenharSombra(g, rect, Raio, distancia: 1, profundidade: 2);
        }

        using var path = Tema.PathArredondado(rect, Raio);
        using var brush = new SolidBrush(Tema.CorCard);
        g.FillPath(brush, path);

        using var pen = new Pen(CorBorda, 1);
        g.DrawPath(pen, path);

        base.OnPaint(e);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Não chamar base — evita o flicker do BackColor sólido
        if (Parent != null)
        {
            using var brush = new SolidBrush(Parent.BackColor);
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }
}
