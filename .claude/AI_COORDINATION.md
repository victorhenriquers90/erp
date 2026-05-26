# 🤖 Coordenação entre IAs - Projeto Varejo

**Última atualização:** 2026-05-26 11:02

---

## 📊 Status Atual - MODERNIZAÇÃO v1.0

| IA | Responsável | Tarefa Atual | Status | Progresso |
|---|---|---|---|---|
| **IA1** | Claude | **FASE 1: Foundation (P0 Críticos)** | ✅ Em progresso | 60% |
| **IA2** | Codex | *Aguardando - FASE 1* | ⏳ Pausado | 35% |

### 🚀 Novo: Plano de Modernização
- **Arquivo:** `.claude/MODERNIZATION_PLAN.md` (Completo)
- **Arquivo:** `.claude/PHASE1_PROGRESS.md` (Rastreamento)
- **Branch:** `modernization/foundation`
- **Fases:** 5 principais | **Total:** 60-80 horas

---

## 🎯 Áreas Designadas

### IA1 (Claude) - Frontend & UI
- ✅ Interface gráfica WinForms
- ✅ Temas e customizações visuais
- ✅ Formulários (FrmLogin, FrmMain, etc)
- ✅ UX/Layout e responsividade
- ✅ Sistema Modular de Temas (TemasNegocio.cs)
- ✅ Gerenciador de Módulos (FrmGerenciadorModulos)

### IA2 (Outra IA) - Backend & Data
- 🔲 Lógica de negócio (Services)
- 🔲 Banco de dados (Entities, Migrations)
- 🔲 APIs e integrações externas
- 🔲 Testes unitários
- 🔲 Performance e otimizações
- 🔲 Segurança e validações

---

## 📝 Convenções Git

```bash
# Commits IA1 (Claude)
git commit -m "feat(ui): [descrição] - IA1 Claude"

# Commits IA2 (Outra IA)
git commit -m "feat(backend): [descrição] - IA2"

# Exemplos:
git commit -m "fix(login): corrigir campos cortados - IA1 Claude"
git commit -m "feat(service): adicionar novo validador - IA2"
```

---

## ⚠️ Regras de Colaboração

### ✅ PERMITIDO
- Trabalhar em arquivos diferentes simultaneamente
- Modificar arquivos da própria área de responsabilidade
- Fazer commits regularmente
- Atualizar este arquivo com status

### ❌ NÃO PERMITIDO
- Modificar o mesmo arquivo simultaneamente
- Deletar/renomear sem comunicar
- Fazer push sem testar compilação
- Mudar código fora da área designada sem avisar

---

## 📞 Comunicação entre IAs

### Como IA2 deve iniciar:

1. **Ler esta documentação:**
   - Este arquivo (AI_COORDINATION.md)
   - TASKS_IA2.md (tarefas designadas)
   - ROADMAP.md (contexto geral)
   - SISTEMA_MODULAR_COMPLETO.md (o que foi feito)

2. **Atualizar este arquivo:**
   ```markdown
   | **IA2** | Outra IA | [Sua tarefa atual] | ✅ Em progresso | X% |
   ```

3. **Fazer commit regularmente** com prefixo "IA2"

### Checklist antes de começar (IA2):

- [ ] Li AI_COORDINATION.md
- [ ] Li TASKS_IA2.md
- [ ] Entendi o projeto (SISTEMA_MODULAR_COMPLETO.md)
- [ ] Tenho Git configurado
- [ ] Posso fazer commits com meu nome
- [ ] Não vou modificar código de IA1 sem avisar

---

## 🔄 Fluxo de Sincronização

```
1. IA1 faz commits com seu trabalho
   └─ Atualiza AI_COORDINATION.md

2. IA2 puxa (git pull) as mudanças
   └─ Verifica o status da IA1

3. IA2 trabalha em sua área (backend/services)
   └─ Faz commits com prefixo "IA2"

4. IA1 puxa mudanças de IA2 (git pull)
   └─ Integra com seu trabalho de UI

5. Comunicam-se via este arquivo
   └─ Status, bloqueadores, próximas etapas
```

---

## 📋 Log de Atividades

### 2026-05-25

**15:20 - IA1 (Claude)**
- ✅ Corrigidos problemas de UI (FrmLogin)
  - Campos de login/senha cortados
  - Botão X desalinhado
- ✅ Compilado sem erros
- 📝 Próximo: Testes de UI

