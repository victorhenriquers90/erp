# 📋 Tarefas da IA1 (Claude) - Projeto Varejo

**Responsável:** Claude (você + Claude)  
**Data de Início:** 2026-05-25  
**Status:** ✅ Ativo

---

## 🎯 Visão Geral

A IA1 (Claude) é responsável por **interface gráfica, temas, UX e formulários**.

A IA2 está cuidando da lógica de negócio e banco de dados.

---

## ✅ Tarefas Concluídas (Fases 1-7)

- ✅ **Fase 1:** Sistema Modular Base (Enums, Services, Attributes)
- ✅ **Fase 2:** Setup Inicial (FrmConfiguracao)
- ✅ **Fase 3:** Sidebar Dinâmica (FrmMain com temas)
- ✅ **Fase 4:** Marcação de Formulários ([ModuloRequerido])
- ✅ **Fase 5:** Validação de Acesso (ScopedFormHelper)
- ✅ **Fase 6:** Temas Customizados (TemasNegocio.cs)
- ✅ **Fase 7:** Gerenciador de Módulos (FrmGerenciadorModulos)
- ✅ **Bonus:** Corrigida tela de login (campos e botão X)

---

## 📈 Tarefas Atuais (Prioridade: ALTA)

### 1. Testes de UI e Validação
- [ ] Testar aplicação em diferentes resoluções de tela
- [ ] Validar responsividade em 1024x768, 1366x768, 1920x1080
- [ ] Testar com janelas redimensionadas
- [ ] Verificar alinhamento de elementos em cada tipo de negócio
- **Tempo estimado:** 2-3 horas

### 2. Documentação de UI
- [ ] Criar guia de componentes WinForms customizados
- [ ] Documentar como adicionar novo tema
- [ ] Documentar como criar novo formulário com módulos
- **Tempo estimado:** 2 horas

### 3. Feedback Visual Melhorado
- [ ] Adicionar loading spinners em operações assíncronas
- [ ] Melhorar animações de transição
- [ ] Adicionar tooltips informativos
- [ ] Validação em tempo real em formulários
- **Tempo estimado:** 4-6 horas

---

## 📋 Tarefas de Médio Prazo (Próximas 1-2 semanas)

### Fase 8: Dashboard Admin (Parte UI)

#### 8.1 Criar FrmRelatorioConfiguracao
```csharp
Formulário com abas:
- "Resumo": Widgets mostrando estatísticas
- "Módulos": Tabela com matriz tipo × módulo
- "Histórico": Timeline de mudanças
- "Exportar": Botões para PDF/Excel
```

**Arquivos a criar:**
- `Desktop/Forms/FrmRelatorioConfiguracao.cs` (novo)

**Estilo:**
- Usar padrão dos formulários atuais (Card, Inputs, Botoes)
- Cores do tema do tipo de negócio selecionado
- Responsive (scroll em grids grandes)

**Dependência:**
- IA2 cria os services que alimentam os dados

**Tempo estimado:** 6-8 horas

#### 8.2 Integrar ao Menu
- [ ] Adicionar opção "Relatórios de Configuração" no menu SISTEMA
- [ ] Proteger com [ModuloRequerido(Backup)]
- [ ] Adicionar ícone e descrição
- **Tempo estimado:** 1 hora

---

### Fase 9: Multi-tenant UI

#### 9.1 Tela de Seleção de Instalação
```
FrmSelecionarInstalacao (similar a FrmSelecionarEmpresa)
- Lista de instalações disponíveis
- Preview do tipo de negócio
- Mostrar última acesso
- Botão conectar/trocar
```

**Tempo estimado:** 4-5 horas

#### 9.2 Trocar Tipo de Negócio
```
Dialog FrmTrocarTipoNegocio
- Confirmar mudança (com aviso de consequências)
- Mostrar modules que serão ativados/desativados
- Progress bar do backup antes de trocar
- Confirmação após sucesso
```

**Tempo estimado:** 4-6 horas

---

## 🎨 Tarefas de Longo Prazo

### Fase 10: Customização Avançada

#### 10.1 UI Editor de Cores
```
FrmCustomizarTema
- Picker de cores para primária, secundária, destaque
- Preview em tempo real
- Salvar como novo tema
- Importar/exportar temas
```

**Tempo estimado:** 8-10 horas

#### 10.2 UI Editor de Logo
```
FrmCustomizarLogo
- Upload de imagem (PNG, JPG)
- Preview no sidebar
- Crop/redimensionar
- Resetar padrão
```

**Tempo estimado:** 4-6 horas

#### 10.3 Reordenador de Menu
```
FrmReordenarMenu
- Drag-drop dos itens do menu
- Checkbox para mostrar/ocultar
- Botão "Resetar Padrão"
- Preview em tempo real no sidebar
```

**Tempo estimado:** 6-8 horas

#### 10.4 Gerenciador de Atalhos
```
FrmAtalhos
- Tabela com atalhos disponíveis
- Permitir editar combinação de teclas
- Validar conflitos
- Salvar em config local
```

**Tempo estimado:** 4-5 horas

---

## 🏆 Melhorias Gerais (Quando tiver tempo)

### UI/UX Enhancements
- [ ] Adicionar dark mode toggle
- [ ] Melhorar contrast em campos desabilitados
- [ ] Adicionar breadcrumb de navegação
- [ ] Menu contextual (botão direito) em tabelas
- [ ] Drag-drop de arquivos em upload fields

### Acessibilidade
- [ ] Suporte a navegação por teclado (Tab, setas)
- [ ] Labels corretos em formulários
- [ ] Contrast ratio adequado (WCAG)
- [ ] Descrições em tooltips

