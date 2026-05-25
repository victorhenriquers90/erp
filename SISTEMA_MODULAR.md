# Sistema Modular por Tipo de Negócio

## 📋 Visão Geral

O Projeto Varejo agora suporta um **sistema de módulos dinâmicos** onde cada instalação é configurada para um tipo específico de negócio, carregando apenas os módulos necessários.

### Benefícios

✅ **Interface simplificada** - Usuários veem apenas as opções relevantes  
✅ **Menos overhead** - Módulos não necessários não são carregados  
✅ **Suporte melhorado** - Documentação e suporte focados por tipo  
✅ **Escalabilidade** - Fácil adicionar novos tipos e módulos  
✅ **Manutenção** - Código mais organizado e testável

---

## 🎯 Tipos de Negócio Suportados

| Tipo | Ícone | Descrição |
|------|-------|-----------|
| **Padaria** | 🥐 | Produção de pães, bolos e confeitaria |
| **Açougue** | 🥩 | Venda de carnes e derivados |
| **Loja** | 🛍️ | Varejo geral |
| **Indústria** | 🏭 | Manufatura e produção |
| **Bazar** | 🧺 | Pequeno comércio e armarinho |
| **Supermercado** | 🛒 | Varejo de grande escala |
| **Farmácia** | 💊 | Farmácia e drogaria |
| **Restaurante** | 🍽️ | Bar e restaurante |

---

## 📦 Módulos Disponíveis

### Módulos Obrigatórios (em todas as instalações)
- **PDV** - Ponto de Venda e Frente de Caixa
- **Estoque** - Gestão de Estoque
- **Cadastros** - Produtos, Clientes, Fornecedores
- **Financeiro** - Contas a Pagar/Receber
- **Relatórios** - Relatórios e Analytics
- **Auditoria** - Governança e Compliance
- **Backup** - Backup e Restauração

### Módulos Opcionais
- **Fiscal** - NFC-e, Integração SEFAZ
- **Produção** - Controle de produção, BOM
- **Pesagem** - Balança integrada
- **Pré-venda** - Promoções e ofertas
- **Comissões** - Comissões de vendedores
- **PIX** - Integração com PIX
- **TEF** - Transferência Eletrônica de Fundos
- **Receitas** - Controle de receitas (Farmácia)
- **Comandas** - Mesas e comandas (Restaurante)

---

## 📊 Mapeamento de Módulos por Tipo

### 🥐 Padaria
```
✓ PDV, Estoque, Cadastros, Financeiro (obrigatórios)
✓ Fiscal, Produção, Pesagem, PIX
```

### 🥩 Açougue
```
✓ PDV, Estoque, Cadastros, Financeiro (obrigatórios)
✓ Fiscal, Produção, Pesagem, PIX, TEF
```

### 🛍️ Loja
```
✓ PDV, Estoque, Cadastros, Financeiro (obrigatórios)
✓ Fiscal, Pré-venda, Comissões, PIX, TEF
```

### 🏭 Indústria
```
✓ PDV, Estoque, Cadastros, Financeiro (obrigatórios)
✓ Fiscal, Produção, Comissões, PIX, TEF
```

### 🧺 Bazar
```
✓ PDV, Estoque, Cadastros, Financeiro (obrigatórios)
✓ Fiscal, Pré-venda, PIX
```

### 🛒 Supermercado
```
✓ PDV, Estoque, Cadastros, Financeiro (obrigatórios)
✓ Fiscal, Pré-venda, Pesagem, Comissões, PIX, TEF
```

### 💊 Farmácia
```
✓ PDV, Estoque, Cadastros, Financeiro (obrigatórios)
✓ Fiscal, Receitas, PIX, TEF
```

### 🍽️ Restaurante
```
✓ PDV, Estoque, Cadastros, Financeiro (obrigatórios)
✓ Fiscal, Produção, Comandas, PIX, TEF
```

---

## 🛠️ Arquitetura Técnica

### Arquivos Criados

```
Domain/
├── Enums/
│   ├── TipoNegocio.cs          # Enum de tipos de negócio
│   └── ModuloSistema.cs        # Enum de módulos (flags)
├── Configuracao/
│   └── ConfiguracaoNegocio.cs  # Entidade de configuração

Application/
└── Configuracao/
    ├── ModuloRequeridoAttribute.cs    # Atributo para marcar classes
    ├── ModulosPorTipo.cs              # Mapeamento módulos ↔ tipos
    ├── ConfiguracaoNegocioService.cs  # Serviço de gerenciamento
    └── ExemploConfiguracao.cs         # Exemplos de uso
```

### Fluxo de Dados

```
1. Instalação
   └─→ Setup inicial pergunta tipo de negócio
   
2. Configuração
   └─→ ModulosPorTipo recomenda módulos
   └─→ ConfiguracaoNegocioService salva no BD
   
3. Execução
   └─→ Formulários checam [ModuloRequerido] attribute
   └─→ Sidebar carrega seções dinamicamente
   └─→ Menus mostram apenas opções ativas
```

---

## 💻 Como Usar

### 1. Configurar a Injeção de Dependência

