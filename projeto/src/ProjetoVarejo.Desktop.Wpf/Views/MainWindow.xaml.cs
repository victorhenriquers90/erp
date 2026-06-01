using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Infrastructure.Data;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly SessaoApp _sessao;
    private readonly IServiceProvider _services;

    public MainWindow(SessaoApp sessao, IServiceProvider services)
    {
        _sessao = sessao;
        _services = services;
        InitializeComponent();

        var nomeUsuario = _sessao.UsuarioLogado?.Nome ?? "Usuario";
        var nomeEmpresa = _sessao.EmpresaAtiva?.NomeFantasia ?? _sessao.EmpresaAtiva?.RazaoSocial ?? "Empresa";

        LblUsuario.Text = nomeUsuario;
        LblEmpresa.Text = nomeEmpresa;

        // Top bar
        LblTopUsuario.Text = nomeUsuario;
        LblTopEmpresa.Text = nomeEmpresa;
        LblAvatar.Text = string.IsNullOrWhiteSpace(nomeUsuario) ? "?" : nomeUsuario.Trim()[..1].ToUpperInvariant();

        // Saudação + data
        var hora = DateTime.Now.Hour;
        var saudacao = hora < 12 ? "Bom dia" : hora < 18 ? "Boa tarde" : "Boa noite";
        var primeiroNome = nomeUsuario.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nomeUsuario;
        LblSaudacao.Text = $"{saudacao}, {primeiroNome}!";
        LblData.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy",
            new System.Globalization.CultureInfo("pt-BR"));

        _ = CarregarKpisAsync();
    }

    private async Task CarregarKpisAsync()
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var clientes = await db.Clientes.CountAsync(c => c.Ativo);
            var produtos = await db.Produtos.CountAsync(p => p.Ativo);
            var fornecedores = await db.Fornecedores.CountAsync(f => f.Ativo);
            var contasAbertas = await db.ContasFinanceiras.CountAsync(c => c.Ativo);

            LblKpiClientes.Text = clientes.ToString("N0");
            LblKpiProdutos.Text = produtos.ToString("N0");
            LblKpiFornecedores.Text = fornecedores.ToString("N0");
            LblKpiFinanceiro.Text = contasAbertas.ToString("N0");
        }
        catch
        {
            LblKpiClientes.Text = "-";
            LblKpiProdutos.Text = "-";
            LblKpiFornecedores.Text = "-";
            LblKpiFinanceiro.Text = "-";
        }
    }

    private void Painel_Click(object sender, RoutedEventArgs e) => _ = CarregarKpisAsync();

    private void Clientes_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<ClientesWindow>();
        win.Owner = this;
        win.ShowDialog();
        _ = CarregarKpisAsync();
    }

    private void Produtos_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<ProdutosWindow>();
        win.Owner = this;
        win.ShowDialog();
        _ = CarregarKpisAsync();
    }

    private void Fornecedores_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<FornecedoresWindow>();
        win.Owner = this;
        win.ShowDialog();
        _ = CarregarKpisAsync();
    }

    private void Aquisicao_Click(object sender, RoutedEventArgs e) => Fornecedores_Click(sender, e);

    private void CadeiaSuprimentos_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<CadeiaSuprimentosWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void Estoque_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<EstoqueWindow>();
        win.Owner = this;
        win.ShowDialog();
        _ = CarregarKpisAsync();
    }

    private void Financeiro_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<FinanceiroWindow>();
        win.Owner = this;
        win.ShowDialog();
        _ = CarregarKpisAsync();
    }

    private void Armazem_Click(object sender, RoutedEventArgs e) => Estoque_Click(sender, e);

    private void Pedidos_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<PedidosWindow>();
        win.Owner = this;
        win.ShowDialog();
        _ = CarregarKpisAsync();
    }

    private void Producao_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<ProducaoWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void Projetos_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<ProjetosWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void ForcaTrabalho_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<ForcaTrabalhoWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void Horas_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<ApontamentoHorasWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void Relatorios_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<RelatoriosWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void Auditoria_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<AuditoriaWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void Ecommerce_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<EcommerceWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void Marketing_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<MarketingWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void Indisponivel_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var win = scope.ServiceProvider.GetRequiredService<FiscalWindow>();
        win.Owner = this;
        win.ShowDialog();
    }

    private void AbrirModuloPlanejado(string nomeModulo)
    {
        MessageBox.Show(
            $"{nomeModulo} ja esta previsto na arquitetura do ERP e sera liberado na proxima etapa de implantacao.",
            "Modulo em implantacao",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Sair_Click(object sender, RoutedEventArgs e) => Close();
}
