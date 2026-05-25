namespace ProjetoVarejo.Desktop.Theme;

/// <summary>
/// Fábrica de botões padronizados, todos seguem o tema.
/// </summary>
public static class Botoes
{
    public static Button Primario(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorPrimaria, Tema.Branco);

    public static Button PrimarioIcone(string texto, string icone, int width = 132, int height = 34)
        => ComIcone(Primario(texto, width, height), icone);

    public static Button Sucesso(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorSucesso, Tema.Branco);

    public static Button SucessoIcone(string texto, string icone, int width = 132, int height = 34)
        => ComIcone(Sucesso(texto, width, height), icone);

    public static Button Perigo(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorErro, Tema.Branco);

    public static Button PerigoIcone(string texto, string icone, int width = 132, int height = 34)
        => ComIcone(Perigo(texto, width, height), icone);

    public static Button Aviso(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorAlerta, Tema.Branco);

    public static Button Info(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorInfo, Tema.Branco);

    public static Button InfoIcone(string texto, string icone, int width = 132, int height = 34)
        => ComIcone(Info(texto, width, height), icone);

    public static Button Secundario(string texto, int width = 132, int height = 34)
        => Construir(texto, width, height, Tema.CorCardAlt, Tema.CorTextoEscuro);

    public static Button Ghost(string texto, int width = 132, int height = 34)
    {
        var b = Construir(texto, width, height, Color.Transparent, Tema.CorPrimaria);
        b.FlatAppearance.BorderColor = Tema.CorPrimaria;
        b.FlatAppearance.BorderSize = 1;
        return b;
    }

    public static Button GhostIcone(string texto, string icone, int width = 132, int height = 34, Color? cor = null)
    {
        var b = Ghost(texto, width, height);
        if (cor.HasValue)
        {
            b.ForeColor = cor.Value;
            b.FlatAppearance.BorderColor = cor.Value;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, cor.Value);
        }

        return ComIcone(b, icone);
    }

    public static void ParaToolbar(params Button[] botoes)
    {
        foreach (var botao in botoes)
        {
            botao.Height = 40;
            botao.Margin = new Padding(6, 0, 0, 0);
            botao.AutoEllipsis = true;
        }
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
            UseVisualStyleBackColor = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(10, 0, 12, 0)
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

    private static Button ComIcone(Button botao, string icone)
    {
        botao.Image = CriarIcone(icone, botao.ForeColor, 16);
        botao.ImageAlign = ContentAlignment.MiddleLeft;
        botao.TextAlign = ContentAlignment.MiddleCenter;
        botao.TextImageRelation = TextImageRelation.ImageBeforeText;
        return botao;
    }

    private static Image CriarIcone(string icone, Color cor, int tamanho)
    {
        var bmp = new Bitmap(tamanho, tamanho);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        using var fonte = Tema.FontIcone(tamanho - 2);
        TextRenderer.DrawText(g, icone, fonte, new Rectangle(0, 0, tamanho, tamanho), cor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        return bmp;
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
