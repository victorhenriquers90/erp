/*
PHASE 2.5 - CupomPrinterService refactoring deferred

This service currently depends on Infrastructure types directly, causing circular dependencies:
- ImpressoraConfig (Infrastructure)
- TipoImpressora (Infrastructure)
- EscPosPrinter (Infrastructure.Printing)
- EscPosBuilder (Infrastructure.Printing)
- QrCodeNfce (Infrastructure.Nfce)
- ChaveAcessoNfce (Infrastructure.Nfce)

Solution: Create IPrinterService interface in Application.Contracts and move printing logic to Infrastructure.
This will allow Application to depend on the interface instead of concrete Infrastructure implementations.

TODO: Implement proper printer abstraction and refactor this service

using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class CupomPrinterService
{
    public async Task<Result> ImprimirVendaAsync(Venda venda, EmpresaConfig empresa, NotaFiscal? nota = null)
    {
        try
        {
            var cfg = new ImpressoraConfig
            {
                Tipo = (TipoImpressora)empresa.ImpressoraTipo,
                Destino = empresa.ImpressoraDestino,
                Porta = empresa.ImpressoraPorta,
                Baud = empresa.ImpressoraBaud,
                Colunas = empresa.ImpressoraColunas
            };

            if (string.IsNullOrWhiteSpace(cfg.Destino))
                return Result.Falha("Impressora não configurada (Menu → Configurações).");

            var bytes = ConstruirCupom(venda, empresa, nota, cfg.Colunas);
            await EscPosPrinter.ImprimirAsync(bytes, cfg);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Falha("Erro ao imprimir: " + ex.Message);
        }
    }

    private byte[] ConstruirCupom(Venda venda, EmpresaConfig empresa, NotaFiscal? nota, int colunas)
    {
        var b = new EscPosBuilder();

        b.Centro().Negrito(true).Linha(empresa.NomeFantasia ?? empresa.RazaoSocial).Negrito(false);
        b.Linha(empresa.RazaoSocial);
        b.Linha($"CNPJ: {FormatarCnpj(empresa.Cnpj)}");
        b.Linha($"IE: {empresa.InscricaoEstadual}");
        b.Linha($"{empresa.Logradouro}, {empresa.Numero}");
        b.Linha($"{empresa.Bairro} - {empresa.Cidade}/{empresa.Uf}");
        if (!string.IsNullOrWhiteSpace(empresa.Telefone)) b.Linha($"Fone: {empresa.Telefone}");
        b.Pular();

        if (nota != null && nota.Status == StatusNotaFiscal.Autorizada)
        {
            b.Negrito(true).Linha($"DANFE NFC-e nº {nota.Numero} série {nota.Serie}").Negrito(false);
            if (empresa.AmbienteHomologacao)
                b.Negrito(true).Linha("*** HOMOLOGAÇÃO - SEM VALOR FISCAL ***").Negrito(false);
        }
        else
        {
            b.Negrito(true).Linha("CUPOM NÃO FISCAL").Negrito(false);
        }
        b.Linha($"Venda: {venda.Numero}   {venda.FinalizadaEm:dd/MM/yyyy HH:mm}");
        b.Separador(colunas);

        b.Esquerda();
        b.Linha("CÓD DESCRIÇÃO");
        b.Linha("  QTD x UN.    VL UN     TOTAL");
        b.Separador(colunas);
        foreach (var item in venda.Itens)
        {
            var desc = item.Produto.Descricao;
            if (desc.Length > colunas - 5) desc = desc[..(colunas - 5)];
            b.Linha($"{Truncar(item.Produto.Codigo, 4)} {desc}");
            var linhaQtd = $"  {item.Quantidade,8:N3} x {item.PrecoUnitario,9:N2}   {item.Total,10:N2}";
            b.Linha(linhaQtd);
        }
        b.Separador(colunas);

        b.ColunaDupla("SUBTOTAL", venda.SubTotal.ToString("N2"), colunas);
        if (venda.Desconto > 0)
            b.ColunaDupla("DESCONTO", "-" + venda.Desconto.ToString("N2"), colunas);
        b.Negrito(true);
        b.ColunaDupla("TOTAL R$", venda.Total.ToString("N2"), colunas);
        b.Negrito(false);
        b.Pular();

        b.Linha("FORMAS DE PAGAMENTO:");
        foreach (var p in venda.Pagamentos)
            b.ColunaDupla(p.FormaPagamento.ToString(), p.Valor.ToString("N2"), colunas);
        if (venda.Troco > 0)
            b.ColunaDupla("TROCO", venda.Troco.ToString("N2"), colunas);
        b.Pular();

        if (nota != null && nota.Status == StatusNotaFiscal.Autorizada && !string.IsNullOrWhiteSpace(nota.ChaveAcesso))
        {
            b.Separador(colunas);
            b.Centro();
            b.Linha("Consulte pela chave de acesso em:");
            b.Linha(QrCodeNfce.UrlConsulta(empresa.AmbienteHomologacao));
            b.Pular();
            b.Linha("CHAVE DE ACESSO:");
            b.Linha(FormatarChave(nota.ChaveAcesso));
            if (!string.IsNullOrWhiteSpace(nota.Protocolo))
                b.Linha($"Protocolo: {nota.Protocolo}");
            b.Pular();

            try
            {
                var urlQr = QrCodeNfce.GerarUrl(
                    nota.ChaveAcesso, empresa.AmbienteHomologacao,
                    empresa.CscId, empresa.CscToken,
                    nota.AutorizadaEm, venda.Total);
                b.QrCode(urlQr, 6);
                b.Pular();
            }
            catch { }
        }

        b.Centro().Linha("Obrigado e volte sempre!");
        b.Pular(3);
        b.Cortar();
        return b.Build();
    }

    private static string Truncar(string s, int n) => s.Length > n ? s[..n] : s.PadRight(n);

    private static string FormatarCnpj(string cnpj)
    {
        var d = ChaveAcessoNfce.SoNumeros(cnpj);
        return d.Length == 14
            ? $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d.Substring(12, 2)}"
            : cnpj;
    }

    private static string FormatarChave(string chave)
    {
        if (chave.Length != 44) return chave;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < chave.Length; i += 4)
            sb.Append(chave.Substring(i, 4)).Append(' ');
        return sb.ToString().Trim();
    }
}
*/
