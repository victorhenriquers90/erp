using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Configuracao;
using ProjetoVarejo.Domain.Enums;
using ProjetoVarejo.Application.Contracts.Repositories;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Serviço que gerencia a configuração de negócio e módulos ativos.
/// </summary>
public class ConfiguracaoNegocioService
{
    private readonly IUnitOfWork _unitOfWork;
    private ConfiguracaoNegocio? _cacheConfiguracao;

    public ConfiguracaoNegocioService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Obtém a configuração atual (com cache em memória).
    /// </summary>
    public async Task<ConfiguracaoNegocio> ObterConfiguracao()
    {
        if (_cacheConfiguracao != null)
            return _cacheConfiguracao;

        _cacheConfiguracao = await _unitOfWork.ConfiguracoesNegocio.Query()
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync();

        if (_cacheConfiguracao == null)
        {
            // Se não existe configuração, criar padrão (não configurado)
            _cacheConfiguracao = new ConfiguracaoNegocio
            {
                Id = 1,
                TipoNegocio = (TipoNegocio)0,
                ConfiguracaoInicial = false,
                ModulosAtivos = ModuloSistema.PDV | ModuloSistema.Estoque | ModuloSistema.Cadastros | ModuloSistema.Financeiro,
                DataAtualizacao = DateTime.Now,
                Versao = 1
            };
        }

        return _cacheConfiguracao;
    }

    /// <summary>
    /// Define o tipo de negócio e carrega os módulos recomendados.
    /// </summary>
    public async Task ConfigurarNegocio(TipoNegocio tipo, string descricao = "")
    {
        var config = await ObterConfiguracao();

        config.TipoNegocio = tipo;
        config.DescricaoNegocio = descricao;
        config.ModulosAtivos = ModulosPorTipo.ObterModulosRecomendados(tipo);
        config.ConfiguracaoInicial = true;
        config.DataAtualizacao = DateTime.Now;

        await SalvarConfiguracao(config);
    }

    /// <summary>
    /// Salva a configuração no banco de dados.
    /// </summary>
    public async Task SalvarConfiguracao(ConfiguracaoNegocio config)
    {
        config.DataAtualizacao = DateTime.Now;

        var existente = await _unitOfWork.ConfiguracoesNegocio.Query()
            .FirstOrDefaultAsync(c => c.Id == config.Id);

        if (existente == null)
        {
            await _unitOfWork.ConfiguracoesNegocio.InsertAsync(config);
        }
        else
        {
            existente.TipoNegocio = config.TipoNegocio;
            existente.DescricaoNegocio = config.DescricaoNegocio;
            existente.ModulosAtivos = config.ModulosAtivos;
            existente.ConfiguracaoInicial = config.ConfiguracaoInicial;
            existente.DataAtualizacao = config.DataAtualizacao;
            existente.Versao = config.Versao;
            await _unitOfWork.ConfiguracoesNegocio.UpdateAsync(existente);
        }

        await _unitOfWork.SaveChangesAsync();

        // Limpar cache
        _cacheConfiguracao = config;
    }

    /// <summary>
    /// Ativa um módulo adicional.
    /// </summary>
    public async Task AtivarModulo(ModuloSistema modulo)
    {
        var config = await ObterConfiguracao();
        config.AtivarModulo(modulo);
        await SalvarConfiguracao(config);
    }

    /// <summary>
    /// Desativa um módulo (exceto os obrigatórios).
    /// </summary>
    public async Task DesativarModulo(ModuloSistema modulo)
    {
        if (ModulosPorTipo.EObrigatorio(modulo))
            throw new InvalidOperationException($"O módulo {modulo} é obrigatório e não pode ser desativado.");

        var config = await ObterConfiguracao();
        config.DesativarModulo(modulo);
        await SalvarConfiguracao(config);
    }

    /// <summary>
    /// Verifica se a instalação foi configurada (tipo de negócio selecionado).
    /// </summary>
    public async Task<bool> EstaConfigurada()
    {
        var config = await ObterConfiguracao();
        return config.ConfiguracaoInicial && config.TipoNegocio != 0;
    }

    /// <summary>
    /// Obtém todos os módulos e seu status (ativo/inativo).
    /// </summary>
    public async Task<Dictionary<ModuloSistema, bool>> ObterStatusModulos()
    {
        var config = await ObterConfiguracao();
        var resultado = new Dictionary<ModuloSistema, bool>();

        foreach (var modulo in ModulosPorTipo.ObterTodosModulos())
        {
            resultado[modulo] = config.EstaModuloAtivo(modulo);
        }

        return resultado;
    }

    /// <summary>
    /// Reseta a configuração para padrão (desconfigurado).
    /// ATENÇÃO: Operação irreversível!
    /// </summary>
    public async Task ResetarConfiguracao()
    {
        var config = new ConfiguracaoNegocio
        {
            Id = 1,
            TipoNegocio = (TipoNegocio)0,
            ConfiguracaoInicial = false,
            ModulosAtivos = ModuloSistema.PDV | ModuloSistema.Estoque | ModuloSistema.Cadastros | ModuloSistema.Financeiro,
            DataAtualizacao = DateTime.Now,
            Versao = 1
        };

        await SalvarConfiguracao(config);
    }

    /// <summary>
    /// Limpa o cache (força recarga do banco na próxima chamada).
    /// </summary>
    public void LimparCache()
    {
        _cacheConfiguracao = null;
    }
}
