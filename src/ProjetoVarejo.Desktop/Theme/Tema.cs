using System.Drawing.Drawing2D;

namespace ProjetoVarejo.Desktop.Theme;

/// <summary>
/// Sistema de design central com linguagem ERP corporativa.
/// Paleta clara, alta densidade visual, bordas discretas e acentos institucionais.
/// </summary>
public static class Tema
{
    public const string NomeProduto = "ProjetoVarejo ERP";
    public const string NomeProdutoCurto = "ProjetoVarejo";
    public const string TaglineProduto = "Gestao integrada para varejo";

    // === Paleta principal ===
    // Azul institucional, proximo de suites ERP corporativas.
    public static Color CorPrimaria = Color.FromArgb(10, 110, 209);
    public static Color CorPrimariaDark = Color.FromArgb(7, 73, 143);
    public static Color CorPrimariaLight = Color.FromArgb(53, 142, 224);
    public static Color CorPrimariaSoft = Color.FromArgb(231, 243, 255);   // fundo para items ativos

    // === Cores semanticas ===
    public static Color CorSucesso = Color.FromArgb(37, 140, 88);
    public static Color CorSucessoSoft = Color.FromArgb(231, 246, 239);
    public static Color CorAlerta = Color.FromArgb(196, 116, 18);
    public static Color CorAlertaSoft = Color.FromArgb(255, 245, 225);
    public static Color CorErro = Color.FromArgb(187, 43, 43);
    public static Color CorErroSoft = Color.FromArgb(255, 235, 235);
    public static Color CorInfo = Color.FromArgb(0, 124, 142);
    public static Color CorInfoSoft = Color.FromArgb(226, 246, 249);
    public static Color CorNeutro = Color.FromArgb(98, 110, 127);
    public static Color CorNeutroSoft = Color.FromArgb(242, 244, 247);

    // === Neutros ===
    public static Color Branco = Color.White;
    public static Color CorFundo = Color.FromArgb(245, 247, 250);
    public static Color CorCard = Color.White;
    public static Color CorCardAlt = Color.FromArgb(250, 251, 253);
    public static Color CorBorda = Color.FromArgb(214, 220, 229);
    public static Color CorBordaSuave = Color.FromArgb(231, 235, 241);
    public static Color CorTextoEscuro = Color.FromArgb(28, 39, 55);
    public static Color CorTextoMedio = Color.FromArgb(91, 105, 123);
    public static Color CorTextoClaro = Color.FromArgb(141, 154, 171);

    // === Shell ERP ===
    public static Color ShellBarFundo = Color.FromArgb(8, 49, 92);
    public static Color ShellBarHover = Color.FromArgb(17, 68, 121);
    public static Color ShellBarTexto = Color.FromArgb(245, 249, 255);
    public static Color ShellBarTextoSuave = Color.FromArgb(188, 210, 234);

    public static Color SidebarFundo = Color.FromArgb(20, 32, 48);
    public static Color SidebarHover = Color.FromArgb(32, 47, 67);
    public static Color SidebarAtivo = Color.FromArgb(11, 96, 176);
    public static Color SidebarTexto = Color.FromArgb(198, 209, 223);
    public static Color SidebarTextoAtivo = Color.FromArgb(255, 255, 255);
    public static Color SidebarSecao = Color.FromArgb(130, 148, 170);  // label de secao

    // === Sombras ===
    public static Color SombraSuave = Color.FromArgb(8, 0, 0, 0);
    public static Color SombraMedia = Color.FromArgb(16, 0, 0, 0);
    public static Color SombraForte = Color.FromArgb(28, 0, 0, 0);

    // === Fontes ===
    public const string FontFamily = "Segoe UI";
    public const string FontFamilyMono = "Consolas";

