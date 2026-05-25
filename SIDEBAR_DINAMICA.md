# Sidebar Dinâmica - Tarefa 3

## 📋 Visão Geral

O **FrmMain** foi modificado para carregar a sidebar dinamicamente baseado na configuração de negócio criada no setup inicial. Isso garante que o usuário veja apenas os módulos relevantes para seu tipo de negócio.

## 🔄 Fluxo Completo

```
Aplicação Inicia
    ↓
ValidadorSetupInicial.PrecisaDeSetupInicial()
    ├─ SIM → FrmConfiguracao (Tarefa 2)
    │        Usuário seleciona tipo → Salva no BD
    │
FrmMain Abre
    ↓
Constructor recebe ConfiguracaoNegocioService
    ↓
ObterConfiguracao() → Lê tipo e módulos ativos
    ↓
Sidebar carrega dinamicamente conforme módulos
    ↓
Formulários só aparecem se módulo estiver ativo
```

## 📦 Classes Criadas/Modificadas

### Novas
1. **`ModuloFormularioLoader.cs`** ✓
   - Carrega formulários que têm `[ModuloRequerido]` attribute
   - Verifica disponibilidade conforme módulos ativos
   - Análise de quais formulários estão bloqueados

2. **`SidebarBuilderDinamico.cs`** ✓
   - Constrói seções da sidebar conforme módulos ativos
   - Análise de itens por seção

### Modificadas
1. **`FrmMain.cs`** ✓
   - Adiciona injeção de `ConfiguracaoNegocioService`
   - Carrega `ConfiguracaoNegocio` no constructor
   - Modifica método `ModuloAtivo()` para verificar nova config
   - Usa marca visual com tipo de negócio

## 💻 Exemplo: Como Funciona

### Configuração Padaria
```
ConfiguracaoNegocio:
  TipoNegocio: Padaria
  ModulosAtivos: PDV, Estoque, Cadastros, Financeiro, 
                Fiscal, Produção, Pesagem, PIX
```

### Sidebar Resultante
```
Principal
  └─ Cockpit

Vendas
  ├─ PDV           (✓ em Vendas)
  ├─ Caixa         (✓ em Vendas)
  └─ Notas Fiscais (✓ Fiscal ativo)

Cadastros
  ├─ Produtos
  ├─ Clientes
  └─ Fornecedores

Suprimentos
  ├─ Estoque
  └─ Importar NF-e (✓ Fiscal ativo)

Gestão
  ├─ Financeiro
  └─ Relatórios

Sistema
  ├─ Backup
  ├─ Auditoria
  ├─ Checklist de Produção
  └─ Configurações
```

### Se fosse Bazar (sem Produção, sem Pesagem)
```
Sidebar teria:
  ├─ Sem "Pesagem"
  ├─ Sem "Importar NF-e" (não tem Fiscal por padrão)
  ├─ Menos opções no geral
```

## 🎯 Como Usar

### 1. Marcar um Formulário como Requerendo um Módulo

```csharp
[ModuloRequerido(ModuloSistema.Producao)]
public class FrmProducao : Form
{
    // Este formulário só será carregado se Produção estiver ativa
}

[ModuloRequerido(ModuloSistema.Pesagem, ModuloSistema.Producao)]
public class FrmPesagem : Form
{
    // Requer TANTO Pesagem QUANTO Produção
}
```

### 2. Verificar se Módulo Está Ativo

```csharp
var config = await _configuracaoService.ObterConfiguracao();

if (config.EstaModuloAtivo(ModuloSistema.Producao))
{
    // Mostrar opção de produção
}
```

### 3. Analisar Disponibilidade de Formulários

```csharp
var disponibilidade = ModuloFormularioLoader
    .AnalisarDisponibilidade(config.ModulosAtivos);

foreach (var f in disponibilidade)
{
    Console.WriteLine($"{f.Nome}: {(f.Disponivel ? "✓" : "✗")}");
}
```

## 📊 Modificações no FrmMain

### Constructor
```csharp
public FrmMain(
    SessaoApp sessao, 
    ImplantacaoService implantacaoService,
    ConfiguracaoNegocioService configuracaoService)  // NOVO
{
    _sessao = sessao;
    _implantacaoService = implantacaoService;
    _configuracaoService = configuracaoService;      // NOVO
    _configuracaoNegocio = _configuracaoService
        .ObterConfiguracao()
        .GetAwaiter()
        .GetResult();                                  // NOVO
}
```

