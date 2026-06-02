namespace ProjetoVarejo.Desktop.Theme;

/// <summary>
/// Fábrica de botões padronizados, todos seguem o tema.
/// </summary>
public static class Botoes
{
    public static Button Primario(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorPrimaria, Tema.Branco);

    public static Button Sucesso(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorSucesso, Tema.Branco);

    public static Button Perigo(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorErro, Tema.Branco);

    public static Button Aviso(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorAlerta, Tema.Branco);

    public static Button Info(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorInfo, Tema.Branco);

    public static Button Secundario(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorCardAlt, Tema.CorTextoEscuro);

    public static Button Ghost(string texto, int width = 132, int height = 34)
    {
        var b = Construir(texto, width, height, Color.Transparent, Tema.CorPrimaria);
        b.FlatAppearance.BorderColor = Tema.CorPrimaria;
        b.FlatAppearance.BorderSize = 1;
        return b;
    }

    private static Button Construir(string texto, int width, int height, Color bg, Color fg)
    {
        var b = new Button
        {
            Text = texto,
            Width = width,
            Height = height,
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Font = new Font(Tema.FontFamily, 9, FontStyle.Bold),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = bg == Tema.CorCardAlt ? 1 : 0;
        b.FlatAppearance.BorderColor = Tema.CorBorda;
        if (bg != Color.Transparent)
        {
            b.FlatAppearance.MouseOverBackColor = Misturar(bg, Color.Black, 0.10f);
            b.FlatAppearance.MouseDownBackColor = Misturar(bg, Color.Black, 0.20f);
        }
        else
        {
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, fg);
        }
        return b;
    }

    public static Color Misturar(Color a, Color b, float pct)
    {
        pct = Math.Clamp(pct, 0f, 1f);
        return Color.FromArgb(
            (int)(a.R * (1 - pct) + b.R * pct),
            (int)(a.G * (1 - pct) + b.G * pct),
            (int)(a.B * (1 - pct) + b.B * pct));
    }
}
