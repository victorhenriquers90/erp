namespace ProjetoVarejo.Desktop.Theme;

/// <summary>
/// Fabrica central de botoes do sistema.
/// Usa desenho proprio para evitar glitches de componentes externos e glyphs quebrados.
/// </summary>
public static class Botoes
{
    public static Button Primario(string texto, int width = 132, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Primario);

    public static Button PrimarioIcone(string texto, string icone, int width = 148, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Primario);

    public static Button Ghost(string texto, int width = 108, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Ghost);

    public static Button GhostIcone(string texto, string icone, int width = 108, int height = 40, Color? cor = null)
    {
        var variante = cor == Tema.CorErro
            ? BotaoModerno.Variante.Perigo
            : cor == Tema.CorAlerta
                ? BotaoModerno.Variante.Aviso
                : BotaoModerno.Variante.Ghost;

        return Criar(texto, width, height, variante);
    }

    public static Button Sucesso(string texto, int width = 132, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Sucesso);

    public static Button SucessoIcone(string texto, string icone, int width = 132, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Sucesso);

    public static Button Perigo(string texto, int width = 108, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Perigo);

    public static Button PerigoIcone(string texto, string icone, int width = 108, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Perigo);

    public static Button Aviso(string texto, int width = 108, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Aviso);

    public static Button Info(string texto, int width = 132, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Ghost);

    public static Button InfoIcone(string texto, string icone, int width = 132, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Ghost);

    public static Button Secundario(string texto, int width = 132, int height = 40)
        => Criar(texto, width, height, BotaoModerno.Variante.Ghost);

    public static Button Toggle(string label, bool ativo = true, int width = 112)
    {
        var b = (BotaoModerno)Criar(label, width, 40, BotaoModerno.Variante.Toggle);
        b.Toggled = ativo;
        return b;
    }

    public static bool ToggleAtivo(Button btn)
        => btn is BotaoModerno moderno
            ? moderno.Toggled
            : btn.Tag is bool ativo && ativo;

    public static void ParaToolbar(params Button[] botoes)
    {
        foreach (var b in botoes)
        {
            b.Height = 40;
            b.Margin = new Padding(8, 0, 0, 0);
            b.AutoEllipsis = true;
            b.AutoSize = false;
            b.Width = Math.Max(b.Width, CalcularLarguraMinima(b));
            b.MinimumSize = new Size(b.Width, b.Height);
        }
    }

    public static void ParaPainelToolbar(FlowLayoutPanel painel, params Button[] botoes)
    {
        ParaToolbar(botoes);

        for (var i = 0; i < botoes.Length; i++)
            botoes[i].Margin = new Padding(i == 0 ? 0 : 8, 0, 0, 0);

        painel.FlowDirection = FlowDirection.LeftToRight;
        painel.WrapContents = false;
        painel.AutoScroll = false;
        painel.Width = LarguraPainelToolbar(botoes);
    }

    public static int LarguraPainelToolbar(params Button[] botoes)
        => botoes.Sum(b => b.Width + b.Margin.Horizontal) + 2;

    public static Color Misturar(Color a, Color b, float pct)
        => Tema.Misturar(a, b, pct);

    private static Button Criar(string texto, int width, int height, BotaoModerno.Variante variante)
    {
        var b = new BotaoModerno
        {
            Text = texto,
            Width = width,
            Height = height,
            Estilo = variante,
            Font = Tema.FontPequenaBold,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(14, 0, 14, 0),
            AutoEllipsis = true
        };

        b.Width = Math.Max(b.Width, CalcularLarguraMinima(b));
        b.MinimumSize = new Size(b.Width, b.Height);
        return b;
    }

    private static int CalcularLarguraMinima(Button b)
    {
        var larguraTexto = TextRenderer.MeasureText(b.Text ?? "", b.Font).Width;
        if (string.Equals((b.Text ?? "").Trim(), "...", StringComparison.Ordinal))
            return Math.Max(48, larguraTexto + 36);

        return Math.Max(104, larguraTexto + 58);
    }
}
