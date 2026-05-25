using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Exemplos de como usar o sistema de configuração modular.
/// REMOVER EM PRODUÇÃO - apenas para documentação.
/// </summary>
public static class ExemploConfiguracao
{
    /// <summary>
    /// Exemplo 1: Verificar se uma forma está disponível.
    /// </summary>
    public static bool VerificarSeFormaProdutoEstaDisponivel(ModuloSistema modulosAtivos)
    {
        // Apenas mostrar a opção se o módulo de Produção estiver ativo
        return (modulosAtivos & ModuloSistema.Producao) == ModuloSistema.Producao;
    }

    /// <summary>
    /// Exemplo 2: Usar o atributo para marcar um formulário.
    /// </summary>
    [ModuloRequerido(ModuloSistema.Producao)]
    public class ExemploFrmProducao
    {
        // Este formulário só será carregado se ModuloSistema.Producao estiver ativo
    }

    /// <summary>
    /// Exemplo 3: Usar o atributo com múltiplos módulos (um OU outro).
    /// </summary>
    [ModuloRequerido(ModuloSistema.Pesagem, ModuloSistema.Producao)]
    public class ExemploFrmPesagemOuProducao
    {
        // Este formulário requer TANTO Pesagem QUANTO Produção
    }

    /// <summary>
    /// Exemplo 4: Verificar módulos recomendados para um tipo.
    /// </summary>
    public static void ExemploCarregarModulos()
    {
        var modulosPadaria = ModulosPorTipo.ObterModulosRecomendados(TipoNegocio.Padaria);
        var modulosSuperMercado = ModulosPorTipo.ObterModulosRecomendados(TipoNegocio.Supermercado);

        // Resultado esperado para Padaria:
        // PDV, Estoque, Cadastros, Financeiro, Fiscal, Producao, Pesagem, Pix, Relatorios, Auditoria, Backup

        // Resultado esperado para Supermercado:
        // PDV, Estoque, Cadastros, Financeiro, Fiscal, Prevenda, Pesagem, Comissoes, Pix, Tef, Relatorios, Auditoria, Backup
    }

    /// <summary>
    /// Exemplo 5: Obter descrição legível de um módulo.
    /// </summary>
    public static void ExemploDescricoes()
    {
        var desc1 = ModulosPorTipo.ObterDescricaoModulo(ModuloSistema.Producao);
        var desc2 = ModulosPorTipo.ObterDescricaoModulo(ModuloSistema.Pesagem);

        // desc1 = "Módulo de Produção"
        // desc2 = "Controle de Pesagem e Balança"
    }

    /// <summary>
    /// Exemplo 6: Usar em um formulário/sidebar para carregar seções dinamicamente.
    /// </summary>
    public static List<string> ExemploCarregarSecoesSidebar(ModuloSistema modulosAtivos)
    {
        var secoes = new List<string> { "Principal", "Vendas", "Cadastros" };

        if ((modulosAtivos & ModuloSistema.Producao) == ModuloSistema.Producao)
            secoes.Add("Produção");

        if ((modulosAtivos & ModuloSistema.Prevenda) == ModuloSistema.Prevenda)
            secoes.Add("Promoções");

        if ((modulosAtivos & ModuloSistema.Comissoes) == ModuloSistema.Comissoes)
            secoes.Add("Vendedores");

        return secoes;
    }
}
