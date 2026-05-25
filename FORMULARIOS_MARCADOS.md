# Formulários Marcados com [ModuloRequerido]

## 📋 Visão Geral

Este documento lista todos os formulários que foram marcados com o atributo `[ModuloRequerido]` para indicar de qual módulo eles dependem.

## ✅ Formulários Marcados

### 🎫 Módulo: **Fiscal**

| Formulário | Arquivo | Descrição |
|-----------|---------|-----------|
| `FrmNotasFiscais` | `FrmNotasFiscais.cs` | Emissão e consulta de NFC-e |
| `FrmImportarNfe` | `FrmImportarNfe.cs` | Importação de NF-e de fornecedores |

```csharp
[ModuloRequerido(ModuloSistema.Fiscal)]
public class FrmNotasFiscais : Form { }

[ModuloRequerido(ModuloSistema.Fiscal)]
public class FrmImportarNfe : Form { }
```

### 🏭 Módulo: **Produção**

| Formulário | Arquivo | Descrição |
|-----------|---------|-----------|
| `FrmChecklistProducao` | `FrmChecklistProducao.cs` | Checklist de go-live |

```csharp
[ModuloRequerido(ModuloSistema.Producao)]
public class FrmChecklistProducao : Form { }
```

### 💰 Módulo: **Financeiro**

| Formulário | Arquivo | Descrição |
|-----------|---------|-----------|
| `FrmFinanceiro` | `FrmFinanceiro.cs` | Contas a pagar/receber |

```csharp
[ModuloRequerido(ModuloSistema.Financeiro)]
public class FrmFinanceiro : Form { }
```

### 📊 Módulo: **Relatórios**

| Formulário | Arquivo | Descrição |
|-----------|---------|-----------|
| `FrmRelatorios` | `FrmRelatorios.cs` | Relatórios e analytics |

```csharp
[ModuloRequerido(ModuloSistema.Relatorios)]
public class FrmRelatorios : Form { }
```

### 💾 Módulo: **Backup**

| Formulário | Arquivo | Descrição |
|-----------|---------|-----------|
| `FrmBackup` | `FrmBackup.cs` | Backup e restauração |

```csharp
[ModuloRequerido(ModuloSistema.Backup)]
public class FrmBackup : Form { }
```

### 🔍 Módulo: **Auditoria**

| Formulário | Arquivo | Descrição |
|-----------|---------|-----------|
| `FrmAuditoria` | `FrmAuditoria.cs` | Logs de auditoria |

```csharp
[ModuloRequerido(ModuloSistema.Auditoria)]
public class FrmAuditoria : Form { }
```

---

## 📝 Formulários SEM Marcação (Sempre Disponíveis)

Estes formulários aparecem em todas as instalações, pois seus módulos são obrigatórios:

| Módulo | Formulários |
|--------|------------|
| **PDV** | `FrmPdv`, `FrmCaixa` |
| **Estoque** | `FrmEstoque` |
| **Cadastros** | `FrmProdutos`, `FrmClientes`, `FrmFornecedores` |
| **Sistema** | `FrmConfigEmpresa`, `FrmUsuarios`, `FrmImplantacao` |

---

## 🎯 Como Funciona

### Exemplo: Padaria (com Fiscal)
```
[ModuloRequerido(ModuloSistema.Fiscal)]
FrmNotasFiscais → ✓ DISPONÍVEL (Padaria tem Fiscal)

[ModuloRequerido(ModuloSistema.Producao)]
FrmChecklistProducao → ✓ DISPONÍVEL (Padaria tem Produção)

[ModuloRequerido(ModuloSistema.Relatorios)]
FrmRelatorios → ✓ DISPONÍVEL (Obrigatório em todos)
```

### Exemplo: Bazar (sem Fiscal, sem Produção)
```
[ModuloRequerido(ModuloSistema.Fiscal)]
FrmNotasFiscais → ✗ BLOQUEADO (Bazar não tem Fiscal)

[ModuloRequerido(ModuloSistema.Producao)]
FrmChecklistProducao → ✗ BLOQUEADO (Bazar não tem Produção)

[ModuloRequerido(ModuloSistema.Relatorios)]
FrmRelatorios → ✓ DISPONÍVEL (Obrigatório em todos)
```

---

## 🔄 Fluxo de Verificação

Quando o usuário tenta abrir um formulário:

```
1. Verificar se FrmXXX tem atributo [ModuloRequerido]
   ├─ NÃO tem → Abrir normalmente (sempre disponível)
   └─ TEM → Ir para passo 2

2. Verificar se módulo está ativo
   ├─ SIM → Abrir formulário
   └─ NÃO → Mostrar mensagem de bloqueio
```

