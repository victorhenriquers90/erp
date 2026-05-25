using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Marca uma classe/formulário como dependente de um ou mais módulos.
/// Quando o módulo não está ativo, a classe não deve ser carregada.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ModuloRequeridoAttribute : Attribute
{
    /// <summary>
    /// Módulos obrigatórios para usar esta classe.
    /// </summary>
    public ModuloSistema ModulosRequeridos { get; }

    /// <summary>
    /// Cria um atributo indicando os módulos requeridos.
    /// </summary>
    public ModuloRequeridoAttribute(params ModuloSistema[] modulos)
    {
        ModulosRequeridos = modulos.Aggregate((a, b) => a | b);
    }

    /// <summary>
    /// Verifica se um módulo está nos requerimentos.
    /// </summary>
    public bool RequerModulo(ModuloSistema modulo)
    {
        return (ModulosRequeridos & modulo) == modulo;
    }

    /// <summary>
    /// Verifica se todos os módulos requeridos estão ativos.
    /// </summary>
    public bool TodosModulosAtivos(ModuloSistema modulosAtivos)
    {
        return (modulosAtivos & ModulosRequeridos) == ModulosRequeridos;
    }
}
