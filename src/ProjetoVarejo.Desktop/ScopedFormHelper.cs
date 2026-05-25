using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Desktop;

/// <summary>
/// Helper para abrir formulários com injeção de dependência e validação de módulos.
/// </summary>
public static class ScopedFormHelper
{
    /// <summary>
    /// Abre um formulário modal verificando se todos os módulos requeridos estão ativos.
    /// </summary>
    public static void AbrirModal<T>(IWin32Window? owner = null) where T : Form
    {
        using var scope = Program.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ConfiguracaoNegocioService>();

        // Verificar se formulário requer módulos
        var atributo = typeof(T).GetCustomAttribute<ModuloRequeridoAttribute>();
        if (atributo != null)
        {
            var config = configService.ObterConfiguracao().Result;
            if (!atributo.TodosModulosAtivos(config.ModulosAtivos))
            {
                MostrarErroModuloIndisponivel(atributo, owner);
                return;
            }
        }

        // Abrir formulário
        var form = scope.ServiceProvider.GetRequiredService<T>();
        form.ShowDialog(owner);
    }

    /// <summary>
    /// Exibe toast de erro quando módulo não está disponível.
    /// </summary>
    private static void MostrarErroModuloIndisponivel(ModuloRequeridoAttribute atributo, IWin32Window? owner)
    {
        var descricao = ObtiveDescricaoModulos(atributo.ModulosRequeridos);
        var mensagem = $"⚠️ Módulo '{descricao}' não disponível nesta instalação. Contate o administrador.";
        Toast.Mostrar(mensagem, TipoToast.Aviso, owner: owner);
    }

    /// <summary>
    /// Obtém descrição amigável dos módulos requeridos.
    /// </summary>
    private static string ObtiveDescricaoModulos(ModuloSistema modulos)
    {
        var lista = new List<string>();

        // Verificar cada módulo individualmente
        if ((modulos & ModuloSistema.PDV) != 0) lista.Add("PDV");
        if ((modulos & ModuloSistema.Estoque) != 0) lista.Add("Estoque");
        if ((modulos & ModuloSistema.Cadastros) != 0) lista.Add("Cadastros");
        if ((modulos & ModuloSistema.Financeiro) != 0) lista.Add("Financeiro");
        if ((modulos & ModuloSistema.Fiscal) != 0) lista.Add("Fiscal");
        if ((modulos & ModuloSistema.Producao) != 0) lista.Add("Produção");
        if ((modulos & ModuloSistema.Pesagem) != 0) lista.Add("Pesagem");
        if ((modulos & ModuloSistema.Prevenda) != 0) lista.Add("Pré-venda");
        if ((modulos & ModuloSistema.Comissoes) != 0) lista.Add("Comissões");
        if ((modulos & ModuloSistema.Relatorios) != 0) lista.Add("Relatórios");
        if ((modulos & ModuloSistema.Auditoria) != 0) lista.Add("Auditoria");
        if ((modulos & ModuloSistema.Backup) != 0) lista.Add("Backup");
        if ((modulos & ModuloSistema.Pix) != 0) lista.Add("PIX");
        if ((modulos & ModuloSistema.Tef) != 0) lista.Add("TEF");
        if ((modulos & ModuloSistema.Receitas) != 0) lista.Add("Receitas");
        if ((modulos & ModuloSistema.Comandas) != 0) lista.Add("Comandas");

        return lista.Count == 1 ? lista[0] : string.Join(", ", lista);
    }
}
