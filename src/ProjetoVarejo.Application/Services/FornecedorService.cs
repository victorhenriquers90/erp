using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Microsoft.EntityFrameworkCore;

namespace ProjetoVarejo.Application.Services;

public class FornecedorService
{
    private readonly IUnitOfWork _unitOfWork;
    public FornecedorService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<List<Fornecedor>> ListarAsync(string? filtro = null)
    {
        System.Diagnostics.Trace.WriteLine("[FornecedorService.ListarAsync] Iniciando...");
        try
        {
            System.Diagnostics.Trace.WriteLine("[FornecedorService.ListarAsync] Obtendo query...");
            var q = _unitOfWork.Fornecedores.Query().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro))
            {
                System.Diagnostics.Trace.WriteLine($"[FornecedorService.ListarAsync] Aplicando filtro: {filtro}");
                q = q.Where(f => f.RazaoSocial.Contains(filtro) ||
                                (f.NomeFantasia != null && f.NomeFantasia.Contains(filtro)) ||
                                 f.Cnpj.Contains(filtro));
            }

            System.Diagnostics.Trace.WriteLine("[FornecedorService.ListarAsync] Executando ToListAsync()...");
            var resultado = await q.OrderBy(f => f.RazaoSocial).Take(500).ToListAsync();
            System.Diagnostics.Trace.WriteLine($"[FornecedorService.ListarAsync] ✅ Retornou {resultado.Count} fornecedores");
            return resultado;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[FornecedorService.ListarAsync] ❌ ERRO: {ex}");
            throw;
        }
    }

    public Task<Fornecedor?> BuscarPorIdAsync(int id) =>
        _unitOfWork.Fornecedores.Query().FirstOrDefaultAsync(f => f.Id == id);

    public async Task<Result<Fornecedor>> SalvarAsync(Fornecedor f)
    {
        if (string.IsNullOrWhiteSpace(f.RazaoSocial))
            return Result.Falha<Fornecedor>("Razão Social é obrigatória.");
        if (string.IsNullOrWhiteSpace(f.Cnpj))
            return Result.Falha<Fornecedor>("CNPJ é obrigatório.");

        var duplicado = await _unitOfWork.Fornecedores.Query().AnyAsync(x => x.Cnpj == f.Cnpj && x.Id != f.Id);
        if (duplicado) return Result.Falha<Fornecedor>("CNPJ já cadastrado.");

        if (f.Id == 0) await _unitOfWork.Fornecedores.InsertAsync(f);
        else { f.AtualizadoEm = DateTime.Now; await _unitOfWork.Fornecedores.UpdateAsync(f); }
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok(f);
    }

    public async Task<Result> ExcluirAsync(int id)
    {
        var f = await _unitOfWork.Fornecedores.GetByIdAsync(id);
        if (f == null) return Result.Falha("Fornecedor não encontrado.");
        f.Ativo = false;
        f.AtualizadoEm = DateTime.Now;
        await _unitOfWork.Fornecedores.UpdateAsync(f);
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }
}
