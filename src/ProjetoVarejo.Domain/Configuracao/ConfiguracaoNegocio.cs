using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Domain.Configuracao;

/// <summary>
/// Armazena a configuração do tipo de negócio e módulos ativos da instalação.
/// </summary>
public class ConfiguracaoNegocio
{
    /// <summary>ID único da configuração (sempre 1, pois é global da aplicação)</summary>
    public int Id { get; set; }

    /// <summary>Tipo de negócio configurado</summary>
    public TipoNegocio TipoNegocio { get; set; }

    /// <summary>Descrição do tipo de negócio (ex: "Padaria Artesanal")</summary>
    public string DescricaoNegocio { get; set; } = string.Empty;

    /// <summary>Flag indicando se a configuração inicial foi concluída</summary>
    public bool ConfiguracaoInicial { get; set; }

    /// <summary>Módulos ativos para este tipo de negócio (combinação de flags)</summary>
    public ModuloSistema ModulosAtivos { get; set; }

    /// <summary>Data/hora da última atualização da configuração</summary>
    public DateTime DataAtualizacao { get; set; }

    /// <summary>Versão da configuração (para controle de migrações)</summary>
    public int Versao { get; set; } = 1;

    /// <summary>
    /// Verifica se um módulo específico está ativo.
    /// </summary>
    public bool EstaModuloAtivo(ModuloSistema modulo)
    {
        return (ModulosAtivos & modulo) == modulo;
    }

    /// <summary>
    /// Ativa um módulo adicionando a flag.
    /// </summary>
    public void AtivarModulo(ModuloSistema modulo)
    {
        ModulosAtivos |= modulo;
    }

    /// <summary>
    /// Desativa um módulo removendo a flag.
    /// </summary>
    public void DesativarModulo(ModuloSistema modulo)
    {
        ModulosAtivos &= ~modulo;
    }

    /// <summary>
    /// Obtém uma descrição legível do tipo de negócio.
    /// </summary>
    public string ObterDescricaoTipo() => TipoNegocio switch
    {
        TipoNegocio.Padaria => "🥐 Padaria",
        TipoNegocio.Acougue => "🥩 Açougue",
        TipoNegocio.Loja => "🛍️ Loja Varejo",
        TipoNegocio.Industria => "🏭 Indústria",
        TipoNegocio.Bazar => "🧺 Bazar/Armarinho",
        TipoNegocio.Supermercado => "🛒 Supermercado",
        TipoNegocio.Farmacia => "💊 Farmácia",
        TipoNegocio.Restaurante => "🍽️ Restaurante/Bar",
        _ => "Desconhecido"
    };
}
