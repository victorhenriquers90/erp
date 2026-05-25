# 📋 Tarefas da IA2 - Projeto Varejo

**Responsável:** Outra IA  
**Data de Início:** 2026-05-25  
**Status:** ⏳ Aguardando

---

## 🎯 Visão Geral

A IA2 é responsável pela **lógica de negócio, banco de dados e serviços** do Projeto Varejo.

A IA1 (Claude) está cuidando de toda a interface gráfica e temas.

---

## ✅ Tarefas Imediatas (Prioridade: ALTA)

### 1. Revisar Código Existente
- [ ] Ler `SISTEMA_MODULAR_COMPLETO.md` (documentação do que foi feito)
- [ ] Revisar serviços em `Application/Services/`
- [ ] Revisar entidades em `Domain/`
- [ ] Revisar migrations em `Infrastructure/Migrations/`
- **Tempo estimado:** 2-3 horas

### 2. Validar Integridade do Banco de Dados
- [ ] Executar migrations (EF Core)
- [ ] Verificar se todas as tabelas foram criadas
- [ ] Testar conexão com SQL Server
- [ ] Validar relacionamentos entre entidades
- **Tempo estimado:** 1 hora

### 3. Criar Testes Unitários Básicos
- [ ] Testes para `ConfiguracaoNegocioService`
- [ ] Testes para `AutenticacaoService`
- [ ] Testes para `ModulosPorTipo` (mapeamento de tipos)
- [ ] Setup de projeto de testes se não existir
- **Tempo estimado:** 3-4 horas

---

## 📈 Tarefas de Médio Prazo (Próximas 1-2 semanas)

### Fase 8: Relatório de Configuração

#### 8.1 Dashboard Admin
```
Criar FrmRelatorioConfiguracao com:
- Tabela de módulos por tipo de negócio
- Gráfico de ativação de módulos
- Histórico de mudanças de configuração
- Filtros por data/tipo/usuário
```

**Arquivos a criar/modificar:**
- `Desktop/Forms/FrmRelatorioConfiguracao.cs` (novo)
- `Application/Services/RelatorioConfiguracaoService.cs` (novo)
- `Infrastructure/Migrations/[date]_AddRelatorioTables.cs` (novo)

**Dependências:**
- IA1 não interfere (é service + DB)

**Tempo estimado:** 8-12 horas

#### 8.2 Histórico de Mudanças
```
Tabela: ConfiguracaoAudit
- ID (PK)
- ConfiguracaoNegocioId (FK)
- ModulosAnterior (string com flags)
- ModulosNovo (string com flags)
- DataMudanca
- UsuarioQueFez (FK)
- Motivo (string)
```

**Arquivos:**
- `Domain/Entities/ConfiguracaoAudit.cs` (novo)
- Migration para criar tabela
- `Application/Services/ConfiguracaoAuditService.cs` (novo)

**Tempo estimado:** 4-6 horas

#### 8.3 Exportar para PDF/Excel
```
Implementar:
- ExportarRelatorioPDF(filtros)
- ExportarRelatorioExcel(filtros)
- EmailRelatorio(email, filtros)
```

**Bibliotecas sugeridas:**
- iText7 ou SelectPdf (PDF)
- EPPlus ou ClosedXML (Excel)

**Tempo estimado:** 6-8 horas

---

### Fase 9: Multi-tenant

#### 9.1 Suporte a Múltiplas Instalações
```
Adicionar coluna:
- Empresa.InstalacaoId (FK → nova tabela Instalacao)
- ConfiguracaoNegocio.InstalacaoId (opcional, para future)

Criar migração para popolar dados existentes
```

**Tempo estimado:** 4-6 horas

#### 9.2 Switching Dinâmico de Tipos
```
Implementar:
- TrocarTipoNegocio(novoTipo, insId)
- ValidarTrocaTipo() com confirmação
- BackupAntesDeTroca()
- RecarregarConfiguracao()
```

**Tempo estimado:** 6-8 horas

#### 9.3 Backup Isolado
```
Modificar BackupService para:
- Backup por tipo/instalação
- Restore por tipo/instalação
- Agendamento diferenciado
```

**Tempo estimado:** 6-8 horas

---

## 🔍 Tarefas de Longo Prazo

### Fase 10: Customização Avançada

#### 10.1 Sistema de Customização
```
Tabela: CustomizacaoUIConfig
- ID
- TipoNegocio (FK)
- CorPrimaria (string hex)
- CorSecundaria
- Logo (blob)
- Fonte
- IconeCustomizado
- DataCriacao

Service: CustomizacaoUIService
- SalvarCustomizacao()
- ObterCustomizacao()
- ResetarPadrao()
```