    public static Font FontDisplay = new(FontFamily, 30, FontStyle.Bold);
    public static Font FontTituloGigante = new(FontFamily, 24, FontStyle.Bold);
    public static Font FontTituloGrande = new(FontFamily, 18, FontStyle.Bold);
    public static Font FontTitulo = new(FontFamily, 14, FontStyle.Bold);
    public static Font FontSubtitulo = new(FontFamily, 11, FontStyle.Bold);
    public static Font FontCorpo = new(FontFamily, 10);
    public static Font FontCorpoBold = new(FontFamily, 10, FontStyle.Bold);
    public static Font FontPequena = new(FontFamily, 9);
    public static Font FontPequenaBold = new(FontFamily, 9, FontStyle.Bold);
    public static Font FontMicro = new(FontFamily, 8);
    public static Font FontMono = new(FontFamilyMono, 10);

    // === Métricas ===
    public const int RaioCard = 6;
    public const int RaioBotao = 4;
    public const int RaioBadge = 10;
    public const int Espacamento = 14;
    public const int EspacamentoGrande = 22;
    public const int EspacamentoExtra = 30;
    public const int AlturaInput = 34;
    public const int AlturaBotao = 34;
    public const int AlturaTopbar = 56;
    public const int LarguraSidebar = 256;

    // === Glyphs (Segoe MDL2 Assets / Fluent UI) ===
    public const string IconHome = "";       // home
    public const string IconVendas = "";     // shopping cart
    public const string IconCaixa = "";      // money
    public const string IconProdutos = "";   // package
    public const string IconClientes = "";   // contact
    public const string IconFornecedores = ""; // truck
    public const string IconEstoque = "";    // package
    public const string IconFinanceiro = ""; // bank
    public const string IconRelatorios = ""; // chart
    public const string IconNotas = "";      // document
    public const string IconConfig = "";     // settings
    public const string IconSair = "";       // signout
    public const string IconBusca = "";      // search
    public const string IconNotificacao = "";// bell
    public const string IconUsuario = "";    // person
    public const string IconSucesso = "";    // check
    public const string IconAlerta = "";     // warning
    public const string IconErro = "";       // error
    public const string IconInfo = "";       // info
    public const string IconArrowUp = "";    // arrow up
    public const string IconArrowDown = "";  // arrow down
    public const string IconFiltro = "";     // filter
    public const string IconAdicionar = "";  // add
    public const string IconEditar = "";     // edit
    public const string IconExcluir = "";    // delete
    public const string IconImpressora = ""; // print
    public const string IconRefresh = "";    // refresh
    public const string IconDownload = "";   // download
    public const string IconUpload = "";     // upload
    public const string IconBackup = "\uE74E"; // save
    public const string IconAuditoria = "\uE9D9"; // report document
    public const string IconChecklist = "\uE9D5"; // checklist

    public static Font FontIcone(float size = 14) => new("Segoe MDL2 Assets", size);

    // === Helpers de desenho ===

    /// <summary>
    /// Cria um path com cantos arredondados.
    /// </summary>
    public static GraphicsPath PathArredondado(Rectangle rect, int raio)
    {
        var path = new GraphicsPath();
        if (raio <= 0) { path.AddRectangle(rect); return path; }
        var d = raio * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>
    /// Desenha sombra atrás de um rect arredondado. Use ANTES de desenhar o conteúdo.
    /// </summary>
    public static void DesenharSombra(Graphics g, Rectangle alvo, int raio, int distancia = 4, int profundidade = 3)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        for (int i = profundidade; i > 0; i--)
        {
            var expandido = new Rectangle(alvo.X - i, alvo.Y - i + distancia / 2, alvo.Width + 2 * i, alvo.Height + 2 * i);
            using var path = PathArredondado(expandido, raio + i);
            using var brush = new SolidBrush(Color.FromArgb(4, 0, 0, 0));
            g.FillPath(brush, path);
        }
    }

    /// <summary>
    /// Mistura duas cores. pct=0 retorna a, pct=1 retorna b.
    /// </summary>
    public static Color Misturar(Color a, Color b, float pct)
    {
        pct = Math.Clamp(pct, 0f, 1f);
        return Color.FromArgb(
            (int)(a.R * (1 - pct) + b.R * pct),
            (int)(a.G * (1 - pct) + b.G * pct),
            (int)(a.B * (1 - pct) + b.B * pct));
    }
}
