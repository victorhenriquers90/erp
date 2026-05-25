# 👋 Bem-vindo ao Projeto Varejo - Codex!

**Para:** Codex (IA2 - Backend)  
**De:** Claude (IA1 - Frontend) + Usuário  
**Data:** 2026-05-25  
**Projeto:** Projeto Varejo v1.0 - Sistema Modular

---

## 🎯 **Sua Missão**

Você, **Codex**, vai ser responsável por:
- ✅ **Backend** - Services, lógica de negócio
- ✅ **Database** - Entidades, migrations, contexto EF Core
- ✅ **APIs** - Integrações e endpoints
- ✅ **Testes** - Testes unitários e validações

Enquanto **Claude** (IA1) cuida de:
- UI/Forms (WinForms)
- Temas e customizações visuais
- Componentes gráficos

---

## 📂 **Localização do Projeto**

```
C:\Users\victo\Documents\projeto
```

**Git:** Já inicializado e com primeiro commit  
**Compilação:** 0 erros ✅  
**Status:** Pronto para colaboração

---

## 📖 **O Que Você Precisa Ler (Ordem)**

### 1. ROADMAP.md (15 min)
```
C:\Users\victo\Documents\projeto\ROADMAP.md
```
**Leia para entender:** Visão geral do projeto, fases, timeline

### 2. SISTEMA_MODULAR_COMPLETO.md (45 min)
```
C:\Users\victo\Documents\projeto\SISTEMA_MODULAR_COMPLETO.md
```
**Leia para entender:** Tudo que foi implementado, arquitetura, módulos

### 3. AI_COORDINATION.md (10 min)
```
C:\Users\victo\Documents\projeto\.claude\AI_COORDINATION.md
```
**Leia para entender:** Como você e Claude trabalham juntos

### 4. TASKS_IA2.md (30 min)
```
C:\Users\victo\Documents\projeto\.claude\TASKS_IA2.md
```
**Leia para saber:** Suas tarefas específicas, prioridades, convenções

---

## 🚀 **Setup em 7 Passos**

### Passo 1: Acesse o Repositório
```bash
cd C:\Users\victo\Documents\projeto
git status
git log --oneline
```

### Passo 2: Verifique Git
```bash
# Ver histórico
git log --oneline

# Ver branches
git branch -a

# Seu primeiro commit como Codex:
git config user.name "Codex"
git config user.email "codex@example.com"
```

### Passo 3: Execute Migrations
```bash
cd C:\Users\victo\Documents\projeto\src\ProjetoVarejo.Infrastructure
dotnet ef database update
```

### Passo 4: Compile
```bash
cd C:\Users\victo\Documents\projeto
dotnet build -c Release
```

**Esperado:** 0 erros, 0 warnings

### Passo 5: Teste a Aplicação
```bash
cd C:\Users\victo\Documents\projeto\src\ProjetoVarejo.Desktop
dotnet run
```

- Login: `admin` / `admin`
- Explore o sistema
- Escolha tipo de negócio: **Loja Exemplo**

### Passo 6: Abra em IDE
```bash
# VS Code
code C:\Users\victo\Documents\projeto

# Visual Studio
start C:\Users\victo\Documents\projeto\src\ProjetoVarejo.sln
```

### Passo 7: Atualize AI_COORDINATION.md
Edite `.claude/AI_COORDINATION.md` e mude:

```markdown
| **IA2** | Codex | Setup inicial | ✅ Em progresso | 20% |
```

---

## 🏗️ **Entenda o Sistema em 2 Minutos**

### O Problema Original
A aplicação tinha **16 módulos carregados para todos**, causando confusão e uso desnecessário.

### A Solução
Transformar em **sistema modular** que:
1. **Detecta tipo de negócio** (8 tipos)
2. **Carrega apenas módulos relevantes** (16 módulos configuráveis)
3. **Customiza interface automaticamente** (cores, temas, ícones)
4. **Bloqueia acesso a módulos indisponíveis** (validação em tempo real)

### 8 Tipos de Negócio
```
🥐 Padaria      - Foco em produção e ingredientes
🥩 Açougue      - Foco em pesagem e cortes
🛍️ Loja         - Varejo geral (padrão)
🏭 Indústria    - Foco em BOM e produção
🧺 Bazar        - Pequeno varejo simplificado
🛒 Supermercado - Múltiplas seções
💊 Farmácia     - Receitas e medicamentos
🍽️ Restaurante  - Comandas e mesas
```

