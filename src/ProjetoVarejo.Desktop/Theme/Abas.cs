using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ProjetoVarejo.Desktop.Theme;

public static class Abas
{
    public static void Modernizar(TabControl tc)
    {
        tc.DrawMode = TabDrawMode.OwnerDrawFixed;
        tc.SizeMode = TabSizeMode.Fixed;
        tc.ItemSize = new Size(210, 42);
        tc.Appearance = TabAppearance.Normal;
        tc.Padding = new Point(16, 6);

        tc.DrawItem += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var tab = tc.TabPages[e.Index];
            var selected = e.Index == tc.SelectedIndex;
            var rect = tc.GetTabRect(e.Index);
            using (var area = new SolidBrush(Tema.CorFundo))
                g.FillRectangle(area, rect);

            var pill = Rectangle.Inflate(rect, -3, -4);
            pill.Height -= 1;
            var bgTop = selected ? Color.White : Tema.CorFundo;
            var bgBottom = selected ? Tema.CorCardAlt : Tema.CorFundo;
            var fg = selected ? Tema.CorPrimaria : Tema.CorTextoMedio;

            using var path = Tema.PathArredondado(pill, Tema.RaioBotao);
            using (var fill = new LinearGradientBrush(pill, bgTop, bgBottom, LinearGradientMode.Vertical))
                g.FillPath(fill, path);

            if (selected)
            {
                using var pen = new Pen(Tema.CorBorda, 1);
                g.DrawPath(pen, path);

                using var accent = new SolidBrush(Tema.CorPrimaria);
                g.FillRectangle(accent, pill.X + 12, pill.Bottom - 3, pill.Width - 24, 3);
            }

            using var font = new Font(Tema.FontFamily, 9.5f, selected ? FontStyle.Bold : FontStyle.Regular);
            TextRenderer.DrawText(g, tab.Text, font, pill, fg,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        };
    }
}
