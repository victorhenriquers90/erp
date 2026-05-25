namespace ProjetoVarejo.Domain.Enums;

/// <summary>
/// Define o tipo/ramo de negócio para o qual o sistema será configurado.
/// </summary>
public enum TipoNegocio
{
    /// <summary>Padaria - com módulo de produção e controle de ingredientes</summary>
    Padaria = 1,

    /// <summary>Açougue - com controle de cortes, pesagem e produção</summary>
    Acougue = 2,

    /// <summary>Loja/Varejo Geral - comércio de variedades</summary>
    Loja = 3,

    /// <summary>Indústria - com controle de produção, BOM e comissões</summary>
    Industria = 4,

    /// <summary>Bazar/Armarinho - pequeno comércio diverso</summary>
    Bazar = 5,

    /// <summary>Supermercado - com promoções, pré-venda e múltiplas seções</summary>
    Supermercado = 6,

    /// <summary>Farmácia - com controle de medicamentos e receitas</summary>
    Farmacia = 7,

    /// <summary>Restaurante/Bar - com controle de comanda e mesas</summary>
    Restaurante = 8
}
