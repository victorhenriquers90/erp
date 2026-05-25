using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Define quais módulos são recomendados para cada tipo de negócio.
/// </summary>
public static class ModulosPorTipo
{
    /// <summary>Módulos obrigatórios em todas as instalações</summary>
    private static readonly ModuloSistema ModulosObrigatorios =
        ModuloSistema.PDV |
        ModuloSistema.Estoque |
        ModuloSistema.Cadastros |
        ModuloSistema.Financeiro |
        ModuloSistema.Relatorios |
        ModuloSistema.Auditoria |
        ModuloSistema.Backup;

    /// <summary>
    /// Obtém os módulos recomendados para um tipo de negócio.
    /// </summary>
    public static ModuloSistema ObterModulosRecomendados(TipoNegocio tipo)
    {
        return tipo switch
        {
            TipoNegocio.Padaria => ModulosObrigatorios |
                ModuloSistema.Fiscal |
                ModuloSistema.Producao |
                ModuloSistema.Pesagem |
                ModuloSistema.Pix,

            TipoNegocio.Acougue => ModulosObrigatorios |
                ModuloSistema.Fiscal |
                ModuloSistema.Producao |
                ModuloSistema.Pesagem |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            TipoNegocio.Loja => ModulosObrigatorios |
                ModuloSistema.Fiscal |
                ModuloSistema.Prevenda |
                ModuloSistema.Comissoes |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            TipoNegocio.Industria => ModulosObrigatorios |
                ModuloSistema.Fiscal |
                ModuloSistema.Producao |
                ModuloSistema.Comissoes |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            TipoNegocio.Bazar => ModulosObrigatorios |
                ModuloSistema.Fiscal |
                ModuloSistema.Prevenda |
                ModuloSistema.Pix,

            TipoNegocio.Supermercado => ModulosObrigatorios |
                ModuloSistema.Fiscal |
                ModuloSistema.Prevenda |
                ModuloSistema.Pesagem |
                ModuloSistema.Comissoes |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            TipoNegocio.Farmacia => ModulosObrigatorios |
                ModuloSistema.Fiscal |
                ModuloSistema.Receitas |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            TipoNegocio.Restaurante => ModulosObrigatorios |
                ModuloSistema.Fiscal |
                ModuloSistema.Comandas |
                ModuloSistema.Producao |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            _ => ModulosObrigatorios
        };
    }

    /// <summary>
    /// Obtém a lista de módulos como array para facilitar iteração.
    /// </summary>
    public static ModuloSistema[] ObterTodosModulos()
    {
        return new[]
        {
            ModuloSistema.PDV,
            ModuloSistema.Estoque,
            ModuloSistema.Cadastros,
            ModuloSistema.Financeiro,
            ModuloSistema.Fiscal,
            ModuloSistema.Producao,
            ModuloSistema.Prevenda,
            ModuloSistema.Pesagem,
            ModuloSistema.Comissoes,
            ModuloSistema.Relatorios,
            ModuloSistema.Auditoria,
            ModuloSistema.Backup,
            ModuloSistema.Pix,
            ModuloSistema.Tef,
            ModuloSistema.Receitas,
            ModuloSistema.Comandas
        };
    }

    /// <summary>
    /// Obtém uma descrição legível de um módulo.
    /// </summary>
    public static string ObterDescricaoModulo(ModuloSistema modulo) => modulo switch
    {
        ModuloSistema.PDV => "PDV - Ponto de Venda",
        ModuloSistema.Estoque => "Gestão de Estoque",
        ModuloSistema.Cadastros => "Cadastros (Produtos, Clientes, Fornecedores)",
        ModuloSistema.Financeiro => "Financeiro (Contas a Pagar/Receber)",
        ModuloSistema.Fiscal => "NFC-e e Integração Fiscal",
        ModuloSistema.Producao => "Módulo de Produção",
        ModuloSistema.Prevenda => "Pré-venda e Promoções",
        ModuloSistema.Pesagem => "Controle de Pesagem e Balança",
        ModuloSistema.Comissoes => "Comissões e Vendedores",
        ModuloSistema.Relatorios => "Relatórios e Analytics",
        ModuloSistema.Auditoria => "Auditoria e Governança",
        ModuloSistema.Backup => "Backup e Restauração",
        ModuloSistema.Pix => "Integração com PIX",
        ModuloSistema.Tef => "Integração com TEF",
        ModuloSistema.Receitas => "Controle de Receitas (Farmácia)",
        ModuloSistema.Comandas => "Controle de Mesas/Comandas (Restaurante)",
        _ => "Módulo Desconhecido"
    };

    /// <summary>
    /// Verifica se um módulo é obrigatório.
    /// </summary>
    public static bool EObrigatorio(ModuloSistema modulo)
    {
        return (ModulosObrigatorios & modulo) == modulo;
    }
}