**Program.cs:**
```csharp
services.AddScoped<ConfiguracaoNegocioService>();
```

### 2. Obter a Configuração Atual

```csharp
var config = await _configuracaoService.ObterConfiguracao();

// Verificar módulo específico
if (config.EstaModuloAtivo(ModuloSistema.Producao))
{
    // Carregar formulário de produção
}
```

### 3. Configurar Tipo de Negócio (Setup Inicial)

```csharp
await _configuracaoService.ConfigurarNegocio(
    TipoNegocio.Padaria,
    "Padaria Artesanal do João"
);
```

### 4. Marcar Formulários/Classes com Atributo

```csharp
[ModuloRequerido(ModuloSistema.Producao)]
public class FrmProducao : Form
{
    // Apenas carregado se Produção estiver ativa
}
```

### 5. Carregar Sidebar Dinamicamente

```csharp
var config = await _configuracaoService.ObterConfiguracao();

var secoes = new List<SidebarSecao>();

// Seções obrigatórias
secoes.Add(new SidebarSecao { Titulo = "Principal", ... });
secoes.Add(new SidebarSecao { Titulo = "Vendas", ... });

// Seções condicionais
if (config.EstaModuloAtivo(ModuloSistema.Producao))
    secoes.Add(new SidebarSecao { Titulo = "Produção", ... });

if (config.EstaModuloAtivo(ModuloSistema.Comissoes))
    secoes.Add(new SidebarSecao { Titulo = "Vendedores", ... });

var sidebar = new Sidebar(secoes);
```

### 6. Obter Status de Todos os Módulos

```csharp
var status = await _configuracaoService.ObterStatusModulos();

foreach (var (modulo, ativo) in status)
{
    var descricao = ModulosPorTipo.ObterDescricaoModulo(modulo);
    var eObrigatorio = ModulosPorTipo.EObrigatorio(modulo);
    
    Console.WriteLine($"{descricao}: {(ativo ? "✓" : "✗")} {(eObrigatorio ? "[Obrigatório]" : "")}");
}
```

---

## 📝 Exemplo: Formulário de Setup Inicial

```csharp
public class FrmConfiguracaoInicial : Form
{
    private readonly ConfiguracaoNegocioService _config;
    
    private async void BtnConfigurar_Click(object sender, EventArgs e)
    {
        // Usuário seleciona tipo de negócio
        var tipo = (TipoNegocio)cmbTipo.SelectedValue;
        var descricao = txtDescricao.Text;
        
        // Salvar configuração
        await _config.ConfigurarNegocio(tipo, descricao);
        
        // Mostrar módulos carregados
        var modulosAtivos = await _config.ObterStatusModulos();
        
        MessageBox.Show(
            $"Sistema configurado para {tipo}\n\n" +
            $"Módulos carregados: {modulosAtivos.Count(m => m.Value)}"
        );
    }
}
```

---

## 🔧 Próximos Passos

### Para a Tarefa 2 (Setup Inicial)
- Criar `FrmConfiguracao` - formulário de primeiro acesso
- Integrar no `Program.cs` para executar no primeiro launch
- Validar configuração antes de abrir `FrmMain`

### Para a Tarefa 3 (Sidebar Dinâmica)
- Modificar `FrmMain.cs` para carregar sidebar dinamicamente
- Usar `[ModuloRequerido]` para filtrar formulários
- Validar permissões + módulos ativos

---

## 📚 Referência Rápida

```csharp
// Verificar se módulo está ativo
config.EstaModuloAtivo(ModuloSistema.Producao)

// Ativar módulo
await config.AtivarModulo(ModuloSistema.Pesagem);

// Desativar módulo (exceto obrigatórios)
await config.DesativarModulo(ModuloSistema.Prevenda);

// Obter módulos recomendados para um tipo
ModulosPorTipo.ObterModulosRecomendados(TipoNegocio.Padaria)

// Obter descrição de um módulo
ModulosPorTipo.ObterDescricaoModulo(ModuloSistema.Producao)

// Verificar se é obrigatório
ModulosPorTipo.EObrigatorio(ModuloSistema.PDV)
```

---

## ⚠️ Notas Importantes

1. **Módulos obrigatórios não podem ser desativados** - O sistema impedirá
2. **Configuração é global** - Uma única configuração por instalação
3. **Cache em memória** - `ConfiguracaoNegocioService` cacheia a configuração
4. **Migrations necessárias** - Rodar `dotnet ef database update` após clonar/atualizar
5. **Atributo é documentação** - O atributo `[ModuloRequerido]` é informativo; você precisa fazer o filtering nos formul ários

---

## 🐛 Troubleshooting

**Erro: "ConfiguracaoNegocio" não encontrado**
→ Executar migration: `dotnet ef migrations add AddConfiguracaoNegocio` e `dotnet ef database update`

**Módulo não aparece no Sidebar**
→ Verificar se está ativo: `config.EstaModuloAtivo(modulo)`

**Atributo [ModuloRequerido] não funciona**
→ Atributo é apenas documentação; fazer filtering no código da forma/sidebar

---

**Criado em:** 25/05/2026  
**Versão:** 1.0  
**Autor:** Sistema Modular
