# 🔴 PHASE 1: Foundation (P0 - Críticos)

**Status:** 🚀 EM ANDAMENTO  
**Data Início:** 2026-05-26  
**Estimativa:** 16-20 horas  
**Branch:** `modernization/foundation`

---

## 📋 Tarefas

### 1.1 Race Condition em EstoqueService ✅ EM PROGRESSO
**Responsável:** IA2 (Claude - IA1 temporariamente)  
**Estimativa:** 4-5 horas  
**Risco:** ALTO - Afeta dados financeiros

#### Subtasks:
- [ ] Adicionar RowVersion a ConfiguracaoNegocio (Domain/Entities)
- [ ] Implementar OptimisticConcurrency em AppDbContext
- [ ] Adicionar DbUpdateConcurrencyException handling
- [ ] Testes de concorrência (3 casos)
- [ ] Verificação de migration necessária

**Arquivos Afetados:**
- `Domain/Entities/ConfiguracaoNegocio.cs` - Adicionar `[Timestamp] byte[] RowVersion`
- `Infrastructure/Data/AppDbContext.cs` - Configurar concorrência
- `Application/Services/EstoqueService.cs` - Handle exceptions
- Migrations - Nova migration para RowVersion

---

### 1.2 Segurança - CORS & Session ⏳ AGUARDANDO
**Responsável:** IA2  
**Estimativa:** 3-4 horas

#### CORS (Critical)
- [ ] Configurar CORS específico (não AllowAnyOrigin)
- [ ] Whitelist domínios permitidos em appsettings.json
- [ ] AllowCredentials = true
- [ ] Remover AllowAnyHeader (específico)

**Arquivo:** `Api/Program.cs` (linhas ~30-40)

```csharp
// ❌ ANTES
services.AddCors(o => o.AddDefaultPolicy(b => 
    b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
));

// ✅ DEPOIS
var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "https://localhost:3000" };

services.AddCors(o => o.AddDefaultPolicy(b =>
    b.WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowCredentials()
        .WithExposedHeaders("X-Total-Count")
));
```

#### Session Timeout (Critical)
- [ ] SessaoApp.UltimaAtividade rastreado
- [ ] Middleware valida timeout (15-30 min)
- [ ] Logout automático se expirado
- [ ] Adicionar SessionTimeoutMiddleware

**Arquivos:** 
- `Desktop/Program.cs` - SessaoApp initialization
- `Desktop/Middleware/` - SessionTimeoutMiddleware (novo)

---

### 1.3 Exception Handling Global ⏳ AGUARDANDO
**Responsável:** IA2  
**Estimativa:** 2-3 horas

#### Criar GlobalExceptionHandler
- [ ] Middleware que captura todas exceções
- [ ] Mensagens genéricas para usuário
- [ ] Logging detalhado no servidor
- [ ] Não expor stack traces
- [ ] HTTP status codes apropriados

**Arquivos:**
- `Api/Middleware/GlobalExceptionHandlerMiddleware.cs` (novo)
- `Api/Program.cs` - Registrar middleware
- `Shared/Responses/ErrorResponse.cs` (novo)

```csharp
public class ErrorResponse
{
    public string Message { get; set; }
    public string TraceId { get; set; }
    // Sem stack trace!
}
```

---

## 📊 Compile Status

```
Status: 🟡 AGUARDANDO TESTES
Errors: 0
Warnings: 3 (conhecidos)
Last Check: 2026-05-26
```

---

## 🔗 Bloqueadores

Nenhum no momento. ✅

---

## 📝 Notas de Implementação

### Race Condition
O EstoqueService manipula estoque em múltiplas operações:
- GetMovimentacao()
- AtualizarEstoque()
- Venda.Items carregam dados

Concorrência simulada:
1. User A: Vende 10 unidades (Select 100)
2. User B: Vende 10 unidades (Select 100 - mesmo valor pré-atualização)
3. Ambos: Update 90
4. **Resultado errado:** 90 em vez de 80

**Solução:** RowVersion + Retry ou merge strategy

### CORS
Atualmente:
```
AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()
```
Risco: Qualquer site malicioso pode acessar API

Solução:
```
WithOrigins("https://app.varejo.local")
AllowCredentials()
```

### Session
SessaoApp é singleton sem timeout. Precisa:
1. Track UltimaAtividade
2. Middleware valida a cada request
3. Limpa se expirado (15-30 min)

---

## 🎯 Próximas Fases

Após FASE 1 ✅:
- FASE 2: Dependency Inversion (Repository Pattern)
- FASE 3: Eliminar Duplicação (GenericRepository, FluentValidation)
- FASE 4: Frontend Modernization (BaseCrudForm)
- FASE 5: Performance & Polish

---

**Início:** 2026-05-26  
**Próxima Atualização:** Após conclusão de 1.1
