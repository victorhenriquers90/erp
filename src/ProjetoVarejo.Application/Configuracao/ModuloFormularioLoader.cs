using ProjetoVarejo.Domain.Enums;
using System.Reflection;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Carrega apenas os formulários cujos módulos estão ativos.
/// </summary>
public class ModuloFormularioLoader
{
    /// <summary>
    /// Obtém todos os tipos (classes) que têm o atributo [ModuloRequerido].
    /// </summary>
    public static IEnumerable<Type> ObterTodosFormulariosMarcados()
    {
        var assembly = Assembly.Load("ProjetoVarejo.Desktop");
        return assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ModuloRequeridoAttribute>() != null);
    }

    /// <summary>
    /// Verifica se um tipo de formulário pode ser carregado com os módulos ativos.
    /// </summary>
    public static bool PodeCarregarFormulario(Type tipoFormulario, ModuloSistema modulosAtivos)
    {
        var atributo = tipoFormulario.GetCustomAttribute<ModuloRequeridoAttribute>();

        // Se não tem atributo, é um formulário básico que pode ser carregado
        if (atributo == null)
            return true;

        // Se tem atributo, verifica se todos os módulos requeridos estão ativos
        return atributo.TodosModulosAtivos(modulosAtivos);
    }

    /// <summary>
    /// Obtém informações sobre quais formulários estão disponíveis.
    /// </summary>
    public static IEnumerable<DisponibilidadeFormulario> AnalisarDisponibilidade(ModuloSistema modulosAtivos)
    {
        var formularios = ObterTodosFormulariosMarcados();

        var resultado = new List<DisponibilidadeFormulario>();

        foreach (var tipo in formularios)
        {
            var atributo = tipo.GetCustomAttribute<ModuloRequeridoAttribute>();
            var disponivel = atributo?.TodosModulosAtivos(modulosAtivos) ?? true;

            resultado.Add(new DisponibilidadeFormulario
            {
                TipoFormulario = tipo,
                Nome = tipo.Name,
                Disponivel = disponivel,
                ModulosRequeridos = atributo?.ModulosRequeridos ?? 0,
                Atributo = atributo
            });
        }

        return resultado.OrderBy(d => !d.Disponivel).ThenBy(d => d.Nome);
    }

    /// <summary>
    /// Filtra um tipo de formulário e seus módulos associados.
    /// </summary>
    public static IEnumerable<Type> ObterFormulariosDisponiveisPorModulo(
        ModuloSistema modulo,
        ModuloSistema modulosAtivos)
    {
        var formularios = ObterTodosFormulariosMarcados();

        return formularios
            .Where(t =>
            {
                var atributo = t.GetCustomAttribute<ModuloRequeridoAttribute>();

                // Formulário que requer este módulo
                if (atributo?.RequerModulo(modulo) ?? false)
                {
                    // E está disponível com os módulos ativos
                    return atributo.TodosModulosAtivos(modulosAtivos);
                }

                return false;
            });
    }

    /// <summary>
    /// Obtém todos os módulos requeridos por um tipo de formulário.
    /// </summary>
    public static ModuloSistema? ObterModulosRequeridos(Type tipoFormulario)
    {
        var atributo = tipoFormulario.GetCustomAttribute<ModuloRequeridoAttribute>();
        return atributo?.ModulosRequeridos;
    }
}

/// <summary>
/// Informações sobre disponibilidade de um formulário.
/// </summary>
public class DisponibilidadeFormulario
{
    /// <summary>Tipo do formulário</summary>
    public Type TipoFormulario { get; set; } = null!;

    /// <summary>Nome do formulário (nome da classe)</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Se o formulário está disponível com os módulos ativos</summary>
    public bool Disponivel { get; set; }

    /// <summary>Módulos que este formulário requer</summary>
    public ModuloSistema ModulosRequeridos { get; set; }

    /// <summary>Referência ao atributo (pode ser null)</summary>
    public ModuloRequeridoAttribute? Atributo { get; set; }

    /// <summary>Descrição dos módulos requeridos</summary>
    public string ObterDescricaoModulosRequeridos()
    {
        if (Atributo == null)
            return "Sem requisitos";

        var modulos = ModulosPorTipo.ObterTodosModulos()
            .Where(m => Atributo.RequerModulo(m))
            .Select(m => ModulosPorTipo.ObterDescricaoModulo(m));

        return string.Join(", ", modulos);
    }

    public override string ToString()
    {
        var status = Disponivel ? "✓" : "✗";
        return $"{status} {Nome}";
    }
}