### 16 Módulos [Flags]
```
Obrigatórios (sempre ativados):
- PDV (Ponto de Venda)
- Estoque
- Cadastros
- Financeiro
- Relatórios
- Auditoria
- Backup

Opcionais (ativa conforme tipo):
- Fiscal (NFC-e)
- Produção
- Pesagem
- Pré-venda
- Comissões
- PIX
- TEF
- Receitas
- Comandas
```

---

## 🎯 **Seu Trabalho (Codex)**

### Arquivos-Chave que Você Vai Trabalhar

**1. Domain/Enums/ModuloSistema.cs**
```csharp
[Flags]
public enum ModuloSistema
{
    PDV = 1,
    Estoque = 2,
    Cadastros = 4,
    // ... 16 módulos com potências de 2
}
```
🔍 **Você vai:** Entender e usar este enum

**2. Domain/Enums/TipoNegocio.cs**
```csharp
public enum TipoNegocio
{
    Padaria = 1,
    Acougue = 2,
    Loja = 3,
    // ... 8 tipos
}
```
🔍 **Você vai:** Entender e usar este enum

**3. Application/Configuracao/ModulosPorTipo.cs** ⭐ CRÍTICO
```csharp
// Define qual módulo para cada tipo
public static class ModulosPorTipo
{
    public static ModuloSistema ObterModulosRecomendados(TipoNegocio tipo)
    {
        return tipo switch
        {
            TipoNegocio.Padaria => ModuloSistema.PDV | ModuloSistema.Producao | ...,
            TipoNegocio.Loja => ModuloSistema.PDV | ModuloSistema.Comissoes | ...,
            // ...
        };
    }
}
```
🔍 **Você vai:** Entender esta lógica central

**4. Application/Services/ConfiguracaoNegocioService.cs** ⭐ SERVIÇO PRINCIPAL
```csharp
public class ConfiguracaoNegocioService
{
    public async Task<ConfiguracaoNegocio> ObterConfiguracao()
    public async Task SalvarConfiguracao(ConfiguracaoNegocio config)
    public void LimparCache()
}
```
🔍 **Você vai:** Estender com novos métodos nas Fases 8-10

**5. Domain/Configuracao/ConfiguracaoNegocio.cs**
```csharp
public class ConfiguracaoNegocio
{
    public int Id { get; set; }
    public TipoNegocio TipoNegocio { get; set; }
    public ModuloSistema ModulosAtivos { get; set; }
    public bool ConfiguracaoInicial { get; set; }
    // ...
}
```
🔍 **Você vai:** Adicionar campos para Fase 8-10

---

## ✅ **Suas Tarefas Imediatas (Esta Semana)**

### Tarefa 1: Code Review (2 horas)
- [ ] Ler código em `Application/Services/`
- [ ] Revisar `ConfiguracaoNegocioService`
- [ ] Revisar `ModulosPorTipo`
- [ ] Entender arquitetura

### Tarefa 2: Validar Banco de Dados (1 hora)
- [ ] Executar migrations: `dotnet ef database update`
- [ ] Abrir SQL Server Management Studio
- [ ] Verificar tabelas criadas
- [ ] Testar conexão

### Tarefa 3: Criar Testes Unitários (3-4 horas)
- [ ] Testes para `ConfiguracaoNegocioService`
- [ ] Testes para `ModulosPorTipo`
- [ ] Testes de validação de módulos
- [ ] Setup projeto de testes

### Tarefa 4: Comunicar Status (30 min)
- [ ] Atualizar `AI_COORDINATION.md`
- [ ] Fazer primeiro commit com seu nome
- [ ] Deixar nota sobre o que aprendeu

---

## 📋 **Suas Tarefas de Médio/Longo Prazo**

### Fase 8: Relatório de Configuração (18-26 horas)
**Backend do Codex:**
- [ ] Nova entidade `ConfiguracaoAudit` (histórico)
- [ ] Nova migration para tabela de auditoria
- [ ] Service `RelatorioConfiguracaoService`
- [ ] Métodos para exportar PDF/Excel
- [ ] Testes unitários

### Fase 9: Multi-tenant (16-24 horas)
**Backend do Codex:**
- [ ] Modificar `ConfiguracaoNegocio` para suportar múltiplas instalações
- [ ] Nova migration
- [ ] Service para trocar instalação/tipo
- [ ] Backup isolado por instalação
- [ ] Testes

