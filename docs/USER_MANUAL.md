# Manual do Usuário — ProjetoVarejo

## Fluxo diário típico

```
1. Login → 2. Abrir caixa → 3. Vender (PDV) → 4. Fechar caixa → 5. Conferir relatórios
```

## 1. Login

- Usuário e senha cadastrados pelo administrador
- Se houver mais de uma empresa cadastrada, vai pedir para escolher após o login

## 2. Caixa

**Menu → Caixa**

### Abertura
- Clique em **ABRIR CAIXA**
- Informe o valor inicial em dinheiro (fundo de troco)
- Caixa fica "Aberto" — verde no topo

### Sangria (retirada)
- Use quando o caixa está com muito dinheiro (saca para o cofre/banco)
- Informe valor e motivo (obrigatório)

### Suprimento (reforço)
- Use quando precisa de mais troco
- Informe valor e motivo

### Fechamento
- Clique em **FECHAR CAIXA**
- O sistema mostra o **esperado** (abertura + suprimentos − sangrias + vendas em dinheiro)
- Conte o dinheiro fisicamente, informe o valor contado
- Sistema calcula **diferença** (sobra ou falta)
- Confirme — fica registrado no histórico

> **Sem caixa aberto, o PDV não permite iniciar vendas.**

## 3. PDV — Frente de Caixa

**Menu → Vendas → Frente de Caixa**

### Atalhos
| Tecla | Ação |
|-------|------|
| Enter (campo código) | Adiciona item |
| F2 | Buscar produto por descrição |
| F4 | Aplicar desconto |
| F10 | Finalizar venda |
| F12 | Nova venda |
| Del (na grid) | Remover item selecionado |
| Esc | Cancelar venda atual |

### Operações
1. Bipar código de barras ou digitar e pressionar Enter
2. Continuar adicionando itens
3. F10 → tela de pagamento aparece
4. Escolha forma(s) de pagamento; pode misturar
5. Confirmar → sistema pergunta se emite NFC-e
6. Se "Sim", emite NFC-e (e imprime cupom automaticamente se configurado)
7. Se "Não", imprime cupom não fiscal

### Pagamento misto
Pode lançar várias formas de pagamento na mesma venda. Ex: R$ 50 dinheiro + R$ 100 cartão.

### Cancelar venda
- Esc cancela a venda em andamento (sem afetar estoque)
- Para cancelar uma venda **já finalizada**, vá em Vendas → Notas Fiscais (se tiver NFC-e) ou via SQL

## 4. Cadastros

### Produtos
**Menu → Cadastros → Produtos**

Campos essenciais:
- **Código** — interno (obrigatório, único)
- **Cód. Barras** — EAN do produto (usado para bipar no PDV)
- **Descrição**
- **Preço Custo** e **Preço Venda**
- **Estoque** e **Estoque Mínimo** (alerta)
- **Controla Estoque** — desmarque para serviços ou itens sem controle (ex: pão por kg)
- **Dados Fiscais** — NCM, CFOP, CST ICMS (importante para NFC-e)

### Clientes e Fornecedores
Cadastros padrão. Cliente pode ser PF ou PJ. Fornecedor é PJ obrigatoriamente (CNPJ único).

## 5. Estoque

**Menu → Estoque → Movimentações e Saldo**

### Lançar entrada manual
Use quando recebe mercadoria sem XML (ex: ajuste, compra avulsa):
1. Clique **Lançar Entrada**
2. Buscar produto (digite código/EAN e Tab)
3. Informar quantidade, custo, fornecedor
4. Confirma → estoque atualizado, custo atualizado

### Importar XML NF-e
**Menu → Estoque → Importar XML NF-e**

1. Selecione o arquivo .xml da nota do fornecedor
2. Clique **Analisar** — sistema mostra os itens, marca em verde os já cadastrados e em amarelo os novos
3. Opções:
   - **Criar conta a pagar** — gera no financeiro (com vencimentos das duplicatas se houver)
   - **Criar produtos novos automaticamente** — com margem 30% sobre o custo (ajuste depois)
4. Clique **IMPORTAR** → estoque sobe, contas a pagar criadas

### Ajustar saída
Use para corrigir estoque (ex: produto vencido, perda):
1. **Lançar Ajuste de Saída**
2. Produto, quantidade, observação (motivo)

### Alerta de estoque mínimo
Aba **"Abaixo do Mínimo"** mostra produtos que precisam reposição.

## 6. Financeiro

**Menu → Financeiro**

### Contas a pagar / receber
- Filtrar por tipo, status, período
- Cadastrar manualmente ou auto via importação XML
- **Quitar** — dialog com data, valor pago, juros, multa, desconto, forma de pagamento

### Resumo
Barra inferior mostra: a receber, a pagar, saldo previsto no período filtrado.

## 7. Relatórios

**Menu → Relatórios → Gerenciais**

6 abas com filtro por período:
- **Vendas por Dia** — gráfico mental do faturamento
- **Por Forma de Pagamento** — quanto entrou em cada forma
- **Por Vendedor** — quantidade, total, ticket médio
- **Curva ABC** — classifica produtos (A=80% receita, B=15%, C=5%) — colorida
- **Top Produtos** — 50 mais vendidos
- **Fluxo de Caixa** — entradas vs saídas

Botão **Exportar CSV** exporta a aba ativa.

## 8. NFC-e

**Menu → Vendas → Notas Fiscais (NFC-e)**

- Lista todas as notas emitidas
- **Cancelar Nota** — até 24h após autorização, com justificativa (≥ 15 chars)
- **Inutilizar Faixa** — números nunca emitidos (quebra de sequência por erro)
- **Reenviar Contingência** — envia em lote notas emitidas em modo offline

Detalhes em [NFCE_SETUP.md](NFCE_SETUP.md).

## 9. Multi-empresa

Se tiver mais de uma empresa cadastrada (Configurações → Dados da Empresa), o sistema pede para escolher após login. Toda emissão NFC-e usará a empresa ativa.

## 10. Perguntas frequentes

**Q: Como gerenciar usuarios e senhas?**
R: Acesse **Sistema > Usuarios** para criar operadores, trocar perfis, ativar/inativar acessos e redefinir senhas.

**Q: Esqueci a senha do admin.**
R: Por SQL: `UPDATE Usuarios SET SenhaHash = 'AAAAAAAAAAAAAAAAAAAAAA==.XXXXX' WHERE Login = 'admin'` — recriando via novo hash (não há reset por enquanto).

**Q: Posso usar o sistema sem internet?**
R: Sim, vendas e cadastros funcionam. NFC-e exige internet (ou modo contingência).

**Q: Como integrar com app mobile?**
R: Subir o projeto `ProjetoVarejo.Api` (em outro terminal/serviço). Endpoints REST com auth via API key. Ver Swagger em `/swagger`.

**Q: Como mudar margem padrão na importação XML?**
R: Atualmente fixa em 30%. Editar `NfeImporterService.cs` linha que diz `1.30m`.
