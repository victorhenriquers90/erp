using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;

namespace ProjetoVarejo.Application.Services;

public class FilialService
{
    private readonly IUnitOfWork _unitOfWork;

    public FilialService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<List<Filial>> ListarAsync(bool incluirInativas = true)
    {
        var q = _unitOfWork.Filiais.Query().AsQueryable();

        if (!incluirInativas)
            q = q.Where(f => f.Ativo);

        return q.OrderBy(f => f.Codigo).ToListAsync();
    }

    public Task<Filial?> BuscarPorIdAsync(int id) =>
        _unitOfWork.Filiais.Query().FirstOrDefaultAsync(f => f.Id == id);

    public async Task<Result<Filial>> SalvarAsync(Filial filial)
    {
        try
        {
            filial.Codigo = filial.Codigo.Trim().ToUpperInvariant();
            filial.Nome   = filial.Nome.Trim();

            if (string.IsNullOrWhiteSpace(filial.Codigo))
                return Result.Falha<Filial>("Codigo e obrigatorio.");

            if (string.IsNullOrWhiteSpace(filial.Nome))
                return Result.Falha<Filial>("Nome e obrigatorio.");

            var duplicado = await _unitOfWork.Filiais.Query()
                .AnyAsync(f => f.Codigo == filial.Codigo && f.Id != filial.Id);
            if (duplicado)
                return Result.Falha<Filial>("Ja existe filial com este codigo.");

            if (filial.Id == 0)
            {
                await _unitOfWork.Filiais.InsertAsync(filial);
                Log.Information("Filial criada: {Codigo} - {Nome}", filial.Codigo, filial.Nome);
            }
            else
            {
                var atual = await _unitOfWork.Filiais.Query().FirstOrDefaultAsync(f => f.Id == filial.Id);
                if (atual == null)
                    return Result.Falha<Filial>("Filial nao encontrada.");

                atual.Codigo   = filial.Codigo;
                atual.Nome     = filial.Nome;
                atual.Cnpj     = filial.Cnpj;
                atual.Endereco = filial.Endereco;
                atual.Telefone = filial.Telefone;
                atual.IsMatriz = filial.IsMatriz;
                atual.Ativo    = filial.Ativo;
                atual.AtualizadoEm = DateTime.Now;

                await _unitOfWork.Filiais.UpdateAsync(atual);
                filial = atual;
                Log.Information("Filial atualizada: {Codigo} - {Nome}", filial.Codigo, filial.Nome);
            }

            await _unitOfWork.SaveChangesAsync();
            return Result.Ok(filial);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FilialService.SalvarAsync: {Mensagem}", ex.Message);
            return Result.Falha<Filial>($"Erro ao salvar filial: {ex.Message}");
        }
    }

    public async Task<Result> AlternarAtivoAsync(int filialId)
    {
        try
        {
            var filial = await _unitOfWork.Filiais.Query().FirstOrDefaultAsync(f => f.Id == filialId);
            if (filial == null)
                return Result.Falha("Filial nao encontrada.");

            if (filial.IsMatriz && filial.Ativo)
                return Result.Falha("Nao e possivel inativar a filial Matriz.");

            filial.Ativo = !filial.Ativo;
            filial.AtualizadoEm = DateTime.Now;
            await _unitOfWork.Filiais.UpdateAsync(filial);
            await _unitOfWork.SaveChangesAsync();

            Log.Information("Filial {Codigo} {Status}", filial.Codigo, filial.Ativo ? "ativada" : "inativada");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro em FilialService.AlternarAtivoAsync: {Mensagem}", ex.Message);
            return Result.Falha($"Erro ao alternar status: {ex.Message}");
        }
    }
}