### Fase 10: Customização Avançada (22-32 horas)
**Backend do Codex:**
- [ ] Tabela de customizações (cores, logo, etc)
- [ ] Service de customização
- [ ] Persistência de temas customizados
- [ ] Testes

---

## 🔑 **Convenções Git (Codex)**

### Seus commits devem ser assim:
```bash
git commit -m "feat(service): descrição da mudança - Codex"
git commit -m "feat(domain): nova entidade ConfiguracaoAudit - Codex"
git commit -m "test(unit): testes para ConfiguracaoService - Codex"
git commit -m "refactor(data): reorganizar migrations - Codex"
git commit -m "fix(service): bug em ValidarModulo - Codex"
```

### Exemplos BOM:
```bash
✅ git commit -m "feat(service): adicionar ConfiguracaoAuditService - Codex"
✅ git commit -m "test(unit): testes para ObterModulosRecomendados - Codex"
✅ git commit -m "refactor(domain): reorganizar ConfiguracaoNegocio - Codex"
```

### Exemplos RUIM:
```bash
❌ git commit -m "mudanças" - Codex
❌ git commit -m "feat(ui): novo botão" - Codex (isso é Claude!)
❌ "fix: corrigir" (sem prefixo Codex)
```

---

## 📞 **Comunicação com Claude**

### Arquivo Central:
```
.claude/AI_COORDINATION.md
```

### Se precisar de algo de Claude:
Adicione no arquivo:
```markdown
⚠️ BLOQUEADO: Preciso que Claude crie novo campo em FrmConfiguracao
   Razão: Nova coluna para histórico de mudanças
   Solução proposta: TextBox ou grid mostrando histórico
   Aguardando: Claude implementar até [data]
```

Claude verá na próxima vez que trabalhar neste arquivo.

---

## ⚠️ **O Que Você NÃO Deve Fazer**

❌ **NÃO MEXER:**
- `Desktop/Forms/` - Isso é Claude (UI)
- `Desktop/Theme/` - Temas (Claude)
- `Application/Configuracao/TemasNegocio.cs` - Temas (Claude)

✅ **VOCÊ PODE MEXER:**
- `Domain/Entities/` - Criar/modificar entidades
- `Application/Services/` - Criar/modificar services
- `Infrastructure/Data/` - Migrations, contexto EF
- `Infrastructure/Migrations/` - Novas migrations
- Tests - Testes unitários

---

## 🛠️ **Stack Técnico**

```
Linguagem:      C# .NET 8.0
Framework:      ASP.NET Core
ORM:            Entity Framework Core 8.0
Banco:          SQL Server (LocalDB ou Express)
Testing:        xUnit (ou NUnit)
IDE:            Visual Studio 2022 ou VS Code
Git:            Versionamento compartilhado
```

---

## 📊 **Estrutura do Repositório**

```
C:\Users\victo\Documents\projeto\
│
├── .git/                  ← Git repo
│
├── .claude/
│   ├── AI_COORDINATION.md ← 🤖 Comunicação entre IAs
│   ├── TASKS_IA1.md       ← Tarefas de Claude
│   ├── TASKS_IA2.md       ← Suas tarefas
│   └── CODEX_WELCOME.md   ← Este arquivo
│
├── src/
│   ├── ProjetoVarejo.Domain/           ← Você trabalha aqui
│   │   ├── Entities/
│   │   │   └── ConfiguracaoNegocio.cs
│   │   └── Enums/
│   │       ├── TipoNegocio.cs
│   │       └── ModuloSistema.cs
│   │
│   ├── ProjetoVarejo.Application/      ← Você trabalha aqui
│   │   ├── Services/
│   │   │   └── ConfiguracaoNegocioService.cs
│   │   └── Configuracao/
│   │       └── ModulosPorTipo.cs
│   │
│   ├── ProjetoVarejo.Infrastructure/   ← Você trabalha aqui
│   │   ├── Data/
│   │   │   └── AppDbContext.cs
│   │   └── Migrations/
│   │
│   └── ProjetoVarejo.Desktop/          ← Claude trabalha aqui
│       └── Forms/
│
├── ROADMAP.md             ← Leia isto
└── SISTEMA_MODULAR_COMPLETO.md ← Leia isto
```

---

## ✅ **Seu Checklist de Início**

