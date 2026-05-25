# Setup Inicial - Formulário de Configuração

## 📋 Visão Geral

O **FrmConfiguracao** é um formulário visual e intuitivo que aparece na primeira execução do sistema, permitindo que o usuário configure o tipo de negócio e carregue automaticamente os módulos apropriados.

## 🎨 Layout da Tela

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  ⚙️ Configuração Inicial do Sistema                             │
│  Selecione o tipo de negócio para otimizar com os módulos...   │
│                                                                 │
├─────────────────────┬─────────────────────────────────────────┤
│                     │                                         │
│  Tipo de Negócio    │  Detalhes da Configuração               │
│                     │                                         │
│  ┌───────────────┐  │  🥐 Padaria                             │
│  │ 🥐 Padaria    │  │                                         │
│  └───────────────┘  │  DESCRIÇÃO DA EMPRESA                  │
│                     │  ┌─────────────────────────────────┐   │
│  ┌───────────────┐  │  │ Padaria Artesanal do João       │   │
│  │ 🥩 Açougue    │  │  └─────────────────────────────────┘   │
│  └───────────────┘  │                                         │
│                     │  MÓDULOS QUE SERÃO ATIVADOS             │
│  ┌───────────────┐  │  ✓ PDV - Ponto de Venda                │
│  │ 🛍️ Loja       │  │  ✓ Gestão de Estoque                   │
│  └───────────────┘  │  ✓ Cadastros                            │
│                     │  ✓ Financeiro                           │
│  ... mais tipos     │  ✓ NFC-e e Integração Fiscal           │
│                     │  ✓ Módulo de Produção                  │
│                     │  ✓ Controle de Pesagem                 │
│                     │  ✓ Integração com PIX                  │
│                     │                                         │
│                     │  ┌──────────────────────────────────┐   │
│                     │  │ CONFIRMAR CONFIGURAÇÃO           │   │
│                     │  └──────────────────────────────────┘   │
│                     │                                         │
└─────────────────────┴─────────────────────────────────────────┘
```

## ⚙️ Fluxo de Execução

### 1. **Inicialização (Program.cs)**
```
Program.cs
  ↓
Carregar configuração do banco
  ↓
Verificar se setup foi concluído
  ↓
  ├─ SIM: Ir para FrmLogin
  └─ NÃO: Mostrar FrmConfiguracao
```

### 2. **Dentro do FrmConfiguracao**
```
Carregar tipos de negócio
  ↓
Usuário seleciona tipo
  ↓
Atualizar módulos recomendados (à direita)
  ↓
Usuário preenche descrição (opcional)
  ↓
Clicar "CONFIRMAR CONFIGURAÇÃO"
  ↓
Salvar no banco de dados
  ↓
Fechar e ir para FrmLogin
```

## 💻 Classes Envolvidas

### FrmConfiguracao.cs
- **Responsabilidade:** Interface visual do setup
- **Métodos principais:**
  - `InitUi()` - Constrói a interface
  - `CriarBotaoTipo()` - Cria botão para cada tipo
  - `SelecionarTipo()` - Seleciona tipo e atualiza detalhes
  - `AtualizarListaModulos()` - Lista módulos recomendados
  - `BtnConfigurar_Click()` - Salva configuração

### ValidadorSetupInicial.cs
- **Responsabilidade:** Verificar se setup é necessário
- **Métodos:**
  - `PrecisaDeSetupInicial()` - Retorna true se setup não foi feito
  - `ObterInfoConfiguracao()` - Resumo da config atual

### ConfiguracaoNegocioService.cs (existente)
- Usado para salvar a configuração no banco

## 🎯 Recursos

### ✅ Implementado
- [x] Seleção visual de tipos de negócio com ícones
- [x] Visualização dinâmica de módulos recomendados
- [x] Campo para descrição/nome da empresa
- [x] Validação antes de confirmar
- [x] Integração com banco de dados
- [x] Verificação automática no Program.cs

### 🔄 Interatividade
- Clique em um tipo para selecionar
- Cor muda para indicar seleção
- Descrição da empresa é opcional
- Botão fica ativo apenas após seleção
- Módulos atualizam em tempo real

## 📊 Mapeamento de Tipos

| Tipo | Ícone | Módulos |
|------|-------|---------|
| Padaria | 🥐 | 8 |
| Açougue | 🥩 | 9 |
| Loja | 🛍️ | 8 |
| Indústria | 🏭 | 8 |
| Bazar | 🧺 | 7 |
| Supermercado | 🛒 | 9 |
| Farmácia | 💊 | 8 |
| Restaurante | 🍽️ | 8 |

## 🔧 Integração com Program.cs

```csharp
// 1. Adicionar na importação
using ProjetoVarejo.Application.Configuracao;

// 2. Registrar serviços
sc.AddScoped<ConfiguracaoNegocioService>();
sc.AddScoped<ValidadorSetupInicial>();
sc.AddTransient<FrmConfiguracao>();

// 3. Verificar setup (após DbInitializer)
var validador = scope.ServiceProvider.GetRequiredService<ValidadorSetupInicial>();
if (await validador.PrecisaDeSetupInicial())
{
    var frmSetup = scope.ServiceProvider.GetRequiredService<FrmConfiguracao>();
    WinFormsApp.Run(frmSetup);
    
    if (frmSetup.DialogResult != DialogResult.OK)
    {
        // Cancelado
        return;
    }
}
```

## 💾 Dados Salvos

Quando o usuário confirma, os seguintes dados são salvos no banco:

```sql
INSERT INTO ConfiguracaoNegocio (
    Id,
    TipoNegocio,
    DescricaoNegocio,
    ConfiguracaoInicial,
    ModulosAtivos,
    DataAtualizacao,
    Versao
) VALUES (
    1,
    1,
    'Padaria Artesanal do João',
    1,
    <flags dos módulos>,
    GETUTCDATE(),
    1
)
```

## 🚀 Próximos Passos (Tarefa 3)

### Modificar FrmMain.cs
- Carregar sidebar dinamicamente baseado em `ConfiguracaoNegocio`
- Verificar `[ModuloRequerido]` attribute antes de mostrar opções
- Ocultar seções não relevantes

### Exemplo:
```csharp
// No construtor de FrmMain
var config = await _configuracaoService.ObterConfiguracao();

var secoes = new List<SidebarSecao>();
// Adicionar seções obrigatórias
// Adicionar seções condicionais baseadas em módulos ativos
```

## 📝 Tratamento de Erros

- ✅ Validação se tipo foi selecionado
- ✅ Tratamento de exceções ao salvar
- ✅ Mensagem de erro amigável
- ✅ Opção de tentar novamente

## 🔐 Segurança

- ✅ Configuração é única por instalação
- ✅ Apenas na primeira execução
- ✅ Pode ser resetada via serviço (admin)
- ✅ Salvo em banco de dados protegido

---

**Criado em:** 25/05/2026  
**Status:** ✅ Concluído  
**Próxima Tarefa:** 3 - Sidebar Dinâmica
