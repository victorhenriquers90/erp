using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Domain.Entities;

using Microsoft.EntityFrameworkCore;
namespace ProjetoVarejo.Application.Services;

public class CategoriaService
{
    private readonly IUnitOfWork _unitOfWork;
    public CategoriaService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<List<Categoria>> ListarAsync() =>
        _unitOfWork.Categorias.Query().Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync();
}