- [ ] Li ROADMAP.md
- [ ] Li SISTEMA_MODULAR_COMPLETO.md
- [ ] Li AI_COORDINATION.md
- [ ] Li TASKS_IA2.md
- [ ] Li CODEX_WELCOME.md (este arquivo)
- [ ] Acessei repositório: `git status` OK
- [ ] Executei migrations: `dotnet ef database update` OK
- [ ] Compilei projeto: `dotnet build` - 0 erros
- [ ] Testei app: `dotnet run` - funcionando
- [ ] Abri em IDE (VS Code ou Visual Studio)
- [ ] Entendi arquitetura do sistema
- [ ] Atualizei AI_COORDINATION.md com meu status
- [ ] Fiz primeiro commit com meu nome

---

## 🚀 **Comece Assim**

### 1. Leia a Documentação (1.5 horas)
```
ROADMAP.md → SISTEMA_MODULAR_COMPLETO.md → AI_COORDINATION.md → TASKS_IA2.md
```

### 2. Faça o Setup (1.5 horas)
```bash
cd C:\Users\victo\Documents\projeto
git status
dotnet ef database update
dotnet build -c Release
dotnet run  # (teste login)
```

### 3. Abra em IDE (30 min)
```bash
code C:\Users\victo\Documents\projeto
# ou
start C:\Users\victo\Documents\projeto\src\ProjetoVarejo.sln
```

### 4. Comece Tarefas Imediatas (6-8 horas)
- Code review dos serviços
- Validar banco de dados
- Criar testes unitários

### 5. Comunique Progresso (30 min)
- Atualizar `AI_COORDINATION.md`
- Fazer commits regularmente
- Avisar Claude se precisar de algo

---

## 💬 **Perguntas Frequentes (Codex)**

**P: Preciso de acesso a banco de dados?**
R: SQL Server LocalDB ou Express. Usuário padrão da máquina.

**P: Posso modificar Domain?**
R: SIM! É seu domínio. Pode adicionar entidades, enums, etc.

**P: Posso modificar Desktop/Forms?**
R: NÃO! Isso é Claude (Frontend). Se precisar, peça em AI_COORDINATION.md

**P: Devo fazer commits frequentes?**
R: SIM! Pelo menos uma vez por dia. Ajuda Claude a ver progresso.

**P: Como me comunicar com Claude se tiver dúvida?**
R: Escreva em `.claude/AI_COORDINATION.md` com ⚠️ BLOQUEADO

**P: O banco está SQL Server local?**
R: Sim. Conexão em `appsettings.json`: "(local)\\MSSQLSERVER"

**P: Preciso de permissões especiais?**
R: Não. Você tem acesso total ao código e banco.

---

## 🎓 **Próximas Ações**

1. **Agora:** Ler os 4 documentos em ordem
2. **Próximo:** Fazer os 7 passos de setup
3. **Depois:** Completar checklist
4. **Começar:** Tarefas imediatas (code review + testes)

---

## 🤝 **Você + Claude = Projeto Varejo**

```
┌──────────────┐                    ┌──────────────┐
│   CODEX      │                    │   CLAUDE     │
│  (Backend)   │                    │  (Frontend)  │
├──────────────┤                    ├──────────────┤
│ • Services   │  ◄─────────────►   │ • Forms      │
│ • Database   │   AI_COORD.md      │ • Themes     │
│ • Entities   │                    │ • UI Components│
│ • Migrations │                    │ • Customization│
└──────────────┘                    └──────────────┘
       │                                    │
       └────────────────┬────────────────┘
                        │
                    GIT SYNC
                   (Commits)
```

---

## 📞 **Contato / Dúvidas**

Qualquer dúvida:
1. Consulte `TASKS_IA2.md` (suas tarefas)
2. Consulte `AI_COORDINATION.md` (comunicação)
3. Consulte `SISTEMA_MODULAR_COMPLETO.md` (detalhes técnicos)
4. Escreva bloqueador em `AI_COORDINATION.md` para Claude

---

## 🎉 **Bem-vindo, Codex!**

Você foi escolhido para ser o **IA2** do Projeto Varejo porque você é excelente em:
- ✅ Lógica de negócio e arquitetura
- ✅ Banco de dados e Entity Framework
- ✅ Services e padrões de design
- ✅ Testes e qualidade de código

**Vamos entregar um sistema incrível juntos!**

---

**Codex está pronto?** 🚀

Leia os 4 documentos, faça o setup de 7 passos, e comece com as tarefas imediatas!

**Boa sorte! 💪**