---

## 📊 Tabela de Disponibilidade por Tipo

| Tipo | Fiscal | Produção | Relatórios | Financeiro | Auditoria | Backup | Disponibilidade |
|------|--------|----------|------------|-----------|-----------|--------|-----------------|
| 🥐 Padaria | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 6/6 formulários |
| 🥩 Açougue | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 6/6 formulários |
| 🛍️ Loja | ✓ |  | ✓ | ✓ | ✓ | ✓ | 5/6 formulários |
| 🏭 Indústria | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 6/6 formulários |
| 🧺 Bazar | ✓ |  | ✓ | ✓ | ✓ | ✓ | 5/6 formulários |
| 🛒 Supermercado | ✓ |  | ✓ | ✓ | ✓ | ✓ | 5/6 formulários |
| 💊 Farmácia | ✓ |  | ✓ | ✓ | ✓ | ✓ | 5/6 formulários |
| 🍽️ Restaurante | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | 6/6 formulários |

---

## 🔐 Validação de Acesso

### Implementação Futura
```csharp
private void ValidarAcessoFormulario(Type tipoFormulario)
{
    var atributo = tipoFormulario.GetCustomAttribute<ModuloRequeridoAttribute>();
    
    if (atributo == null) return; // Sem requisito, pode abrir
    
    var config = _configuracaoService.ObterConfiguracao().Result;
    
    if (!atributo.TodosModulosAtivos(config.ModulosAtivos))
    {
        var desc = atributo.ModulosRequeridos.ToString();
        throw new InvalidOperationException(
            $"Este formulário requer os módulos: {desc}");
    }
}
```

### Toast de Erro
```
┌────────────────────────────────────────┐
│ ⚠️  Módulo não disponível              │
│                                        │
│ Este formulário requer o módulo        │
│ "Produção" que não está ativo nesta    │
│ instalação.                            │
│                                        │
│ Contate o administrador para ativar.   │
└────────────────────────────────────────┘
```

---

## 📋 Checklist: Outros Formulários a Marcar (Futuro)

### Módulo: Produção
- [ ] `FrmProducao.cs` (se existir)
- [ ] Formulários específicos de produção

### Módulo: Pesagem
- [ ] Formulários de pesagem/balança

### Módulo: Comissões
- [ ] Formulários de comissões/vendedores

### Módulo: Pré-venda
- [ ] Formulários de promoções

---

## 🎯 Implementação Atual vs Futura

### ✅ Fase 1 (Concluída)
- [x] Marcar formulários principais com `[ModuloRequerido]`
- [x] Documentação de mapeamento

### 🔄 Fase 2 (Próxima)
- [ ] Implementar validação no `ScopedFormHelper.AbrirModal()`
- [ ] Toast de erro quando módulo não ativo
- [ ] Teste de cada tipo de negócio

### 🚀 Fase 3 (Futuro)
- [ ] Interface admin para ativar/desativar módulos
- [ ] Permitir mudança de tipo de negócio
- [ ] Relatório de configuração

---

## 📝 Exemplos de Uso

### Verificar Disponibilidade
```csharp
var disponibilidade = ModuloFormularioLoader
    .AnalisarDisponibilidade(_configuracao.ModulosAtivos);

var formsNaoDisponiveis = disponibilidade
    .Where(f => !f.Disponivel)
    .ToList();

Console.WriteLine($"Formulários bloqueados: {formsNaoDisponiveis.Count}");
foreach (var f in formsNaoDisponiveis)
{
    Console.WriteLine($"  - {f.Nome} (requer: {f.ObterDescricaoModulosRequeridos()})");
}
```

### Listar Formulários de um Módulo
```csharp
var formsProducao = ModuloFormularioLoader
    .ObterFormulariosDisponiveisPorModulo(
        ModuloSistema.Producao, 
        _configuracao.ModulosAtivos);

foreach (var form in formsProducao)
{
    Console.WriteLine($"✓ {form.Name}");
}
```

---

## 📚 Referência

### Atributo Usado
```csharp
[ModuloRequerido(ModuloSistema.Fiscal)]
public class FrmNotasFiscais : Form { }
```

### Namespaces Necessários
```csharp
using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Domain.Enums;
```

### Verificação em Tempo de Execução
```csharp
var atributo = typeof(FrmNotasFiscais)
    .GetCustomAttribute<ModuloRequeridoAttribute>();

if (atributo != null)
{
    Console.WriteLine($"Requer: {atributo.ModulosRequeridos}");
}
```

---

**Status:** ✅ **6 Formulários Marcados**  
**Data:** 25/05/2026  
**Próximo:** Implementar validação de acesso (Fase 2)
