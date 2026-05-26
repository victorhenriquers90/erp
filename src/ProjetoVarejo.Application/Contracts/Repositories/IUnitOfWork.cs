using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Domain.Configuracao;

namespace ProjetoVarejo.Application.Contracts.Repositories;

/// <summary>
/// Unit of Work Pattern - Coordena múltiplos repositórios e gerencia transações
/// Garante consistência de dados em operações que envolvem múltiplas entidades
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repositórios por entidade
    IRepository<Produto> Produtos { get; }
    IRepository<Cliente> Clientes { get; }
    IRepository<Fornecedor> Fornecedores { get; }
    IRepository<Categoria> Categorias { get; }
    IRepository<Venda> Vendas { get; }
    IRepository<ItemVenda> ItensVenda { get; }
    IRepository<PagamentoVenda> PagamentosVenda { get; }
    IRepository<MovimentoEstoque> MovimentosEstoque { get; }
    IRepository<Usuario> Usuarios { get; }
    IRepository<CaixaSessao> CaixaSessoes { get; }
    IRepository<MovimentoCaixa> MovimentosCaixa { get; }
    IRepository<ContaFinanceira> ContasFinanceiras { get; }
    IRepository<EmpresaConfig> Configuracoes { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<UsuarioPermissao> UsuarioPermissoes { get; }
    IRepository<NotaFiscal> NotasFiscais { get; }
    IRepository<ConfiguracaoNegocio> ConfiguracoesNegocio { get; }

    /// <summary>
    /// Salva todas as mudanças pendentes no banco de dados
    /// </summary>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Inicia uma transação no banco de dados
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Confirma a transação (commit)
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// Desfaz a transação (rollback)
    /// </summary>
    Task RollbackAsync();
}
