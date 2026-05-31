# Refactoring Guide for Remaining 4 Services

## Quick Refactoring Steps for Each Service

### 1. ProducaoGuardService
```csharp
// Change from:
private readonly AppDbContext _db;
public ProducaoGuardService(AppDbContext db, IConfiguration config, SessaoApp sessao)

// To:
private readonly IUnitOfWork _unitOfWork;
private readonly IConfiguration _config;
private readonly SessaoApp _sessao;

public ProducaoGuardService(IUnitOfWork unitOfWork, IConfiguration config, SessaoApp sessao)
{
    _unitOfWork = unitOfWork;
    _config = config;
    _sessao = sessao;
}

// Replace all _db.Entity references with _unitOfWork.Entity.Query() or corresponding repository methods
```

### 2. NfeImporterService
```csharp
// Change from:
private readonly AppDbContext _db;

// To:
private readonly IUnitOfWork _unitOfWork;

// Update constructor and all _db references
```

### 3. NfceService
```csharp
// This service has infrastructure dependencies (NfceXmlGenerator, etc.)
// Keep those injected - only replace AppDbContext with IUnitOfWork

// Change from:
private readonly AppDbContext _db;
private readonly NfceXmlGenerator _generator;
// ... other infrastructure services

// To:
private readonly IUnitOfWork _unitOfWork;
private readonly NfceXmlGenerator _generator;
// ... other infrastructure services (keep as is)

public NfceService(
    IUnitOfWork unitOfWork,  // NEW
    NfceXmlGenerator generator,
    NfceAssinador assinador,
    // ... rest of infrastructure services
)
{
    _unitOfWork = unitOfWork;
    _generator = generator;
    // ... assign others
}
```

### 4. ChecklistProducaoService
```csharp
// Similar pattern - replace AppDbContext with IUnitOfWork
// But keep IConfiguration as is

// For database migrations check, use:
// var pendentes = (await _unitOfWork.SaveChangesAsync()) 
// Or create a custom interface for database checks

// Entity references like:
// _db.Produtos.AsNoTracking().CountAsync()
// Become:
// _unitOfWork.Produtos.Query().CountAsync()

// For _db.Database.GetPendingMigrationsAsync(), this needs special handling:
// Option 1: Create an IDatabaseService interface
// Option 2: Keep a cached reference to DbContext
// Option 3: Remove this check (migrations should be pre-applied in production)
```

## Common Replacement Patterns

### Pattern 1: Simple Count
```csharp
// Before:
var count = await _db.Produtos.AsNoTracking().CountAsync(p => p.Ativo);

// After:
var count = await _unitOfWork.Produtos.Query().CountAsync(p => p.Ativo);
```

### Pattern 2: Find and Update
```csharp
// Before:
var usuario = await _db.Usuarios.FindAsync(id);
usuario.SenhaHash = novaHash;
await _db.SaveChangesAsync();

// After:
var usuario = await _unitOfWork.Usuarios.GetByIdAsync(id);
usuario.SenhaHash = novaHash;
await _unitOfWork.Usuarios.UpdateAsync(usuario);
await _unitOfWork.SaveChangesAsync();
```

### Pattern 3: Insert with Relationship
```csharp
// Before:
_db.MovimentosCaixa.Add(new MovimentoCaixa { ... });
await _db.SaveChangesAsync();

// After:
await _unitOfWork.MovimentosCaixa.InsertAsync(new MovimentoCaixa { ... });
await _unitOfWork.SaveChangesAsync();
```

### Pattern 4: Complex Query
```csharp
// Before:
var resultados = await _db.Vendas
    .Include(v => v.Itens)
    .Where(v => v.Status == StatusVenda.Finalizada)
    .ToListAsync();

// After:
var resultados = await _unitOfWork.Vendas.Query()
    .Include(v => v.Itens)
    .Where(v => v.Status == StatusVenda.Finalizada)
    .ToListAsync();
```

## Dependency Injection Updates

After refactoring services, ensure Desktop and API Program.cs have:

```csharp
// In Program.cs or ConfigureServices:

// Desktop (Program.cs):
sc.AddScoped<AutenticacaoService>();
sc.AddScoped<ProdutoService>();
sc.AddScoped<ClienteService>();
// ... all services now accept IUnitOfWork

// API (Program.cs):
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddScoped<ProdutoService>();
builder.Services.AddScoped<ClienteService>();
// ... all services now accept IUnitOfWork

// Both already have:
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

## Testing After Refactoring

1. **Compilation Check:**
   ```bash
   dotnet build
   ```

2. **Unit Tests (if exist):**
   ```bash
   dotnet test
   ```

3. **Manual Testing Checklist:**
   - [ ] Login works (AutenticacaoService)
   - [ ] Create/Edit products (ProdutoService)
   - [ ] List clients (ClienteService)
   - [ ] Stock movements (EstoqueService)
   - [ ] NFC-e generation (NfceService)
   - [ ] Report generation (RelatorioService)
   - [ ] Cash register operations (CaixaService)

## Special Considerations

### ContasFinanceiras Issue
The split between ContasPagar and ContasReceber means:
- FinanceiroService needs refactoring to use both repositories
- RelatorioService.FluxoCaixaAsync() needs similar split handling
- Consider creating a unified query method in FinanceiroService

### Database Migrations
ChecklistProducaoService checks `_db.Database.GetPendingMigrationsAsync()`. Options:
1. Create `IDatabaseService` interface with migration check
2. Keep this as Infrastructure concern (don't check from Application)
3. Pre-apply all migrations before running application

### Infrastructure Services
NfceService, NfeImporterService have infrastructure-specific services (generators, signers, etc.)
- These should remain injected as dependencies
- Only replace data access (AppDbContext) with IUnitOfWork
- Architecture is already correct for these classes

## Estimated Time
- ProducaoGuardService: 30-45 minutes
- NfeImporterService: 20-30 minutes  
- NfceService: 30-45 minutes
- ChecklistProducaoService: 45-60 minutes
- **Total: 2-3 hours** for complete refactoring

---

**Next Phase:** PHASE 2.3 - Create Service Interfaces (IVendaService, etc.)
