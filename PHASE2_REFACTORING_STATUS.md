# FASE 2 Refactoring Status - IUnitOfWork Implementation

## Summary

Successfully refactored **13 out of 19 services** in Projeto Varejo to use `IUnitOfWork` pattern instead of direct `AppDbContext` injection. This removes the circular dependency between Application and Infrastructure layers.

## ✅ Completed Services (13/19)

1. **ProdutoService** - ✓ Refactored
2. **ClienteService** - ✓ Refactored  
3. **FornecedorService** - ✓ Refactored
4. **CategoriaService** - ✓ Refactored
5. **UsuarioService** - ✓ Refactored
6. **PermissaoService** - ✓ Refactored (added `UsuarioPermissao` repository)
7. **AuditLogService** - ✓ Refactored
8. **EstoqueService** - ✓ Refactored (fixed concurrency exception handling)
9. **AutenticacaoService** - ✓ Refactored
10. **VendaService** - ✓ Refactored (added `PagamentoVenda` repository)
11. **CaixaService** - ✓ Refactored (added `MovimentoCaixa` repository)
12. **RelatorioService** - ✓ Refactored (with TODO for ContasFinanceiras)
13. **FinanceiroService** - ⚠️ Partially Refactored (needs split Pagar/Receber implementation)

## ⏳ Remaining Services (4/19) - Need Refactoring

These 4 services still use direct `AppDbContext` injection and need refactoring:

### 1. **ChecklistProducaoService** (Most Complex)
- **Issues:**
  - Uses `AppDbContext` for database validations
  - References `EmpresaConfigs` which maps to `ConfiguracaoEmpresa`
  - Uses `_db.Database.GetPendingMigrationsAsync()` (database-specific method)
  - Complex evaluation logic with multiple queries
  - Depends on `IConfiguration` for checking connection strings
  
- **Refactoring Strategy:**
  - Create repository methods for needed queries
  - Handle database migrations check differently (may need custom interface)
  - Keep connection string validation in service or move to separate utility

### 2. **NfceService** (Complex, NFC-e Specific)
- **Issues:**
  - Heavy infrastructure dependencies (NfceXmlGenerator, NfceAssinador, SefazSpClient, etc.)
  - These are infrastructure-level services, not domain services
  - Uses AppDbContext for data access
  
- **Refactoring Strategy:**
  - Convert to use `IUnitOfWork` for entity access
  - Keep infrastructure services as injected dependencies (they're correctly separated)
  - Update constructor to accept `IUnitOfWork` instead of `AppDbContext`

### 3. **NfeImporterService**
- **Issues:**
  - Uses `AppDbContext` directly
  - Likely has complex business logic for XML import
  
- **Refactoring Strategy:**
  - Replace `AppDbContext` with `IUnitOfWork`
  - Update all data access to use repository pattern

### 4. **ProducaoGuardService**
- **Issues:**
  - Uses `AppDbContext`
  - Depends on `IConfiguration`
  
- **Refactoring Strategy:**
  - Replace `AppDbContext` with `IUnitOfWork`
  - Keep `IConfiguration` as dependency (external configuration is valid)

## 🔧 Repository Additions

Successfully added these new repositories to `IUnitOfWork`:

- `UsuarioPermissao` - For custom user permissions
- `PagamentoVenda` - For payment tracking
- `MovimentoCaixa` - For cash register movements

## 🐛 Known Issues

### 1. FinanceiroService - ContasFinanceiras
- **Problem:** Original code uses a unified `ContasFinanceiras` table/view
- **Current:** We have split `ContasPagar` and `ContasReceber` repositories
- **Solution:** Need to either:
  - Implement a unified method that merges results from both repositories
  - OR refactor to keep ContasFinanceiras if it exists as a database view
  - OR split the service into PagamentoService and ReceberService

### 2. RelatorioService - FluxoCaixaAsync
- Same ContasFinanceiras issue as above
- Temporarily returns empty list for financial flows

### 3. Circular Dependency - RESOLVED ✓
- ✅ Removed temporary ProjectReference from Application.csproj
- ✅ Infrastructure still references Application (for IRepository/IUnitOfWork)
- ✅ Application now only references Domain and Shared

## 📝 Build Status

**Current:** Compilation errors in remaining services due to missing `AppDbContext` references

**Next Step:** Refactor the 4 remaining services

## 📋 Refactoring Pattern

All refactored services follow this pattern:

### Before:
```csharp
public class ServiceName
{
    private readonly AppDbContext _db;
    public ServiceName(AppDbContext db) => _db = db;
    
    public async Task MethodAsync()
    {
        var item = await _db.Entidade.FindAsync(id);
        _db.Entidade.Add(item);
        await _db.SaveChangesAsync();
    }
}
```

### After:
```csharp
public class ServiceName
{
    private readonly IUnitOfWork _unitOfWork;
    public ServiceName(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;
    
    public async Task MethodAsync()
    {
        var item = await _unitOfWork.Entidade.GetByIdAsync(id);
        await _unitOfWork.Entidade.InsertAsync(item);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

## 🎯 Next Actions

1. **Refactor the 4 remaining services** following the pattern above
2. **Fix ContasFinanceiras issue** in FinanceiroService and RelatorioService  
3. **Run full compilation** to verify all changes
4. **Update Dependency Injection** in Program.cs files (Desktop and API)
5. **Test each service** with updated repository pattern
6. **Run test suite** to ensure functionality preserved

## 📊 Progress Metrics

- Services refactored: 13/19 (68%)
- Circular dependency: RESOLVED ✓
- New repositories added: 3 (UsuarioPermissao, PagamentoVenda, MovimentoCaixa)
- Estimated remaining time: 2-3 hours for 4 complex services

## 🔗 Related Files

- `/src/ProjetoVarejo.Application/Contracts/Repositories/IRepository.cs` - Generic interface
- `/src/ProjetoVarejo.Application/Contracts/Repositories/IUnitOfWork.cs` - Coordinator interface
- `/src/ProjetoVarejo.Infrastructure/Repositories/GenericRepository.cs` - Implementation
- `/src/ProjetoVarejo.Infrastructure/Repositories/UnitOfWork.cs` - Coordinator implementation
- All refactored services in `/src/ProjetoVarejo.Application/Services/`

---

**Last Updated:** 2026-05-26
**Branch:** FASE 2 - Dependency Inversion (In Progress)
