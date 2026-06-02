using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;

namespace ProjetoVarejo.Application.Services;

/// <summary>KPIs de uma filial individual consultados via API remota.</summary>
public record FilialStatus(
    int Id,
    string Nome,
    string? Apelido,
    string? UrlApi,
    bool Online,
    string Versao,
    decimal VendasHoje,
    int PedidosHoje,
    bool CaixaAberto,
    decimal SaldoPrevisto,
    int ContasAtrasadas,
    DateTime ConsultadaEm);

/// <summary>
/// Consulta todas as filiais registradas, faz ping na API de cada uma
/// e retorna KPIs para exibição no painel de rede.
/// </summary>
public class FilialPainelService
{
    private readonly AppDbContext _db;

    public FilialPainelService(AppDbContext db) => _db = db;

    public async Task<List<FilialStatus>> ObterStatusTodasAsync()
    {
        var empresas = await _db.EmpresaConfigs
            .Where(e => e.Ativo)
            .OrderBy(e => e.NomeFantasia.Length > 0 ? e.NomeFantasia : e.RazaoSocial)
            .ToListAsync();

        var tarefas = empresas.Select(e => ConsultarFilialAsync(e));
        var resultados = await Task.WhenAll(tarefas);
        return resultados.ToList();
    }

    public async Task<FilialStatus> ConsultarFilialAsync(EmpresaConfig empresa)
    {
        var nome = string.IsNullOrWhiteSpace(empresa.NomeFantasia)
            ? empresa.RazaoSocial : empresa.NomeFantasia;

        // Filial local (sem URL de API) — consulta direto no banco
        if (string.IsNullOrWhiteSpace(empresa.UrlApi))
        {
            return await ConsultarLocalAsync(empresa.Id, nome, empresa.Apelido);
        }

        // Filial remota — consulta via ApiClient
        return await ConsultarRemotaAsync(empresa.Id, nome, empresa.Apelido, empresa.UrlApi!);
    }

    private async Task<FilialStatus> ConsultarLocalAsync(int id, string nome, string? apelido)
    {
        var hoje = DateTime.Today;
        var amanha = hoje.AddDays(1);

        try
        {
            var vendasHoje = await _db.Vendas
                .Where(v => v.Status == Shared.StatusVenda.Finalizada
                         && v.FinalizadaEm >= hoje && v.FinalizadaEm < amanha)
                .SumAsync(v => (decimal?)v.Total) ?? 0;

            var pedidosHoje = await _db.Vendas
                .CountAsync(v => v.Status == Shared.StatusVenda.Finalizada
                              && v.FinalizadaEm >= hoje && v.FinalizadaEm < amanha);

            var caixaAberto = await _db.CaixasSessao
                .AnyAsync(c => c.FechadaEm == null);

            var de = hoje.AddDays(-30);
            var ate = hoje.AddDays(30);

            var receber = await _db.ContasFinanceiras
                .Where(c => (c.Status == Shared.StatusConta.EmAberto || c.Status == Shared.StatusConta.Atrasada)
                         && c.Tipo == Shared.TipoConta.Receber
                         && c.DataVencimento >= de && c.DataVencimento <= ate)
                .SumAsync(c => (decimal?)c.Valor) ?? 0;

            var pagar = await _db.ContasFinanceiras
                .Where(c => (c.Status == Shared.StatusConta.EmAberto || c.Status == Shared.StatusConta.Atrasada)
                         && c.Tipo == Shared.TipoConta.Pagar
                         && c.DataVencimento >= de && c.DataVencimento <= ate)
                .SumAsync(c => (decimal?)c.Valor) ?? 0;

            var atrasadas = await _db.ContasFinanceiras
                .CountAsync(c => c.Status == Shared.StatusConta.Atrasada);

            return new FilialStatus(id, nome, apelido, null, true, "local",
                vendasHoje, pedidosHoje, caixaAberto, receber - pagar, atrasadas, DateTime.Now);
        }
        catch
        {
            return new FilialStatus(id, nome, apelido, null, false, "-", 0, 0, false, 0, 0, DateTime.Now);
        }
    }

    private async Task<FilialStatus> ConsultarRemotaAsync(int id, string nome, string? apelido, string urlApi)
    {
        try
        {
            var json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            using var http = new HttpClient
            {
                BaseAddress = new Uri(urlApi.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromSeconds(8)
            };

            // Ping + versão
            var ping = await http.GetAsync("");
            if (!ping.IsSuccessStatusCode)
                return new FilialStatus(id, nome, apelido, urlApi, false, "offline", 0, 0, false, 0, 0, DateTime.Now);

            var versaoJson = await ping.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(versaoJson);
            var versao = doc.RootElement.TryGetProperty("versao", out var v) ? v.GetString() ?? "?" : "?";

            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);

            // Vendas hoje
            var vendasResp = await http.GetFromJsonAsync<List<VendaDiaDto>>(
                $"api/relatorios/vendas-por-dia?de={hoje:yyyy-MM-dd}&ate={amanha:yyyy-MM-dd}", json) ?? [];
            var totalHoje = vendasResp.Sum(x => x.Total);
            var pedidosHoje = vendasResp.Sum(x => x.Quantidade);

            // Caixa
            var caixaResp = await http.GetFromJsonAsync<CaixaStatusDto>("api/caixa/aberto", json);
            var caixaAberto = caixaResp?.Aberto ?? false;

            // Financeiro resumo
            var resumoResp = await http.GetFromJsonAsync<ResumoFinanceiroDto>(
                $"api/financeiro/resumo?de={hoje.AddDays(-30):yyyy-MM-dd}&ate={hoje.AddDays(30):yyyy-MM-dd}", json);

            // Contas atrasadas (status=2)
            var atrasadasResp = await http.GetFromJsonAsync<List<ContaMinDto>>(
                "api/financeiro/contas?status=2", json) ?? [];

            return new FilialStatus(id, nome, apelido, urlApi, true, versao,
                totalHoje, pedidosHoje, caixaAberto,
                resumoResp?.SaldoPrevisto ?? 0,
                atrasadasResp.Count,
                DateTime.Now);
        }
        catch
        {
            return new FilialStatus(id, nome, apelido, urlApi, false, "erro", 0, 0, false, 0, 0, DateTime.Now);
        }
    }

    // DTOs anônimos para deserialização dos endpoints remotos
    private record VendaDiaDto(DateTime Dia, int Quantidade, decimal Total);
    private record CaixaStatusDto(bool Aberto);
    private record ResumoFinanceiroDto(decimal TotalReceber, decimal TotalPagar, decimal SaldoPrevisto);
    private record ContaMinDto(int Id);
}
