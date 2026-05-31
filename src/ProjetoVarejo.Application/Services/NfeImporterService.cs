using ProjetoVarejo.Application.Contracts.Repositories;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class NfeImporterService
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";
    private readonly IUnitOfWork _unitOfWork;
    private readonly SessaoApp _sessao;

    public NfeImporterService(IUnitOfWork unitOfWork, SessaoApp sessao)
    {
        _unitOfWork = unitOfWork; _sessao = sessao;
    }

    public class ItemImportacao
    {
        public string Codigo { get; set; } = "";
        public string CodigoBarras { get; set; } = "";
        public string Descricao { get; set; } = "";
        public string Ncm { get; set; } = "";
        public string Unidade { get; set; } = "UN";
        public decimal Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
        public int? ProdutoIdExistente { get; set; }
        public string? DescricaoExistente { get; set; }
    }

    public class PreviaImportacao
    {
        public string ChaveAcesso { get; set; } = "";
        public int Numero { get; set; }
        public string CnpjEmitente { get; set; } = "";
        public string RazaoSocialEmitente { get; set; } = "";
        public DateTime? DataEmissao { get; set; }
        public decimal ValorTotal { get; set; }
        public Fornecedor? FornecedorExistente { get; set; }
        public List<ItemImportacao> Itens { get; set; } = new();
        public List<DateTime> VencimentosDuplicatas { get; set; } = new();
        public List<decimal> ValoresDuplicatas { get; set; } = new();
    }

    public async Task<Result<PreviaImportacao>> ParsearAsync(string caminhoXml)
    {
        if (!File.Exists(caminhoXml)) return Result.Falha<PreviaImportacao>("Arquivo não encontrado.");
        try
        {
            var doc = XDocument.Load(caminhoXml);
            var infNFe = doc.Descendants(Ns + "infNFe").FirstOrDefault();
            if (infNFe == null) return Result.Falha<PreviaImportacao>("XML não é uma NFe válida (não encontrou infNFe).");

            var chave = (infNFe.Attribute("Id")?.Value ?? "").Replace("NFe", "");
            var ide = infNFe.Element(Ns + "ide");
            var emit = infNFe.Element(Ns + "emit");
            var total = infNFe.Element(Ns + "total")?.Element(Ns + "ICMSTot");
            var cobr = infNFe.Element(Ns + "cobr");

            var previa = new PreviaImportacao
            {
                ChaveAcesso = chave,
                Numero = int.TryParse(ide?.Element(Ns + "nNF")?.Value, out var n) ? n : 0,
                DataEmissao = DateTime.TryParse(ide?.Element(Ns + "dhEmi")?.Value, out var d) ? d : null,
                CnpjEmitente = emit?.Element(Ns + "CNPJ")?.Value ?? "",
                RazaoSocialEmitente = emit?.Element(Ns + "xNome")?.Value ?? "",
                ValorTotal = ParseDec(total?.Element(Ns + "vNF")?.Value)
            };

            if (!string.IsNullOrWhiteSpace(previa.CnpjEmitente))
                previa.FornecedorExistente = await _unitOfWork.Fornecedores.Query()
                    .FirstOrDefaultAsync(f => f.Cnpj == previa.CnpjEmitente);

            foreach (var det in infNFe.Elements(Ns + "det"))
            {
                var prod = det.Element(Ns + "prod");
                if (prod == null) continue;

                var item = new ItemImportacao
                {
                    Codigo = prod.Element(Ns + "cProd")?.Value ?? "",
                    CodigoBarras = NormalizarEan(prod.Element(Ns + "cEAN")?.Value),
                    Descricao = prod.Element(Ns + "xProd")?.Value ?? "",
                    Ncm = prod.Element(Ns + "NCM")?.Value ?? "",
                    Unidade = prod.Element(Ns + "uCom")?.Value ?? "UN",
                    Quantidade = ParseDec(prod.Element(Ns + "qCom")?.Value),
                    ValorUnitario = ParseDec(prod.Element(Ns + "vUnCom")?.Value),
                    ValorTotal = ParseDec(prod.Element(Ns + "vProd")?.Value)
                };

                var existente = await BuscarProdutoExistenteAsync(item.CodigoBarras, item.Codigo);
                if (existente != null)
                {
                    item.ProdutoIdExistente = existente.Id;
                    item.DescricaoExistente = existente.Descricao;
                }
                previa.Itens.Add(item);
            }

            if (cobr != null)
            {
                foreach (var dup in cobr.Elements(Ns + "dup"))
                {
                    var v = ParseDec(dup.Element(Ns + "vDup")?.Value);
                    if (DateTime.TryParse(dup.Element(Ns + "dVenc")?.Value, out var venc))
                    {
                        previa.VencimentosDuplicatas.Add(venc);
                        previa.ValoresDuplicatas.Add(v);
                    }
                }
            }

            return Result.Ok(previa);
        }
        catch (Exception ex)
        {
            return Result.Falha<PreviaImportacao>("Erro ao parsear XML: " + ex.Message);
        }
    }

    private async Task<Produto?> BuscarProdutoExistenteAsync(string ean, string codigoFornecedor)
    {
        if (!string.IsNullOrWhiteSpace(ean))
        {
            var p = await _unitOfWork.Produtos.Query().FirstOrDefaultAsync(x => x.CodigoBarras == ean);
            if (p != null) return p;
        }
        return null;
    }

    public async Task<Result<int>> ImportarAsync(PreviaImportacao previa, bool criarContaPagar, bool criarProdutosNovos)
    {
        if (_sessao.UsuarioLogado == null) return Result.Falha<int>("Usuário não autenticado.");
        if (!previa.Itens.Any()) return Result.Falha<int>("XML sem itens.");

        var jaImportada = await _unitOfWork.MovimentosEstoque.Query().AnyAsync(m =>
            m.Documento != null && m.Documento.Contains(previa.ChaveAcesso));
        if (jaImportada)
            return Result.Falha<int>("Este XML já foi importado anteriormente (movimentação existente com a chave).");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var fornecedor = previa.FornecedorExistente;
            if (fornecedor == null)
            {
                fornecedor = new Fornecedor
                {
                    RazaoSocial = previa.RazaoSocialEmitente,
                    Cnpj = previa.CnpjEmitente
                };
                await _unitOfWork.Fornecedores.InsertAsync(fornecedor);
                await _unitOfWork.SaveChangesAsync();
            }

            int produtosCriados = 0;
            int movimentosCriados = 0;
            foreach (var item in previa.Itens)
            {
                Produto? produto = item.ProdutoIdExistente.HasValue
                    ? await _unitOfWork.Produtos.Query().FirstOrDefaultAsync(p => p.Id == item.ProdutoIdExistente.Value)
                    : null;

                if (produto == null)
                {
                    if (!criarProdutosNovos)
                        return Result.Falha<int>($"Produto '{item.Descricao}' não cadastrado e criação automática desativada.");

                    produto = new Produto
                    {
                        Codigo = item.Codigo,
                        CodigoBarras = string.IsNullOrWhiteSpace(item.CodigoBarras) ? null : item.CodigoBarras,
                        Descricao = item.Descricao,
                        Ncm = item.Ncm,
                        Unidade = Enum.TryParse<UnidadeMedida>(item.Unidade, true, out var u) ? u : UnidadeMedida.UN,
                        PrecoCusto = item.ValorUnitario,
                        PrecoVenda = Math.Round(item.ValorUnitario * 1.30m, 2),
                        ControlaEstoque = true
                    };
                    await _unitOfWork.Produtos.InsertAsync(produto);
                    await _unitOfWork.SaveChangesAsync();
                    produtosCriados++;
                }

                var saldoAnterior = produto.Estoque;
                produto.Estoque += item.Quantidade;
                produto.PrecoCusto = item.ValorUnitario;
                produto.AtualizadoEm = DateTime.Now;

                await _unitOfWork.MovimentosEstoque.InsertAsync(new MovimentoEstoque
                {
                    ProdutoId = produto.Id,
                    Tipo = TipoMovimentoEstoque.Entrada,
                    Quantidade = item.Quantidade,
                    SaldoAnterior = saldoAnterior,
                    SaldoAtual = produto.Estoque,
                    CustoUnitario = item.ValorUnitario,
                    Documento = $"NF-e {previa.Numero} (chave {previa.ChaveAcesso})",
                    FornecedorId = fornecedor.Id,
                    UsuarioId = _sessao.UsuarioLogado.Id,
                    Observacao = "Importação XML"
                });
                movimentosCriados++;
            }

            if (criarContaPagar)
            {
                if (previa.ValoresDuplicatas.Any())
                {
                    for (int i = 0; i < previa.ValoresDuplicatas.Count; i++)
                    {
                        await _unitOfWork.ContasFinanceiras.InsertAsync(new ContaFinanceira
                        {
                            Tipo = TipoConta.Pagar,
                            Descricao = $"NF-e {previa.Numero} - {fornecedor.RazaoSocial} - parcela {i + 1}/{previa.ValoresDuplicatas.Count}",
                            DocumentoNumero = previa.Numero.ToString(),
                            DataEmissao = previa.DataEmissao ?? DateTime.Now,
                            DataVencimento = previa.VencimentosDuplicatas[i],
                            Valor = previa.ValoresDuplicatas[i],
                            FornecedorId = fornecedor.Id
                        });
                    }
                }
                else
                {
                    await _unitOfWork.ContasFinanceiras.InsertAsync(new ContaFinanceira
                    {
                        Tipo = TipoConta.Pagar,
                        Descricao = $"NF-e {previa.Numero} - {fornecedor.RazaoSocial}",
                        DocumentoNumero = previa.Numero.ToString(),
                        DataEmissao = previa.DataEmissao ?? DateTime.Now,
                        DataVencimento = (previa.DataEmissao ?? DateTime.Now).AddDays(30),
                        Valor = previa.ValorTotal,
                        FornecedorId = fornecedor.Id
                    });
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
            return Result.Ok(movimentosCriados);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            return Result.Falha<int>("Falha na importação: " + ex.Message);
        }
    }

    private static decimal ParseDec(string? s) =>
        decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
    private static string NormalizarEan(string? s) =>
        string.IsNullOrWhiteSpace(s) || s == "SEM GTIN" ? "" : s.Trim();
}
