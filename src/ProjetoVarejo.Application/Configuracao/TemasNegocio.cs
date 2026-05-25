using System.Drawing;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Informações de tema customizado para cada tipo de negócio.
/// </summary>
public record TemaInfo(
    TipoNegocio Tipo,
    string Nome,
    string Icone,
    Color CorPrimaria,
    Color CorSecundaria,
    Color CorDestaque,
    string Descricao
);

/// <summary>
/// Define cores e ícones customizados para cada tipo de negócio.
/// Permite personalização visual do sistema conforme o ramo de negócio.
/// </summary>
public static class TemasNegocio
{
    /// <summary>
    /// Define os temas para todos os tipos de negócio.
    /// </summary>
    public static IReadOnlyList<TemaInfo> Temas { get; } = new List<TemaInfo>
    {
        // 🥐 PADARIA - Cores quentes, tons de trigo/ouro
        new(
            TipoNegocio.Padaria,
            "Padaria",
            "🥐",
            Color.FromArgb(200, 130, 40),    // Ouro/Trigo
            Color.FromArgb(245, 220, 180),   // Bege claro
            Color.FromArgb(255, 180, 0),     // Ouro destaque
            "Sistema otimizado para panificação com controle de ingredientes e produção"
        ),

        // 🥩 AÇOUGUE - Cores vermelhas, tons carnudos
        new(
            TipoNegocio.Acougue,
            "Açougue",
            "🥩",
            Color.FromArgb(200, 30, 30),     // Vermelho carne
            Color.FromArgb(255, 200, 200),   // Rosa claro
            Color.FromArgb(220, 20, 20),     // Vermelho destaque
            "Sistema especializado em pesagem, cortes e controle de produção"
        ),

        // 🛍️ LOJA - Cores neutras, tons profissionais
        new(
            TipoNegocio.Loja,
            "Loja",
            "🛍️",
            Color.FromArgb(70, 100, 140),    // Azul aço
            Color.FromArgb(210, 225, 240),   // Azul claro
            Color.FromArgb(50, 80, 120),     // Azul destaque
            "Sistema para varejo geral com gestão de comissões e pré-vendas"
        ),

        // 🏭 INDÚSTRIA - Cores sólidas, tons industriais
        new(
            TipoNegocio.Industria,
            "Indústria",
            "🏭",
            Color.FromArgb(60, 70, 80),      // Cinza industrial
            Color.FromArgb(200, 210, 220),   // Cinza claro
            Color.FromArgb(40, 50, 60),      // Cinza destaque
            "Sistema industrial com controle de BOM, produção e comissões"
        ),

        // 🧺 BAZAR - Cores vibrantes, tons divertidos
        new(
            TipoNegocio.Bazar,
            "Bazar",
            "🧺",
            Color.FromArgb(150, 90, 150),    // Roxo bazar
            Color.FromArgb(230, 200, 230),   // Roxo claro
            Color.FromArgb(180, 100, 180),   // Roxo destaque
            "Sistema simplificado para pequeno varejo com cadastro básico"
        ),

        // 🛒 SUPERMERCADO - Cores alegres, tons de varejo
        new(
            TipoNegocio.Supermercado,
            "Supermercado",
            "🛒",
            Color.FromArgb(0, 130, 70),      // Verde supermercado
            Color.FromArgb(200, 240, 220),   // Verde claro
            Color.FromArgb(0, 100, 50),      // Verde destaque
            "Sistema completo com múltiplas seções e gestão de promoções"
        ),

        // 💊 FARMÁCIA - Cores verdes médicas, tons de saúde
        new(
            TipoNegocio.Farmacia,
            "Farmácia",
            "💊",
            Color.FromArgb(30, 130, 100),    // Verde saúde
            Color.FromArgb(200, 235, 225),   // Verde saúde claro
            Color.FromArgb(20, 100, 80),     // Verde saúde destaque
            "Sistema farmacêutico com gestão de receitas e medicamentos"
        ),

        // 🍽️ RESTAURANTE - Cores quentes, tons de hospitalidade
        new(
            TipoNegocio.Restaurante,
            "Restaurante",
            "🍽️",
            Color.FromArgb(180, 80, 40),     // Marrom restaurante
            Color.FromArgb(240, 210, 190),   // Bege restaurante
            Color.FromArgb(200, 60, 20),     // Laranja destaque
            "Sistema gastronômico com gestão de comanda e mesas"
        )
    };

    /// <summary>
    /// Obtém o tema para um tipo de negócio específico.
    /// </summary>
    public static TemaInfo ObterTema(TipoNegocio tipo)
    {
        return Temas.FirstOrDefault(t => t.Tipo == tipo) ??
               Temas.First(t => t.Tipo == TipoNegocio.Loja); // Fallback para Loja
    }

    /// <summary>
    /// Obtém a cor primária de um tipo de negócio.
    /// </summary>
    public static Color ObterCorPrimaria(TipoNegocio tipo) => ObterTema(tipo).CorPrimaria;

    /// <summary>
    /// Obtém a cor secundária de um tipo de negócio.
    /// </summary>
    public static Color ObterCorSecundaria(TipoNegocio tipo) => ObterTema(tipo).CorSecundaria;

    /// <summary>
    /// Obtém a cor de destaque de um tipo de negócio.
    /// </summary>
    public static Color ObterCorDestaque(TipoNegocio tipo) => ObterTema(tipo).CorDestaque;

    /// <summary>
    /// Obtém o ícone emoji de um tipo de negócio.
    /// </summary>
    public static string ObterIcone(TipoNegocio tipo) => ObterTema(tipo).Icone;

    /// <summary>
    /// Obtém a descrição de um tipo de negócio.
    /// </summary>
    public static string ObterDescricao(TipoNegocio tipo) => ObterTema(tipo).Descricao;
}
