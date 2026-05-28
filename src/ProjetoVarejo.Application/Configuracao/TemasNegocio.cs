using System.Drawing;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Informações de tema customizado para cada tipo de negócio.
/// CorShell é usada para o fundo escuro da sidebar e topbar.
/// </summary>
public record TemaInfo(
    TipoNegocio Tipo,
    string Nome,
    string Icone,
    Color CorPrimaria,
    Color CorSecundaria,
    Color CorDestaque,
    Color CorShell,
    string Descricao
);

/// <summary>
/// Define cores e ícones customizados para cada tipo de negócio.
/// </summary>
public static class TemasNegocio
{
    public static IReadOnlyList<TemaInfo> Temas { get; } = new List<TemaInfo>
    {
        // 🥐 PADARIA — tons quentes de trigo e ouro
        new(
            TipoNegocio.Padaria,
            "Padaria",
            "🥐",
            Color.FromArgb(200, 130, 40),    // Ouro/Trigo
            Color.FromArgb(245, 220, 180),   // Bege claro
            Color.FromArgb(255, 180, 0),     // Ouro destaque
            Color.FromArgb(44, 26, 8),       // Castanho escuro
            "Sistema otimizado para panificação com controle de produção e ingredientes"
        ),

        // 🥩 AÇOUGUE — vermelhos intensos
        new(
            TipoNegocio.Acougue,
            "Açougue",
            "🥩",
            Color.FromArgb(195, 30, 30),     // Vermelho carne
            Color.FromArgb(255, 200, 200),   // Rosa claro
            Color.FromArgb(220, 20, 20),     // Vermelho destaque
            Color.FromArgb(52, 8, 8),        // Vermelho quase-preto
            "Sistema especializado em pesagem, cortes e controle de produção"
        ),

        // 🛍️ LOJA — azul aço profissional
        new(
            TipoNegocio.Loja,
            "Loja",
            "🛍️",
            Color.FromArgb(55, 95, 145),     // Azul aço
            Color.FromArgb(210, 225, 240),   // Azul claro
            Color.FromArgb(35, 72, 120),     // Azul destaque
            Color.FromArgb(12, 22, 42),      // Azul naval escuro
            "Sistema para varejo com gestão de comissões e pré-vendas"
        ),

        // 🏭 INDÚSTRIA — cinza industrial sólido
        new(
            TipoNegocio.Industria,
            "Indústria",
            "🏭",
            Color.FromArgb(65, 78, 92),      // Cinza industrial
            Color.FromArgb(200, 210, 220),   // Cinza claro
            Color.FromArgb(45, 58, 72),      // Cinza destaque
            Color.FromArgb(16, 20, 24),      // Cinza quase-preto
            "Sistema industrial com BOM, produção e gestão de comissões"
        ),

        // 🧺 BAZAR — roxo vibrante
        new(
            TipoNegocio.Bazar,
            "Bazar",
            "🧺",
            Color.FromArgb(148, 80, 160),    // Roxo bazar
            Color.FromArgb(230, 200, 235),   // Roxo claro
            Color.FromArgb(170, 90, 185),    // Roxo destaque
            Color.FromArgb(28, 10, 36),      // Roxo escuro profundo
            "Sistema simplificado para pequeno varejo com cadastro básico"
        ),

        // 🛒 SUPERMERCADO — verde varejo
        new(
            TipoNegocio.Supermercado,
            "Supermercado",
            "🛒",
            Color.FromArgb(0, 128, 68),      // Verde supermercado
            Color.FromArgb(200, 240, 220),   // Verde claro
            Color.FromArgb(0, 100, 52),      // Verde destaque
            Color.FromArgb(6, 28, 16),       // Verde floresta escuro
            "Sistema completo com múltiplas seções e gestão de promoções"
        ),

        // 💊 FARMÁCIA — verde saúde
        new(
            TipoNegocio.Farmacia,
            "Farmácia",
            "💊",
            Color.FromArgb(22, 128, 98),     // Verde saúde
            Color.FromArgb(200, 238, 228),   // Verde saúde claro
            Color.FromArgb(14, 100, 76),     // Verde saúde destaque
            Color.FromArgb(6, 26, 20),       // Verde-azul escuro
            "Sistema farmacêutico com gestão de receitas e medicamentos"
        ),

        // 🍽️ RESTAURANTE — laranja quente hospitaleiro
        new(
            TipoNegocio.Restaurante,
            "Restaurante",
            "🍽️",
            Color.FromArgb(185, 82, 38),     // Terracota
            Color.FromArgb(245, 215, 195),   // Bege quente
            Color.FromArgb(210, 62, 22),     // Laranja destaque
            Color.FromArgb(38, 14, 5),       // Marrom quase-preto
            "Sistema gastronômico com gestão de comandas e mesas"
        )
    };

    public static TemaInfo ObterTema(TipoNegocio tipo)
        => Temas.FirstOrDefault(t => t.Tipo == tipo)
           ?? Temas.First(t => t.Tipo == TipoNegocio.Loja);

    public static Color ObterCorPrimaria(TipoNegocio tipo) => ObterTema(tipo).CorPrimaria;
    public static Color ObterCorSecundaria(TipoNegocio tipo) => ObterTema(tipo).CorSecundaria;
    public static Color ObterCorDestaque(TipoNegocio tipo) => ObterTema(tipo).CorDestaque;
    public static string ObterIcone(TipoNegocio tipo) => ObterTema(tipo).Icone;
    public static string ObterDescricao(TipoNegocio tipo) => ObterTema(tipo).Descricao;
}
