namespace ProjetoVarejo.Domain.Enums;

/// <summary>
/// Define todos os módulos disponíveis no sistema.
/// </summary>
[Flags]
public enum ModuloSistema
{
    /// <summary>PDV - Ponto de Venda e Frente de Caixa (obrigatório)</summary>
    PDV = 1 << 0,

    /// <summary>Gestão de Estoque</summary>
    Estoque = 1 << 1,

    /// <summary>Cadastros (produtos, clientes, fornecedores)</summary>
    Cadastros = 1 << 2,

    /// <summary>Módulo Financeiro (contas a pagar/receber)</summary>
    Financeiro = 1 << 3,

    /// <summary>NFC-e e Integração Fiscal</summary>
    Fiscal = 1 << 4,

    /// <summary>Módulo de Produção (padaria, açougue, indústria)</summary>
    Producao = 1 << 5,

    /// <summary>Pré-venda e Promoções</summary>
    Prevenda = 1 << 6,

    /// <summary>Controle de Pesagem e Balança (açougue, padaria)</summary>
    Pesagem = 1 << 7,

    /// <summary>Comissões e Vendedores</summary>
    Comissoes = 1 << 8,

    /// <summary>Relatórios e Analytics</summary>
    Relatorios = 1 << 9,

    /// <summary>Auditoria e Governança</summary>
    Auditoria = 1 << 10,

    /// <summary>Backup e Restauração</summary>
    Backup = 1 << 11,

    /// <summary>Integração com PIX</summary>
    Pix = 1 << 12,

    /// <summary>Integração com TEF (Transferência Eletrônica de Fundos)</summary>
    Tef = 1 << 13,

    /// <summary>Controle de Receitas (farmácia)</summary>
    Receitas = 1 << 14,

    /// <summary>Controle de Mesas/Comandas (restaurante)</summary>
    Comandas = 1 << 15
}