### Performance UI
- [ ] Virtual scrolling em listas grandes (>1000 itens)
- [ ] Lazy loading de formulários
- [ ] Cache de imagens/temas
- [ ] Reduzir redraw em updates

---

## 📁 Estrutura de Arquivos (Frontend)

```
src/ProjetoVarejo.Desktop/
├── Forms/
│   ├── FrmLogin.cs                     ✅ Corrigido
│   ├── FrmMain.cs                      ✅ Com temas
│   ├── FrmConfiguracao.cs              ✅ Setup
│   ├── FrmGerenciadorModulos.cs        ✅ Admin
│   ├── FrmRelatorioConfiguracao.cs     🔲 TODO (Fase 8)
│   └── [outros forms]
│
├── Theme/
│   ├── Tema.cs                         ✅ Core
│   ├── Sidebar.cs                      ✅ Dinâmica
│   ├── Topbar.cs                       ✅ Barra superior
│   └── [componentes customizados]
│
└── [outros]
```

---

## 🎓 Convenções de Código (IA1)

### Naming
```csharp
// Forms
public class FrmNovoFormulario : Form { }

// Componentes
private Label lblTitulo = null!;
private TextBox txtLogin = null!;
private Button btnEntrar = null!;
private Panel pnlContainer = null!;

// Métodos privados
private void InitUi() { }
private void CarregarDados() { }
```

### Layout com Dock
```csharp
// Container principal
var container = new Panel { Dock = DockStyle.Fill };

// Seções empilhadas
var header = new Panel { Dock = DockStyle.Top, Height = 80 };
var content = new Panel { Dock = DockStyle.Fill };
var footer = new Panel { Dock = DockStyle.Bottom, Height = 40 };

// Adicionar em ORDEM INVERSA (último = primeiro)
container.Controls.Add(footer);
container.Controls.Add(content);
container.Controls.Add(header);
```

### Cores e Temas
```csharp
// SEMPRE usar Tema.cor*
BackColor = Tema.CorFundo;
ForeColor = Tema.CorTextoEscuro;
button.BackColor = Tema.CorPrimaria;

// NUNCA hardcode cores
// ❌ BackColor = Color.FromArgb(50, 100, 150);  ERRADO
// ✅ BackColor = Tema.CorPrimaria;               CERTO
```

---

## ✅ Checklist de Cada Tarefa UI

Ao criar novo formulário/componente:

- [ ] Nome segue padrão `Frm[Funcionario]` ou `Componente[Nome]`
- [ ] Usa `Tema.*` para cores, fonts, ícones
- [ ] Responsivo (testa em diferentes resoluções)
- [ ] Trata estados loading/erro/sucesso
- [ ] Comentários em lógica complexa
- [ ] Sem hardcode de valores (use constantes/config)
- [ ] Testado com dados reais
- [ ] Compilou sem warnings
- [ ] Commitar com "IA1 Claude"

---

## 🔗 Integração com IA2

**Você NÃO deve modificar:**
- ❌ Services em `Application/Services/`
- ❌ Entities em `Domain/Entities/`
- ❌ Migrations
- ❌ Database logic

**Você PODE usar:**
- ✅ `ScopedFormHelper.AbrirModal<Form>()` para abrir forms
- ✅ Services via DI (read-only)
- ✅ Enums do Domain (TipoNegocio, ModuloSistema, etc)

**Se precisar de mudança no backend:**
1. Abra issue em `AI_COORDINATION.md`
2. Descreva claramente o que precisa
3. IA2 implementa a solução
4. Você consome via UI

---

## 📊 Prioridade de Tarefas

```
🔴 CRÍTICO (Fazer primeiro)
├─ Testes de UI em diferentes resoluções
├─ Documentação de componentes
└─ Corrigir bugs visuais se encontrar

🟡 IMPORTANTE (2ª prioridade)
├─ Fase 8 (Relatórios UI)
└─ Fase 9 (Seleção multi-tenant UI)

🟢 DESEJÁVEL (quando tiver tempo)
└─ Fase 10 (Customização avançada)
```

---

## 💬 Workflow com IA2

```
IA1: "Preciso que IA2 crie um novo método em ConfiguracaoService"
     ↓
  [Escreve no AI_COORDINATION.md]
     ↓
IA2: Implementa o método
     ↓
IA1: Usa o método via `ScopedFormHelper` ou DI
     ↓
IA1: Faz commit "feat(ui): novo relatório - IA1 Claude"
     ↓
IA2: Puxa mudanças (git pull)
```

---

## 🚀 Como Começar

1. **Abra o projeto:**
   ```bash
   cd C:\Users\victo\Documents\projeto
   dotnet run
   ```

2. **Teste a UI atual:**
   - Login com admin/admin
   - Escolha tipo de negócio (Loja, Padaria, etc)
   - Navegue por diferentes módulos
   - Procure por bugs visuais

3. **Atualize AI_COORDINATION.md:**
   ```markdown
   | **IA1** | Claude | Testando UI | ✅ Em progresso | 25% |
   ```

4. **Comece com primeira tarefa (testes de UI)**

---

**Bem-vindo de volta ao Projeto Varejo! 🚀**

Você já fez um excelente trabalho nas fases 1-7. Agora é hora de melhorar a experiência de usuário e preparar o caminho para IA2 implementar features avançadas!

Qualquer dúvida:
- Leia AI_COORDINATION.md
- Verifique documentação anterior (SISTEMA_MODULAR_COMPLETO.md)
- Consulte o código-fonte com comentários
