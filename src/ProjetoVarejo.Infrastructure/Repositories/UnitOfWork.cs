using Microsoft.EntityFrameworkCore.Storage;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Domain.Configuracao;
using ProjetoVarejo.Infrastructure.Data;

namespace ProjetoVarejo.Infrastructure.Repositories;

/// <summary>
/// Implementação do Unit of Work Pattern.
/// Coordena múltiplos repositórios e gerencia transações para garantir consistência.
/// Cada repositório reutiliza o mesmo DbContext, garantindo mudanças coordenadas.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    // Repositórios - inicializados lazy
    private IRepository<Produto>? _produtos;
    private IRepository<Cliente>? _clientes;
    private IRepository<Fornecedor>? _fornecedores;
    private IRepository<Categoria>? _categorias;
    private IRepository<Venda>? _vendas;
    private IRepository<ItemVenda>? _itensVenda;
    private IRepository<PagamentoVenda>? _pagamentosVenda;
    private IRepository<MovimentoEstoque>? _movimentosEstoque;
    private IRepository<Usuario>? _usuarios;
    private IRepository<CaixaSessao>? _caixaSessoes;
    private IRepository<MovimentoCaixa>? _movimentosCaixa;
    private IRepository<ContaFinanceira>? _contasFinanceiras;
    private IRepository<EmpresaConfig>? _configuracoes;
    private IRepository<AuditLog>? _auditLogs;
    private IRepository<UsuarioPermissao>? _usuarioPermissoes;
    private IRepository<NotaFiscal>? _notasFiscais;
    private IRepository<ConfiguracaoNegocio>? _configuracoesNegocio;

    public UnitOfWork(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IRepository<Produto> Produtos =>
        _produtos ??= new GenericRepository<Produto>(_context);

    public IRepository<Cliente> Clientes =>
        _clientes ??= new GenericRepository<Cliente>(_context);

    public IRepository<Fornecedor> Fornecedores =>
        _fornecedores ??= new GenericRepository<Fornecedor>(_context);

    public IRepository<Categoria> Categorias =>
        _categorias ??= new GenericRepository<Categoria>(_context);

    public IRepository<Venda> Vendas =>
        _vendas ??= new GenericRepository<Venda>(_context);

    public IRepository<ItemVenda> ItensVenda =>
        _itensVenda ??= new GenericRepository<ItemVenda>(_context);

    public IRepository<PagamentoVenda> PagamentosVenda =>
        _pagamentosVenda ??= new GenericRepository<PagamentoVenda>(_context);

    public IRepository<MovimentoEstoque> MovimentosEstoque =>
        _movimentosEstoque ??= new GenericRepository<MovimentoEstoque>(_context);

    public IRepository<Usuario> Usuarios =>
        _usuarios ??= new GenericRepository<Usuario>(_context);

    public IRepository<CaixaSessao> CaixaSessoes =>
        _caixaSessoes ??= new GenericRepository<CaixaSessao>(_context);

    public IRepository<MovimentoCaixa> MovimentosCaixa =>
        _movimentosCaixa ??= new GenericRepository<MovimentoCaixa>(_context);

    public IRepository<ContaFinanceira> ContasFinanceiras =>
        _contasFinanceiras ??= new GenericRepository<ContaFinanceira>(_context);

    public IRepository<EmpresaConfig> Configuracoes =>
        _configuracoes ??= new GenericRepository<EmpresaConfig>(_context);

    public IRepository<AuditLog> AuditLogs =>
        _auditLogs ??= new GenericRepository<AuditLog>(_context);

    public IRepository<UsuarioPermissao> UsuarioPermissoes =>
        _usuarioPermissoes ??= new GenericRepository<UsuarioPermissao>(_context);

    public IRepository<NotaFiscal> NotasFiscais =>
        _notasFiscais ??= new GenericRepository<NotaFiscal>(_context);

    public IRepository<ConfiguracaoNegocio> ConfiguracoesNegocio =>
        _configuracoesNegocio ??= new GenericRepository<ConfiguracaoNegocio>(_context);

    /// <summary>
    /// Salva todas as mudanças pendentes
    /// </summary>
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Inicia transação para múltiplas operações coordenadas
    /// </summary>
    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// Confirma transação (commit)
    /// </summary>
    public async Task CommitAsync()
    {
        try
        {
            await SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
            _transaction = null;
        }
    }

    /// <summary>
    /// Desfaz transação (rollback)
    /// </summary>
    public async Task RollbackAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
            _transaction = null;
        }
    }

    /// <summary>
    /// Libera recursos
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }
}
