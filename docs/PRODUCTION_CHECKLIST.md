# Checklist de Producao

Use esta lista antes de liberar o sistema para venda real.

## Fiscal

- Dados reais da empresa: razao social, nome fantasia, CNPJ, IE, endereco e codigo IBGE.
- Certificado digital A1 instalado e testado.
- CSC ID e CSC token de producao configurados.
- NFC-e testada em homologacao antes de mudar para producao.
- Serie e proximo numero NFC-e conferidos com o contador.
- Produtos com NCM, CEST quando aplicavel, CFOP, CST/CSOSN e aliquotas revisadas.

## Cadastros e estoque

- Produtos reais importados por XML de NF-e de fornecedores ou planilha conferida.
- Codigos de barras/GTIN validados; quando interno, identificar como codigo proprio da loja.
- Precos de venda revisados.
- Estoque inicial conferido por contagem fisica.
- Estoque minimo configurado para itens criticos.

## Banco e infraestrutura

- SQL Server instalado no servidor da loja.
- Connection string apontando para o servidor correto.
- Migrations aplicadas.
- Backup automatico diario configurado.
- Restauracao de backup testada.
- Firewall e rede local testados para multi-caixa.

## Seguranca

- Senha padrao `admin/admin` removida.
- Usuarios separados por funcao: administrador, gerente, caixa e estoque.
- Permissoes revisadas.
- Certificado A1 e senha guardados em local seguro.
- API key trocada antes de publicar API/PWA.

## Operacao

- Impressora termica testada.
- Leitor de codigo de barras testado.
- Fluxo de abrir caixa, vender, cancelar, sangria, suprimento e fechar caixa validado.
- PIX configurado se a loja aceitar essa forma de pagamento.
- Equipe treinada no PDV e fechamento diario.

## Dentro do sistema

No desktop, abra:

`Sistema > Checklist de Producao`

A tela calcula os principais pontos automaticamente e mostra o que esta pronto, pendente ou precisa de atencao.

## Travas automaticas

- Ao abrir o PDV, o sistema avalia pendencias criticas de producao e pode bloquear a operacao se o ambiente fiscal estiver em producao.
- Ao emitir NFC-e, o sistema bloqueia a emissao quando faltar certificado, CSC, numeracao fiscal ou dados fiscais obrigatorios nos itens da venda.
- Pendencias nao criticas aparecem como aviso para revisao antes do go-live.
