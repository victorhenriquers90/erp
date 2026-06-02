using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

/// <summary>
/// Popula dados de demonstração (clientes, fornecedores, contas) para apresentações.
/// Idempotente: só insere quando há poucos registros. Marca tudo com [DEMO].
/// </summary>
public class DadosDemoService
{
    private readonly AppDbContext _db;

    public DadosDemoService(AppDbContext db) => _db = db;

    public async Task<Result<string>> PopularAsync()
    {
        int addClientes = 0, addFornecedores = 0, addContas = 0;

        if (await _db.Clientes.CountAsync() < 5)
        {
            _db.Clientes.AddRange(NovosClientes());
            addClientes = 8;
        }

        if (await _db.Fornecedores.CountAsync() < 5)
        {
            _db.Fornecedores.AddRange(NovosFornecedores());
            addFornecedores = 6;
        }

        await _db.SaveChangesAsync();

        if (await _db.ContasFinanceiras.CountAsync() < 5)
        {
            var fornecedorId = await _db.Fornecedores.OrderBy(f => f.Id).Select(f => (int?)f.Id).FirstOrDefaultAsync();
            var clienteId = await _db.Clientes.OrderBy(c => c.Id).Select(c => (int?)c.Id).FirstOrDefaultAsync();
            _db.ContasFinanceiras.AddRange(NovasContas(fornecedorId, clienteId));
            addContas = 10;
            await _db.SaveChangesAsync();
        }

        if (addClientes == 0 && addFornecedores == 0 && addContas == 0)
            return Result.Ok("Os dados já estão preenchidos — nada a adicionar.");

        return Result.Ok($"Adicionados: {addClientes} clientes, {addFornecedores} fornecedores, {addContas} contas.");
    }

    private static List<Cliente> NovosClientes() =>
    [
        Cli("Maria Aparecida Souza", "123.456.789-09", "11 98877-1020", "maria.souza@email.com", "São Paulo"),
        Cli("João Pedro Almeida", "987.654.321-00", "11 97766-3040", "jp.almeida@email.com", "Guarulhos"),
        Cli("Ana Carolina Lima", "456.789.123-11", "11 96655-5060", "ana.lima@email.com", "Osasco"),
        Cli("Carlos Eduardo Ferreira", "321.654.987-22", "11 95544-7080", "cadu.ferreira@email.com", "Santo André"),
        Cli("Mercado Bom Preço Ltda", "12.345.678/0001-95", "11 3344-9000", "contato@bompreco.com.br", "São Paulo", pj: true),
        Cli("Padaria Pão Quente ME", "98.765.432/0001-10", "11 3322-8000", "padaria@paoquente.com.br", "Diadema", pj: true),
        Cli("Fernanda Ribeiro Costa", "654.321.789-33", "11 94433-1122", "fernanda.costa@email.com", "São Bernardo"),
        Cli("Roberto Carlos Dias", "789.123.456-44", "11 93322-3344", "roberto.dias@email.com", "São Paulo"),
    ];

    private static Cliente Cli(string nome, string doc, string tel, string email, string cidade, bool pj = false) => new()
    {
        Nome = nome,
        CpfCnpj = doc,
        PessoaJuridica = pj,
        Telefone = tel,
        Email = email,
        Cidade = cidade,
        Uf = "SP",
        Ativo = true,
        Observacao = "[DEMO]"
    };

    private static List<Fornecedor> NovosFornecedores() =>
    [
        Forn("Distribuidora Central de Alimentos S.A.", "Central Alimentos", "11.222.333/0001-81", "11 4002-1000", "Campinas"),
        Forn("Bebidas & Cia Comércio Ltda", "Bebidas & Cia", "22.333.444/0001-72", "11 4002-2000", "São Paulo"),
        Forn("Higiene Total Indústria Ltda", "Higiene Total", "33.444.555/0001-63", "11 4002-3000", "Sorocaba"),
        Forn("Embalagens Rápidas EIRELI", "Embalagens Rápidas", "44.555.666/0001-54", "11 4002-4000", "Jundiaí"),
        Forn("FrigoSul Carnes e Derivados Ltda", "FrigoSul", "55.666.777/0001-45", "11 4002-5000", "São Paulo"),
        Forn("Limpa Mais Produtos de Limpeza Ltda", "Limpa Mais", "66.777.888/0001-36", "11 4002-6000", "Osasco"),
    ];

    private static Fornecedor Forn(string razao, string fantasia, string cnpj, string tel, string cidade) => new()
    {
        RazaoSocial = razao,
        NomeFantasia = fantasia,
        Cnpj = cnpj,
        Telefone = tel,
        Cidade = cidade,
        Uf = "SP",
        Ativo = true,
        Observacao = "[DEMO]"
    };

    private static List<ContaFinanceira> NovasContas(int? fornecedorId, int? clienteId)
    {
        var hoje = DateTime.Today;
        return
        [
            Conta(TipoConta.Pagar, "Compra de mercadorias - NF 12345", 4850.00m, hoje.AddDays(-5), StatusConta.EmAberto, fornecedorId: fornecedorId),
            Conta(TipoConta.Pagar, "Energia elétrica - mês corrente", 1320.50m, hoje.AddDays(3), StatusConta.EmAberto),
            Conta(TipoConta.Pagar, "Aluguel da loja", 6500.00m, hoje.AddDays(8), StatusConta.EmAberto),
            Conta(TipoConta.Pagar, "Fornecedor de embalagens - NF 8890", 980.00m, hoje.AddDays(-12), StatusConta.Paga, paga: true, fornecedorId: fornecedorId),
            Conta(TipoConta.Pagar, "Internet e telefonia", 299.90m, hoje.AddDays(15), StatusConta.EmAberto),
            Conta(TipoConta.Receber, "Venda a prazo - cliente PJ", 2300.00m, hoje.AddDays(10), StatusConta.EmAberto, clienteId: clienteId),
            Conta(TipoConta.Receber, "Venda parcelada 1/3", 750.00m, hoje.AddDays(2), StatusConta.EmAberto, clienteId: clienteId),
            Conta(TipoConta.Receber, "Venda parcelada 2/3", 750.00m, hoje.AddDays(32), StatusConta.EmAberto, clienteId: clienteId),
            Conta(TipoConta.Receber, "Venda a prazo - recebida", 1480.00m, hoje.AddDays(-8), StatusConta.Paga, paga: true, clienteId: clienteId),
            Conta(TipoConta.Pagar, "Manutenção de equipamentos", 540.00m, hoje.AddDays(-2), StatusConta.EmAberto),
        ];
    }

    private static ContaFinanceira Conta(TipoConta tipo, string descricao, decimal valor, DateTime venc,
        StatusConta status, bool paga = false, int? fornecedorId = null, int? clienteId = null) => new()
    {
        Tipo = tipo,
        Descricao = descricao,
        Valor = valor,
        ValorPago = paga ? valor : 0,
        DataEmissao = venc.AddDays(-20),
        DataVencimento = venc,
        DataPagamento = paga ? venc.AddDays(-1) : null,
        Status = status,
        FornecedorId = fornecedorId,
        ClienteId = clienteId,
        Ativo = true,
        Observacao = "[DEMO]"
    };
}