### Método ModuloAtivo
```csharp
private bool ModuloAtivo(ModuloSistema modulo)
{
    // Verificar configuração nova PRIMEIRO
    if (_configuracaoNegocio?.ConfiguracaoInicial == true)
    {
        return _configuracaoNegocio.EstaModuloAtivo(modulo);
    }

    // Fallback para sistema antigo
    return _implantacaoService.ModuloAtivo(_implantacao, modulo);
}
```

### Marca da Sidebar
```csharp
var marcaTexto = _configuracaoNegocio?.ConfiguracaoInicial == true
    ? _configuracaoNegocio.ObterDescricaoTipo()  // "🥐 Padaria"
    : Tema.NomeProdutoCurto;                     // "PV" (padrão)

var sidebar = new Sidebar(ConstruirSecoes(), marcaTexto: marcaTexto, marcaIcone: "");
```

## 🔧 Compatibilidade

✅ **Totalmente retrocompatível**
- Sistemas que não usarem novo setup continuam com `ImplantacaoService`
- Novo código tenta nova config primeiro, fallback para antiga
- Nenhuma mudança em formulários existentes (a menos que marquem com `[ModuloRequerido]`)

## 📋 Checklist de Implementação

- [x] Criar `ModuloFormularioLoader`
- [x] Criar `SidebarBuilderDinamico`
- [x] Modificar `FrmMain.cs` para:
  - [x] Injetar `ConfiguracaoNegocioService`
  - [x] Carregar `ConfiguracaoNegocio` no constructor
  - [x] Atualizar método `ModuloAtivo()`
  - [x] Usar marca visual dinâmica
- [x] Documentação

## 🚀 Próximas Melhorias (Futuro)

### Fase 4: Marcadores Obrigatórios
- [ ] Marcar formulários principais com `[ModuloRequerido]`
- [ ] Criar validação que impede abertura de formulários não autorizados
- [ ] Toast/erro se tentar abrir formulário sem módulo

### Fase 5: Gerenciador de Módulos
- [ ] Interface para ativar/desativar módulos (admin)
- [ ] Alteração de tipo de negócio em produção
- [ ] Relatório de qual tipo está rodando

### Fase 6: Customização Avançada
- [ ] Temas por tipo de negócio
- [ ] Ícones diferentes para cada tipo
- [ ] Menus contextuais diferentes

## 🔐 Notas de Segurança

✅ Módulos obrigatórios não podem ser desativados
✅ Configuração é global por instalação
✅ Apenas admin pode alterar tipo de negócio
✅ Auditlog registra mudanças de configuração

## 📝 Exemplos de Uso

### Formulário que Requer Produção
```csharp
[ModuloRequerido(ModuloSistema.Producao)]
public class FrmProducao : Form
{
    public FrmProducao(ConfiguracaoNegocioService config)
    {
        var cfg = config.ObterConfiguracao().Result;
        
        if (!cfg.EstaModuloAtivo(ModuloSistema.Producao))
            throw new InvalidOperationException(
                "Módulo de Produção não está ativo para este sistema");
    }
}
```

### Verificação Condicional no Código
```csharp
// Antes: Hardcoded
if (tipo == "Padaria") { /* lógica */ }

// Depois: Dinâmico
var config = await _configuracaoService.ObterConfiguracao();
if (config.EstaModuloAtivo(ModuloSistema.Producao))
{ /* lógica */ }
```

---

## 📊 Tabela de Modules x Tipos

| Módulo | Padaria | Açougue | Loja | Indústria | Bazar | Supermercado | Farmácia | Restaurante |
|--------|---------|---------|------|-----------|-------|--------------|----------|-------------|
| PDV | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Estoque | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Cadastros | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Financeiro | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Fiscal | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Produção | ✓ | ✓ |  | ✓ |  |  |  | ✓ |
| Pesagem | ✓ | ✓ |  |  |  | ✓ |  |  |
| Pré-venda |  |  | ✓ |  | ✓ | ✓ |  |  |
| Comissões |  |  | ✓ | ✓ |  | ✓ |  |  |
| Receitas |  |  |  |  |  |  | ✓ |  |
| Comandas |  |  |  |  |  |  |  | ✓ |
| PIX | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| TEF |  | ✓ | ✓ | ✓ |  | ✓ | ✓ | ✓ |

---

**Status:** ✅ **Concluído**  
**Data:** 25/05/2026  
**Próximo:** Marcar formulários com `[ModuloRequerido]` (Fase 4)
