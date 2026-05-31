using Moq;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Domain.Configuracao;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Tests.Mocking;

/// <summary>
/// Factory for creating Moq-based IUnitOfWork mock objects for unit testing.
///
/// PHASE 4: Replaces TestDbFactory with lightweight Moq-based mocking for faster, isolated tests.
/// Each mock repository returns empty data by default - customize with .Setup() for specific tests.
///
/// Usage:
/// var mockUow = MockUnitOfWorkFactory.CreateMock();
/// var mockVendas = new MockRepositoryBuilder<Venda>().WithData(testVendas).Build();
/// mockUow.Setup(u => u.Vendas).Returns(mockVendas.Object);
/// </summary>
public class MockUnitOfWorkFactory
{
    public static Mock<IUnitOfWork> CreateMock()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        // Setup all repository properties with empty data by default
        mockUnitOfWork.Setup(u => u.Vendas).Returns(CreateMockRepository<Venda>().Object);
        mockUnitOfWork.Setup(u => u.Produtos).Returns(CreateMockRepository<Produto>().Object);
        mockUnitOfWork.Setup(u => u.Categorias).Returns(CreateMockRepository<Categoria>().Object);
        mockUnitOfWork.Setup(u => u.Clientes).Returns(CreateMockRepository<Cliente>().Object);
        mockUnitOfWork.Setup(u => u.Fornecedores).Returns(CreateMockRepository<Fornecedor>().Object);
        mockUnitOfWork.Setup(u => u.Usuarios).Returns(CreateMockRepository<Usuario>().Object);
        mockUnitOfWork.Setup(u => u.ItensVenda).Returns(CreateMockRepository<ItemVenda>().Object);
        mockUnitOfWork.Setup(u => u.PagamentosVenda).Returns(CreateMockRepository<PagamentoVenda>().Object);
        mockUnitOfWork.Setup(u => u.CaixaSessoes).Returns(CreateMockRepository<CaixaSessao>().Object);
        mockUnitOfWork.Setup(u => u.MovimentosCaixa).Returns(CreateMockRepository<MovimentoCaixa>().Object);
        mockUnitOfWork.Setup(u => u.Configuracoes).Returns(CreateMockRepository<EmpresaConfig>().Object);
        mockUnitOfWork.Setup(u => u.NotasFiscais).Returns(CreateMockRepository<NotaFiscal>().Object);
        mockUnitOfWork.Setup(u => u.MovimentosEstoque).Returns(CreateMockRepository<MovimentoEstoque>().Object);
        mockUnitOfWork.Setup(u => u.ContasFinanceiras).Returns(CreateMockRepository<ContaFinanceira>().Object);
        mockUnitOfWork.Setup(u => u.AuditLogs).Returns(CreateMockRepository<AuditLog>().Object);
        mockUnitOfWork.Setup(u => u.UsuarioPermissoes).Returns(CreateMockRepository<UsuarioPermissao>().Object);
        mockUnitOfWork.Setup(u => u.ConfiguracoesNegocio).Returns(CreateMockRepository<ConfiguracaoNegocio>().Object);

        // Setup SaveChangesAsync (returns 0 by default, can be overridden)
        mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(0);

        // Setup Transaction methods
        mockUnitOfWork.Setup(u => u.BeginTransactionAsync()).Returns(() => Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.CommitAsync()).Returns(() => Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.RollbackAsync()).Returns(() => Task.CompletedTask);

        return mockUnitOfWork;
    }

    private static Mock<IRepository<T>> CreateMockRepository<T>() where T : class
    {
        var mockRepo = new Mock<IRepository<T>>();
        // Default: empty list for queries
        mockRepo.Setup(r => r.Query()).Returns(new List<T>().AsQueryable());
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((T?)null);
        mockRepo.Setup(r => r.GetAllAsync(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(new List<T>());
        mockRepo.Setup(r => r.InsertAsync(It.IsAny<T>()))
            .Returns((T entity) => Task.FromResult(entity));
        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<T>()))
            .Returns((T entity) => Task.FromResult(entity));
        mockRepo.Setup(r => r.DeleteAsync(It.IsAny<int>()))
            .ReturnsAsync(true);
        return mockRepo;
    }
}

/// <summary>
/// Helper for building mock repositories with test data.
///
/// Usage:
/// var mockVendas = new MockRepositoryBuilder<Venda>()
///     .WithData(testVendas)
///     .Build();
/// </summary>
public class MockRepositoryBuilder<T> where T : class
{
    private readonly List<T> _data = new();
    private Mock<IRepository<T>>? _mock;

    public MockRepositoryBuilder<T> WithData(List<T> data)
    {
        _data.Clear();
        _data.AddRange(data);
        return this;
    }

    public MockRepositoryBuilder<T> WithData(params T[] data)
    {
        return WithData(data.ToList());
    }

    public Mock<IRepository<T>> Build()
    {
        _mock = new Mock<IRepository<T>>();
        _mock.Setup(r => r.Query()).Returns(_data.AsQueryable());
        _mock.Setup(r => r.GetAllAsync(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(_data);

        _mock.Setup(r => r.InsertAsync(It.IsAny<T>()))
            .Returns((T entity) =>
            {
                _data.Add(entity);
                return Task.FromResult(entity);
            });

        _mock.Setup(r => r.UpdateAsync(It.IsAny<T>()))
            .Returns((T entity) => Task.FromResult(entity));

        _mock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => _data.FirstOrDefault());

        return _mock;
    }
}
