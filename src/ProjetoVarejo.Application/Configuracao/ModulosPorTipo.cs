using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Define quais módulos são recomendados para cada tipo de negócio.
/// </summary>
public static class ModulosPorTipo
{
    /// <summary>
    /// Módulos presentes em TODAS as instalações independente do segmento.
    /// PDV NÃO está aqui — varia conforme o tipo de negócio.
    /// </summary>
    private static readonly ModuloSistema ModulosBase =
        ModuloSistema.Estoque |
        ModuloSistema.Cadastros |
        ModuloSistema.Financeiro |
        ModuloSistema.Relatorios |
        ModuloSistema.Auditoria |
        ModuloSistema.Backup;

    /// <summary>
    /// Obtém os módulos recomendados para cada segmento de negócio.
    /// </summary>
    public static ModuloSistema ObterModulosRecomendados(TipoNegocio tipo)
    {
        return tipo switch
        {
            // Padaria: frente de caixa, produção própria, venda por peso, fiscal
            TipoNegocio.Padaria => ModulosBase |
                ModuloSistema.PDV |
                ModuloSistema.Fiscal |
                ModuloSistema.Producao |
                ModuloSistema.Pesagem |
                ModuloSistema.Pix,

            // Açougue: PDV com balança, produção, fiscal, pagamento eletrônico
            TipoNegocio.Acougue => ModulosBase |
                ModuloSistema.PDV |
                ModuloSistema.Fiscal |
                ModuloSistema.Producao |
                ModuloSistema.Pesagem |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            // Loja varejo: PDV, pré-venda, comissões de vendedores, fiscal completo
            TipoNegocio.Loja => ModulosBase |
                ModuloSistema.PDV |
                ModuloSistema.Fiscal |
                ModuloSistema.Prevenda |
                ModuloSistema.Comissoes |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            // Indústria: SEM PDV (venda atacado/B2B), foco em produção, fiscal NF-e e comissões
            TipoNegocio.Industria => ModulosBase |
                ModuloSistema.Fiscal |
                ModuloSistema.Producao |
                ModuloSistema.Comissoes,

            // Bazar: PDV simples, pré-venda, fiscal básico
            TipoNegocio.Bazar => ModulosBase |
                ModuloSistema.PDV |
                ModuloSistema.Fiscal |
                ModuloSistema.Prevenda |
                ModuloSistema.Pix,

            // Supermercado: PDV com balança, pré-venda, comissões, fiscal completo, TEF
            TipoNegocio.Supermercado => ModulosBase |
                ModuloSistema.PDV |
                ModuloSistema.Fiscal |
                ModuloSistema.Prevenda |
                ModuloSistema.Pesagem |
                ModuloSistema.Comissoes |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            // Farmácia: PDV com controle de receitas, fiscal, pagamento eletrônico
            TipoNegocio.Farmacia => ModulosBase |
                ModuloSistema.PDV |
                ModuloSistema.Fiscal |
                ModuloSistema.Receitas |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            // Restaurante: PDV com comandas/mesas, produção de pratos, fiscal, TEF
            TipoNegocio.Restaurante => ModulosBase |
                ModuloSistema.PDV |
                ModuloSistema.Fiscal |
                ModuloSistema.Comandas |
                ModuloSistema.Producao |
                ModuloSistema.Pix |
                ModuloSistema.Tef,

            _ => ModulosBase
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
        return (ModulosBase & modulo) == modulo;
    }
}
