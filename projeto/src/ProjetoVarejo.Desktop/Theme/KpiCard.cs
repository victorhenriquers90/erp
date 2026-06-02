using System.Drawing.Drawing2D;

namespace ProjetoVarejo.Desktop.Theme;

public class KpiCard : Card
{
    private readonly string _titulo;
    private string _valor;
    private readonly string _icone;
    private readonly Color _corAcento;
    private decimal? _variacaoPct;

    public KpiCard(string titulo, string valor, string icone, Color corAcento, decimal? variacaoPct = null)
    {
        _titulo = titulo;
        _valor = valor;
        _icone = icone;
        _corAcento = corAcento;
        _variacaoPct = variacaoPct;

        Width = 224;
        Height = 124;
        Padding = new Padding(16, 14, 16, 12);

        Paint += DesenharConteudo;
    }

    public void AtualizarValor(string novoValor, decimal? variacao = null)
    {
        _valor = novoValor;
        _variacaoPct = variacao;
        Invalidate();
    }

    private void DesenharConteudo(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var contentInset = 3;
        var contentLeft = Padding.Left + contentInset;
        var contentRight = Width - Padding.Right - contentInset;
        var contentTop = Padding.Top + contentInset;

        using (var stripe = new SolidBrush(_corAcento))
        {
            g.FillRectangle(stripe, 2, 2, Width - 5, 3);
        }

        using (var brush = new SolidBrush(Color.FromArgb(30, _corAcento)))
        {
            var iconRect = new Rectangle(contentRight - 36, contentTop, 32, 32);
            using var iconPath = Tema.PathArredondado(iconRect, Tema.RaioBotao);
            g.FillPath(brush, iconPath);
            using var fontIcone = new Font("Segoe MDL2 Assets", 16);
            TextRenderer.DrawText(g, _icone, fontIcone, iconRect, _corAcento,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        using var fontTitulo = new Font(Tema.FontFamily, 8, FontStyle.Bold);
        TextRenderer.DrawText(g, _titulo.ToUpper(), fontTitulo,
            new Rectangle(contentLeft, contentTop + 2, Width / 2 + 18, 18),
            Tema.CorTextoMedio, TextFormatFlags.Left);

        // Largura disponível para o valor evita colidir com o ícone à direita
        var valorWidth = Width - Padding.Left - Padding.Right - 50;
        using var fontValor = new Font(Tema.FontFamily, 18, FontStyle.Bold);
        TextRenderer.DrawText(g, _valor, fontValor,
            new Rectangle(contentLeft, contentTop + 30, valorWidth, 38),
            Tema.CorTextoEscuro, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

        if (_variacaoPct.HasValue)
        {
            var v = _variacaoPct.Value;
            var positivo = v >= 0;
            var cor = positivo ? Tema.CorSucesso : Tema.CorErro;
            var icone = positivo ? Tema.IconArrowUp : Tema.IconArrowDown;
            var prefixo = positivo ? "+" : "";
            var texto = $"{prefixo}{v:F1}%";

            using var fontVarIcon = new Font("Segoe MDL2 Assets", 9);
            using var fontVar = new Font(Tema.FontFamily, 9, FontStyle.Bold);
            using var fontHelper = new Font(Tema.FontFamily, 9);

            var iconSize = TextRenderer.MeasureText(g, icone, fontVarIcon);
            var textSize = TextRenderer.MeasureText(g, texto, fontVar);
            var labelText = " vs período anterior";

            // Variação fica em linha própria abaixo do valor — não sobrepõe.
            // Usa caracteres ASCII ▲/▼ que renderizam em qualquer fonte (em vez de glyphs MDL2 PUA).
            var setaTxt = positivo ? "▲" : "▼";
            using var fontSeta = new Font(Tema.FontFamily, 9, FontStyle.Bold);
            var setaSize = TextRenderer.MeasureText(g, setaTxt, fontSeta);

            var y = Height - Padding.Bottom - 20;
            TextRenderer.DrawText(g, setaTxt, fontSeta, new Point(contentLeft, y), cor);
            TextRenderer.DrawText(g, " " + texto, fontVar, new Point(contentLeft + setaSize.Width, y), cor);
            TextRenderer.DrawText(g, labelText, fontHelper,
                new Point(contentLeft + setaSize.Width + textSize.Width + 6, y), Tema.CorTextoMedio);
        }
    }
}

public class LineChart : Control
{
    private List<decimal> _valores = new();
    private List<string> _rotulos = new();
    public Color CorLinha { get; set; } = Tema.CorPrimaria;
    public Color CorArea { get; set; } = Color.FromArgb(34, 10, 110, 209);
    public string Titulo { get; set; } = "";

    public LineChart()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        BackColor = Tema.CorCard;
    }

    public void DefinirDados(List<decimal> valores, List<string> rotulos)
    {
        _valores = valores ?? new();
        _rotulos = rotulos ?? new();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (!string.IsNullOrWhiteSpace(Titulo))
        {
            using var fonteT = new Font(Tema.FontFamily, 10, FontStyle.Bold);
            TextRenderer.DrawText(g, Titulo, fonteT, new Point(0, 0), Tema.CorTextoEscuro);
        }

        var top = string.IsNullOrWhiteSpace(Titulo) ? 10 : 30;
        var bottom = Height - 30;
        var left = 50;
        var right = Width - 20;

        using var penEixo = new Pen(Tema.CorBorda, 1);
        g.DrawLine(penEixo, left, bottom, right, bottom);

        if (_valores.Count == 0)
        {
            using var fonteVazio = new Font(Tema.FontFamily, 10, FontStyle.Italic);
            TextRenderer.DrawText(g, "Sem dados no período", fonteVazio,
                new Rectangle(left, top, right - left, bottom - top),
                Tema.CorTextoClaro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            return;
        }

        var min = (float)_valores.Min();
        var max = (float)_valores.Max();
        if (max == min) max = min + 1;

        using var penGrid = new Pen(Tema.CorBordaSuave, 1);
        for (int i = 1; i <= 3; i++)
        {
            var y = top + (bottom - top) * i / 4;
            g.DrawLine(penGrid, left, y, right, y);
            var valorRef = max - (max - min) * i / 4f;
            using var fonteY = new Font(Tema.FontFamily, 8);
            TextRenderer.DrawText(g, valorRef.ToString("N0"), fonteY, new Point(5, y - 8), Tema.CorTextoClaro);
        }

        var pontos = new PointF[_valores.Count];
        for (int i = 0; i < _valores.Count; i++)
        {
            var x = left + (right - left) * (_valores.Count == 1 ? 0.5f : (float)i / (_valores.Count - 1));
            var y = bottom - (bottom - top) * ((float)_valores[i] - min) / (max - min);
            pontos[i] = new PointF(x, y);
        }

        if (pontos.Length > 1)
        {
            var area = new List<PointF>(pontos)
            {
                new(pontos[^1].X, bottom),
                new(pontos[0].X, bottom)
            };
            using var brushArea = new SolidBrush(CorArea);
            g.FillPolygon(brushArea, area.ToArray());
        }

        using var penLinha = new Pen(CorLinha, 2.5f) { LineJoin = LineJoin.Round };
        if (pontos.Length >= 2) g.DrawLines(penLinha, pontos);

        for (int i = 0; i < pontos.Length; i++)
        {
            using var brushFora = new SolidBrush(Tema.CorCard);
            using var brushDentro = new SolidBrush(CorLinha);
            g.FillEllipse(brushDentro, pontos[i].X - 4, pontos[i].Y - 4, 8, 8);
            g.FillEllipse(brushFora, pontos[i].X - 2, pontos[i].Y - 2, 4, 4);
        }

        var step = Math.Max(1, _valores.Count / 6);
        using var fonteX = new Font(Tema.FontFamily, 8);
        for (int i = 0; i < pontos.Length; i += step)
        {
            if (i < _rotulos.Count)
            {
                TextRenderer.DrawText(g, _rotulos[i], fonteX, new Point((int)pontos[i].X - 20, bottom + 5),
                    Tema.CorTextoClaro);
            }
        }
    }
}
