# Varejo Flow

MVP independente para venda, entrada e saída de mercadorias em varejo geral.

## O que já funciona

- PDV com carrinho, desconto, formas de pagamento e comprovante.
- Cadastro de produtos com SKU, código de barras, custo, preço, estoque mínimo e fornecedor.
- Entrada, saída, perda e ajuste de estoque.
- Baixa automática de estoque ao finalizar venda.
- Caixa com abertura, sangria, suprimento, fechamento e vendas vinculadas.
- Relatórios por mês, pagamento, categoria e produtos mais vendidos.
- Backup/importação em JSON e exportação CSV de vendas.
- Login local/Firebase, multiempresa, equipe, permissões e planos.
- Leitor de código de barras por teclado e cupom 58mm/80mm.

## Como abrir

Use um servidor estático dentro desta pasta:

```bash
python -m http.server 4173
```

Depois acesse `http://localhost:4173`.

## Desktop, tablet e smartphone

O sistema foi preparado como PWA. Quando publicado em um endereço HTTPS, pode ser instalado:

- Desktop: pelo botão de instalação do Chrome/Edge.
- Tablet Android/iPad: opção "Adicionar à tela inicial".
- Smartphone: opção "Adicionar à tela inicial".

Para uso local no computador, continue usando `iniciar-varejo-flow.bat`.

URL de teste publicada:

```txt
https://varejo-flow-victo-20260523.web.app
```

## Modo vendável

Veja [docs/SAAS_SETUP.md](docs/SAAS_SETUP.md) para configurar Firebase, regras, assinatura e próximos passos de produção.
