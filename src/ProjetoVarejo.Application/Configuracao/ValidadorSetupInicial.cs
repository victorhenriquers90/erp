using ProjetoVarejo.Domain.Configuracao;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Valida se o setup inicial foi completado.
/// </summary>
public class ValidadorSetupInicial
{
    private readonly ConfiguracaoNegocioService _configuracao;

    public ValidadorSetupInicial(ConfiguracaoNegocioService configuracao)
    {
        _configuracao = configuracao;
    }

    /// <summary>
    /// Verifica se o setup inicial é necessário (configuração não foi feita).
    /// </summary>
    public async Task<bool> PrecisaDeSetupInicial()
    {
        try
        {
            var config = await _configuracao.ObterConfiguracao();
            return !config.ConfiguracaoInicial || config.TipoNegocio == 0;
        }
        catch
        {
            // Se houve erro ao obter configuração, precisa fazer setup
            return true;
        }
    }

    /// <summary>
    /// Obtém informações sobre o status da configuração.
    /// </summary>
    public async Task<InfoConfiguracao> ObterInfoConfiguracao()
    {
        var config = await _configuracao.ObterConfiguracao();
        var modulosAtivos = await _configuracao.ObterStatusModulos();

        return new InfoConfiguracao
        {
            FoiConfigurado = config.ConfiguracaoInicial && config.TipoNegocio != 0,
            TipoNegocio = config.TipoNegocio,
            DescricaoNegocio = config.DescricaoNegocio,
            TotalModulosAtivos = modulosAtivos.Count(m => m.Value),
            TotalModulosDisponiveis = modulosAtivos.Count,
            DataUltimaAtualizacao = config.DataAtualizacao
        };
    }
}

/// <summary>
/// Informações resumidas da configuração.
/// </summary>
public class InfoConfiguracao
{
    /// <summary>Se o setup inicial foi completado</summary>
    public bool FoiConfigurado { get; set; }

    /// <summary>Tipo de negócio configurado</summary>
    public Domain.Enums.TipoNegocio TipoNegocio { get; set; }

    /// <summary>Descrição/nome da empresa</summary>
    public string DescricaoNegocio { get; set; } = string.Empty;

    /// <summary>Total de módulos que estão ativos</summary>
    public int TotalModulosAtivos { get; set; }

    /// <summary>Total de módulos disponíveis</summary>
    public int TotalModulosDisponiveis { get; set; }

    /// <summary>Data da última atualização da configuração</summary>
    public DateTime DataUltimaAtualizacao { get; set; }

    /// <summary>Resumo em formato legível</summary>
    public override string ToString()
    {
        if (!FoiConfigurado)
            return "Sistema não configurado";

        return $"{DescricaoNegocio} - {TipoNegocio} ({TotalModulosAtivos}/{TotalModulosDisponiveis} módulos)";
    }
}