**15:27 - Setup de Coordenação**
- ✅ Criado AI_COORDINATION.md
- ✅ Criado TASKS_IA1.md (Claude)
- ✅ Criado TASKS_IA2.md (Codex)
- ✅ Criado CODEX_WELCOME.md (boas-vindas)
- ✅ Criado ROADMAP.md (plano geral)
- ✅ Git inicializado com primeiro commit
- ✅ Pronto para Codex começar!

---

## 🚀 Próximas Etapas Gerais

### Fase 8: Relatório de Configuração (IA2)
- [ ] Dashboard admin com estatísticas
- [ ] Matriz de módulos por tipo
- [ ] Histórico de mudanças
- [ ] Exportar para PDF/Excel

### Fase 9: Multi-tenant (IA2)
- [ ] Suporte a múltiplas instalações
- [ ] Switching entre tipos dinamicamente
- [ ] Backup isolado por tipo
- [ ] Relatório consolidado

### Fase 10: Customização Avançada (IA1 + IA2)
- [ ] Interface drag-drop para reordenar menus
- [ ] Upload de logos/branding
- [ ] Temas customizáveis via UI
- [ ] Atalhos configuráveis

---

## 📞 Contato / Dúvidas

Se houver conflito ou dúvida:
1. Escreva aqui neste arquivo
2. Marque com ⚠️
3. Aguarde resposta antes de fazer merge

Exemplo:
```
⚠️ BLOQUEADO: IA2 precisa de mudança em ConfiguracaoService
   Razão: Nova coluna na tabela ConfiguracaoNegocio
   Solução proposta: [descrição]
   Aguardando: IA1 implementar
```

---

**Status Geral do Projeto: ✅ 70% Concluído (Modular) → 🚀 Modernização Iniciada**
- Fases 1-7 (Sistema Modular): ✅ 100%
- Fases 8-10 (Enhancements): 🔲 0% (adiado)
- **NOVA: FASE Modernização (P0-P3):** 🚀 Iniciada

---

## 🔴 FASE 1: Foundation (P0 - Críticos) - 2026-05-26

### Mudanças Realizadas (60% - IA1/Claude)

#### ✅ 1.1: Race Condition em EstoqueService - COMPLETO
- **Arquivo:** `Domain/Entities/Produto.cs`
  - ✅ Adicionado `[Timestamp] byte[]? RowVersion`
  - Controle otimista de concorrência para multi-usuário
  
- **Arquivo:** `Application/Services/EstoqueService.cs`
  - ✅ Try-catch para `DbUpdateConcurrencyException`
  - ✅ Reload automático de dados atualizados
  - ✅ Mensagem clara para usuário
  - Removido TODO comment (resolved)

- **Migration:** `AddRowVersionToProduto` (20260526110228)
  - ✅ Criada com sucesso
  - ✅ Coluna `rowversion` no SQL Server
  - ✅ Tipo: `rowversion` (automático)

#### ✅ 1.2: Segurança - CORS Restrito - COMPLETO
- **Arquivo:** `Api/Program.cs`
  - ❌ **Antes:** `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`
  - ✅ **Depois:** CORS policy com whitelist
  - ✅ Lê origens permitidas de `appsettings.json`
  - ✅ Default dev: `https://localhost:3000`, `https://localhost:5173`
  - ✅ Production: Configurável
  - ✅ AllowCredentials = true
  - ✅ WithExposedHeaders para paginação

#### ✅ 1.3: Session Timeout - IMPLEMENTADO - COMPLETO
- **Arquivo:** `Application/Sessao/SessaoApp.cs`
  - ✅ Adicionado `UltimaAtividade` (DateTime?)
  - ✅ Método `Expirou()` - verifica timeout (30 min)
  - ✅ Método `AtualizarAtividade()` - refresh automático
  - ✅ Property `Autenticado` agora verifica expiração
  - ✅ Comportamento: Sessão expira após 30 min inatividade

### 📋 Próximo Passo (40% - IA2)
- [ ] Aplicar migration ao banco: `dotnet ef database update`
- [ ] Testar race condition (simulação multi-caixa)
- [ ] Testar session timeout (inatividade)
- [ ] Testes de CORS (bloqueio de origem não permitida)

### 📦 Compilação
- ✅ Build Release: 0 erros
- ⚠️ 3 warnings (conhecidos, não-críticos em FrmConfiguracao)

### 🔗 Relação com FASE 2
- Após FASE 1: Iniciar FASE 2 (Repository Pattern)
- Branch: `modernization/foundation`
- Próximo merge: `modernization/phase2`

---

### Índice de Documentação Modernização
1. **MODERNIZATION_PLAN.md** - Plano completo (5 fases)
2. **PHASE1_PROGRESS.md** - Rastreamento FASE 1
3. **Este arquivo** - Coordenação entre IAs
