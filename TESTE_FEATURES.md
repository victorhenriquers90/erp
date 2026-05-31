# Plano de Testes - Features Completadas

## 🧪 Testes da Aplicação Desktop

### Feature: Filiais (PR #2)
**Localização:** Menu → Sistema → Filiais

#### Test 1: Listar Filiais
- [ ] Abrir a forma FrmFiliais
- [ ] Grid exibe lista de filiais
- [ ] Label no topo mostra "X filial(is) | Y ativa(s)"

#### Test 2: Criar Nova Filial
- [ ] Clicar "Nova filial"
- [ ] Abre FrmFilialEdit (modal)
- [ ] Preencher: Código (ex: "002"), Nome (ex: "Filial Centro")
- [ ] Marcar "É a filial Matriz" (se desejar)
- [ ] Clicar "Salvar"
- [ ] Validação: Código obrigatório, Código único
- [ ] Validação: Nome obrigatório
- [ ] Nova filial aparece na grid

#### Test 3: Editar Filial
- [ ] Duplo clique em filial existente
- [ ] Abre FrmFilialEdit com dados preenchidos
- [ ] Se for Matriz: checkbox "É a filial Matriz" deve estar desabilitado
- [ ] Alterar nome/telefone/endereço
- [ ] Clicar "Salvar"
- [ ] Mudanças aparecem na grid

#### Test 4: Ativar/Inativar
- [ ] Selecionar filial na grid
- [ ] Clicar "Ativar/Inativar"
- [ ] Se Matriz: mensagem "Não é possível inativar a filial Matriz"
- [ ] Status na coluna "Status" muda para "Ativa" ou "Inativa"
- [ ] Cores mudam (verde/vermelho)

#### Test 5: Filtro de Inativas
- [ ] Clicar button "Inativas" (toggle)
- [ ] Grid exibe apenas filiais inaativas
- [ ] Clicar novamente para mostrar todas

---

### Feature: Conexão com Servidor (Feature A)
**Localização:** Program.cs → tentativa de conexão ao iniciar

#### Test 1: FrmConexaoServidor (Modo Cliente)
- [ ] Iniciar app em modo cliente (connection string aponta para IP remoto)
- [ ] Se servidor não responder em 5s: abre FrmConexaoServidor
- [ ] Form mostra campos: IP/hostname, Instância SQL
- [ ] Topbar âmbar com ícone de alerta
- [ ] Clicar "Testar conexão"
- [ ] Spinner aparece, aguarda até 8s
- [ ] Se OK: painel verde "✓ Servidor alcançado"
- [ ] Se falha: painel vermelho com mensagem de erro
- [ ] Clicar "Salvar e Continuar"
- [ ] appsettings.json é salvo com nova connection string
- [ ] App continua inicialização

---

### Feature: Supervisor Unlock (Feature A)
**Localização:** FrmPdv → Ações restritas (Desconto, Alternar Preço)

#### Test 1: Abrir Unlock Dialog
- [ ] Ir para PDV (menu Vendas → PDV)
- [ ] Criar uma venda
- [ ] Tentar aplicar desconto > 5% OU alternar preço de item
- [ ] Abre FrmSupervisorUnlock (dialog azul)
- [ ] Exibe "AÇÃO SOLICITADA" com descrição
- [ ] Campos: Usuário do supervisor, Senha

#### Test 2: Autorizar com Credenciais Válidas
- [ ] Digitar login de um admin/supervisor
- [ ] Digitar senha correta
- [ ] Clicar "Autorizar"
- [ ] Se tem permissão: dialog fecha, ação é autorizada
- [ ] Sessão do operador NÃO muda (operador continua logado)

#### Test 3: Rejeitar Credenciais Inválidas
- [ ] Digitar senha incorreta
- [ ] Clicar "Autorizar"
- [ ] Erro em vermelho: "Credenciais inválidas. X tentativa(s) restante(s)"
- [ ] Campo senha é limpo, foco volta para senha
- [ ] Após 3 tentativas: botão desabilitado, mensagem "Número máximo de tentativas"

#### Test 4: Rejeitar Falta de Permissão
- [ ] Digitar credenciais de um operador (sem permissão)
- [ ] Clicar "Autorizar"
- [ ] Erro: "O usuário 'Nome' não tem permissão para esta ação"
- [ ] Dialog não fecha

---

### Feature: Componentes de Tema (Feature C)

#### Test 1: BotaoModerno
**Localização:** Qualquer formulário com botões

- [ ] **Variante Primario** (azul): Clicar, gradiente aparece, animação suave
- [ ] **Variante Sucesso** (verde): Salvar button
- [ ] **Variante Perigo** (vermelho): Cancelar/Delete buttons
- [ ] **Variante Aviso** (laranja): Testar conexão button
- [ ] **Variante Ghost** (branco): Editar/Selecionar buttons
- [ ] **Toggle**: Button muda cor ao clicar (ativo/inativo)

- [ ] Hover effect: botão fica mais claro
- [ ] Press effect: botão fica mais escuro ao clicar
- [ ] Disabled: botão fica cinza e desbilitado
- [ ] Focus ring: dotted border azul ao tabular

#### Test 2: Abas (TabControl)
**Localização:** Qualquer form com tabs (ex: FrmConfiguracao)

- [ ] Abas têm aparência arredondada
- [ ] Aba selecionada: branco com accent bar azul embaixo
- [ ] Aba não selecionada: cinza claro
- [ ] Clicar em aba diferente: anima suavemente
- [ ] Texto em bold na aba selecionada

---

## ✅ Checklist Final

Após testar tudo:

- [ ] Filiais: CRUD funciona, validações estão corretas
- [ ] Conexão: FrmConexaoServidor testa e salva corretamente
- [ ] Supervisor: Autorização funciona, limite de tentativas funciona
- [ ] Temas: Botões e abas têm visual modernizado
- [ ] Sem crashes ou erros não tratados
- [ ] App inicia sem erros de DI

---

## 🐛 Se Encontrar Problemas

1. Verificar console/logs para erros
2. Confirmar migration foi aplicada: `Database.Filiais` existe
3. Verificar DI em Program.cs: `AddTransient<FrmFiliais>()` está registrado
4. Tentar rebuild: `dotnet clean && dotnet build`

---

**Desenvolvido em:** 2026-05-31
**Features:** Filiais, Conexão Servidor, Supervisor Unlock, Componentes Tema, Auth Web
