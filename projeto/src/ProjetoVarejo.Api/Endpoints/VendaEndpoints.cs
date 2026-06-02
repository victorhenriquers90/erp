using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Api.Endpoints;

public static class VendaEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/vendas").WithTags("Vendas");

        g.MapGet("/", async (VendaService svc, DateTime? de, DateTime? ate, StatusVenda? status) =>
        {
            var lista = await svc.ListarAsync(de, ate, status);
            return Results.Ok(lista.Select(v => new
            {
                v.Id, v.Numero, v.DataVenda,
                Cliente = v.Cliente?.Nome,
                v.SubTotal, v.Desconto, v.Total, v.ValorPago, v.Troco,
                Status = v.Status.ToString(),
                v.FinalizadaEm, v.CanceladaEm
            }));
        });

        g.MapGet("/resumo", async (VendaService svc, DateTime de, DateTime ate) =>
        {
            var lista = await svc.ListarAsync(de, ate, StatusVenda.Finalizada);
            var total = lista.Sum(v => v.Total);
            var quantidade = lista.Count;
            var ticketMedio = quantidade > 0 ? total / quantidade : 0m;
            return Results.Ok(new { total, quantidade, ticketMedio, de, ate });
        });

        g.MapGet("/{id:int}", async (int id, VendaService svc) =>
        {
            var v = await svc.BuscarAsync(id);
            if (v == null) return Results.NotFound(new { erro = "Venda não encontrada." });
            return Results.Ok(new
            {
                v.Id, v.Numero, v.DataVenda,
                Cliente = v.Cliente == null ? null : (object)new { v.Cliente.Id, v.Cliente.Nome, v.Cliente.CpfCnpj },
                Usuario = new { v.Usuario.Id, v.Usuario.Nome },
                v.SubTotal, v.Desconto, v.Acrescimo, v.Total, v.ValorPago, v.Troco,
                Status = v.Status.ToString(),
                v.Observacao, v.FinalizadaEm, v.CanceladaEm,
                Itens = v.Itens.Select(i => new
                {
                    i.Id,
                    Produto = i.Produto.Descricao,
                    i.Quantidade, i.PrecoUnitario, i.Total
                }),
                Pagamentos = v.Pagamentos.Select(p => new
                {
                    p.Id,
                    Forma = p.FormaPagamento.ToString(),
                    p.Valor
                })
            });
        });
    }
}