**Tempo estimado:** 8-12 horas

#### 10.2 Reordenação de Menu
```
Tabela: MenuOrdem
- ID
- TipoNegocio (FK)
- ModuloSistema (enum)
- Ordem (int)
- Visivel (bool)

Service: MenuOrdenService
- Reordenar()
- Ocultar()
- Mostrar()
- ResetarOrdem()
```

**Tempo estimado:** 6-8 horas

---

## 🛠️ Setup Inicial (IA2)

### 1. Clonar/Acessar o Repositório
```bash
cd C:\Users\victo\Documents\projeto
git status
git log --oneline
```

### 2. Executar Migrations
```bash
cd src/ProjetoVarejo.Infrastructure
dotnet ef database update
```

### 3. Verificar Compilação
```bash
cd C:\Users\victo\Documents\projeto
dotnet build
```

### 4. Fazer primeiro commit
```bash
git pull origin main
git checkout -b feature/backend-improvements
# ... fazer mudanças ...
git add .
git commit -m "setup: IA2 iniciando trabalho no backend - IA2"
git push origin feature/backend-improvements
```

---

## 📁 Estrutura de Arquivos (Backend)

```
src/
├── ProjetoVarejo.Domain/
│   ├── Entities/
│   │   ├── ConfiguracaoNegocio.cs      ← Entidade principal
│   │   └── [outras entidades]
│   └── Enums/
│       ├── TipoNegocio.cs              ← 8 tipos
│       └── ModuloSistema.cs            ← 16 módulos [Flags]

├── ProjetoVarejo.Application/
│   ├── Services/
│   │   ├── ConfiguracaoNegocioService.cs
│   │   ├── ImplantacaoService.cs
│   │   ├── AutenticacaoService.cs
│   │   └── [outras services]
│   └── Configuracao/
│       ├── ModulosPorTipo.cs           ← Mapeamento crucial
│       └── ValidadorSetupInicial.cs

└── ProjetoVarejo.Infrastructure/
    ├── Data/
    │   ├── AppDbContext.cs             ← Contexto EF Core
    │   └── DbInitializer.cs
    └── Migrations/
        └── [todas as migrations]
```

---

## 🎓 Documentação Essencial para Lia2

**Ler em ordem:**

1. **SISTEMA_MODULAR_COMPLETO.md**
   - O que foi feito nas fases 1-7
   - Como o sistema modular funciona

2. **AI_COORDINATION.md** (este arquivo)
   - Como as IAs trabalham juntas

3. **Código:**
   - `Domain/Enums/TipoNegocio.cs` - 8 tipos de negócio
   - `Domain/Enums/ModuloSistema.cs` - 16 módulos com [Flags]
   - `Application/Configuracao/ModulosPorTipo.cs` - Mapeamento central
   - `Application/Services/ConfiguracaoNegocioService.cs` - Serviço principal

---

## ✅ Checklist de Início (IA2)

- [ ] Clonei/acessei o repositório
- [ ] Executei `git status` com sucesso
- [ ] Li SISTEMA_MODULAR_COMPLETO.md
- [ ] Entendi TipoNegocio e ModuloSistema
- [ ] Executei `dotnet build` sem erros
- [ ] Executei migrations do banco
- [ ] Testei login na aplicação
- [ ] Fiz primeiro commit com "IA2"
- [ ] Atualizei AI_COORDINATION.md com meu status

---

## 📊 Prioridade de Tarefas

```
🔴 CRÍTICO (Fazer primeiro)
├─ Revisar código existente
├─ Validar banco de dados
└─ Criar testes básicos

🟡 IMPORTANTE (2ª prioridade)
├─ Fase 8 (Relatórios)
└─ Fase 9 (Multi-tenant)

🟢 DESEJÁVEL (quando tiver tempo)
└─ Fase 10 (Customização)
```

---

## 💬 Comunicação

**Antes de começar uma tarefa:**
1. Atualize `AI_COORDINATION.md` com seu status
2. Marque como "✅ Em progresso"
3. Faça commits regularmente

**Se encontrar problema:**
1. Abra issue em `AI_COORDINATION.md`
2. Marque com ⚠️
3. Aguarde feedback da IA1

---

**Bem-vindo ao Projeto Varejo! 🎉**

Qualquer dúvida, consulte:
- AI_COORDINATION.md (este projeto)
- SISTEMA_MODULAR_COMPLETO.md (o que foi feito)
- Código-fonte com comentários
