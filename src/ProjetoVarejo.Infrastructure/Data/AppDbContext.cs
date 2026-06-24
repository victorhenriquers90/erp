using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Domain.Configuracao;

namespace ProjetoVarejo.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioPermissao> UsuarioPermissoes => Set<UsuarioPermissao>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<PagamentoVenda> PagamentosVenda => Set<PagamentoVenda>();
    public DbSet<MovimentoEstoque> MovimentosEstoque => Set<MovimentoEstoque>();
    public DbSet<ContaFinanceira> ContasFinanceiras => Set<ContaFinanceira>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<CaixaSessao> CaixasSessao => Set<CaixaSessao>();
    public DbSet<MovimentoCaixa> MovimentosCaixa => Set<MovimentoCaixa>();
    public DbSet<EmpresaConfig> EmpresaConfigs => Set<EmpresaConfig>();
    public DbSet<ConfiguracaoNegocio> ConfiguracaoNegocio => Set<ConfiguracaoNegocio>();
    public DbSet<Filial> Filiais => Set<Filial>();
    public DbSet<PedidoCompra> PedidosCompra => Set<PedidoCompra>();
    public DbSet<ItemPedidoCompra> ItensPedidoCompra => Set<ItemPedidoCompra>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        ConfiguraDecimal(b);

        b.Entity<Categoria>(e =>
        {
            e.Property(x => x.Nome).HasMaxLength(80).IsRequired();
            e.HasIndex(x => x.Nome).IsUnique();
        });

        b.Entity<Produto>(e =>
        {
            e.Property(x => x.Codigo).HasMaxLength(30).IsRequired();
            e.Property(x => x.CodigoBarras).HasMaxLength(50);
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.Property(x => x.Ncm).HasMaxLength(10);
            e.Property(x => x.Cest).HasMaxLength(10);
            e.Property(x => x.Cfop).HasMaxLength(4);
            e.Property(x => x.Origem).HasMaxLength(1);
            e.Property(x => x.CstIcms).HasMaxLength(3);
            e.Property(x => x.CstPisCofins).HasMaxLength(2);
            e.HasIndex(x => x.Codigo).IsUnique();
            e.HasIndex(x => x.CodigoBarras);
            e.HasOne(x => x.Categoria).WithMany(c => c.Produtos)
                .HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<Cliente>(e =>
        {
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.CpfCnpj).HasMaxLength(20);
            e.Property(x => x.Email).HasMaxLength(150);
            e.HasIndex(x => x.CpfCnpj);
        });

        b.Entity<Fornecedor>(e =>
        {
            e.Property(x => x.RazaoSocial).HasMaxLength(150).IsRequired();
            e.Property(x => x.NomeFantasia).HasMaxLength(150);
            e.Property(x => x.Cnpj).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Cnpj).IsUnique();
        });

        b.Entity<Filial>(e =>
        {
            e.Property(x => x.Codigo).HasMaxLength(10).IsRequired();
            e.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            e.Property(x => x.Cnpj).HasMaxLength(20);
            e.Property(x => x.Endereco).HasMaxLength(300);
            e.Property(x => x.Telefone).HasMaxLength(20);
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        b.Entity<Usuario>(e =>
        {
            e.Property(x => x.Login).HasMaxLength(50).IsRequired();
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.SenhaHash).HasMaxLength(200).IsRequired();
            e.HasIndex(x => x.Login).IsUnique();
            e.HasOne(x => x.Filial).WithMany(f => f.Usuarios)
                .HasForeignKey(x => x.FilialId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<UsuarioPermissao>(e =>
        {
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.UsuarioId, x.Permissao }).IsUnique();
        });

        b.Entity<AuditLog>(e =>
        {
            e.Property(x => x.Entidade).HasMaxLength(80).IsRequired();
            e.Property(x => x.RegistroId).HasMaxLength(40);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => x.Data);
            e.HasIndex(x => new { x.Entidade, x.RegistroId });
        });

        b.Entity<Venda>(e =>
        {
            e.Property(x => x.Numero).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Numero).IsUnique();
            e.HasOne(x => x.Cliente).WithMany(c => c.Vendas)
                .HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Usuario).WithMany()
                .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NotaFiscal).WithOne(n => n.Venda)
                .HasForeignKey<Venda>(x => x.NotaFiscalId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ItemVenda>(e =>
        {
            e.HasOne(x => x.Venda).WithMany(v => v.Itens)
                .HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Produto).WithMany(p => p.ItensVenda)
                .HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PagamentoVenda>(e =>
        {
            e.HasOne(x => x.Venda).WithMany(v => v.Pagamentos)
                .HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<MovimentoEstoque>(e =>
        {
            e.HasOne(x => x.Produto).WithMany(p => p.Movimentos)
                .HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Usuario).WithMany()
                .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Venda).WithMany()
                .HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Fornecedor).WithMany()
                .HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<ContaFinanceira>(e =>
        {
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Cliente).WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Fornecedor).WithMany().HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Venda).WithMany().HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.SetNull);
        });

        b.Entity<NotaFiscal>(e =>
        {
            e.Property(x => x.ChaveAcesso).HasMaxLength(44);
            e.HasIndex(x => x.ChaveAcesso);
        });

        b.Entity<CaixaSessao>(e =>
        {
            e.HasOne(x => x.UsuarioAbertura).WithMany().HasForeignKey(x => x.UsuarioAberturaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.UsuarioFechamento).WithMany().HasForeignKey(x => x.UsuarioFechamentoId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<MovimentoCaixa>(e =>
        {
            e.HasOne(x => x.CaixaSessao).WithMany().HasForeignKey(x => x.CaixaSessaoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Venda).WithMany().HasForeignKey(x => x.VendaId).OnDelete(DeleteBehavior.SetNull);
            e.HasIndex(x => new { x.CaixaSessaoId, x.Tipo });
        });

        b.Entity<ConfiguracaoNegocio>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.DescricaoNegocio).HasMaxLength(200);
            e.Property(x => x.Versao).HasDefaultValue(1);
            e.Property(x => x.ModulosAtivos).IsRequired();
            e.Property(x => x.DataAtualizacao).HasDefaultValueSql("GETUTCDATE()");
        });

        b.Entity<PedidoCompra>(e =>
        {
            e.Property(x => x.Numero).HasMaxLength(30).IsRequired();
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.Property(x => x.Observacao).HasMaxLength(500);
            e.HasIndex(x => x.Numero).IsUnique();
            e.HasOne(x => x.Fornecedor).WithMany()
                .HasForeignKey(x => x.FornecedorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ItemPedidoCompra>(e =>
        {
            e.Property(x => x.Descricao).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.PedidoCompra).WithMany(p => p.Itens)
                .HasForeignKey(x => x.PedidoCompraId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Produto).WithMany()
                .HasForeignKey(x => x.ProdutoId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfiguraDecimal(ModelBuilder b)
    {
        foreach (var entity in b.Model.GetEntityTypes())
        {
            foreach (var prop in entity.GetProperties())
            {
                if (prop.ClrType == typeof(decimal) || prop.ClrType == typeof(decimal?))
                {
                    prop.SetColumnType("decimal(18,4)");
                }
            }
        }
    }
}
