# LaviApp - site de divulgação

Este workspace agora tem uma estrutura simples para apresentar o app e manter uma demonstração funcional.

## Arquivos principais

- `index.html`: landing page pública do LaviApp, alinhada ao logo e aos estilos escuros/dourados já usados no projeto.
- `app.html`: demonstração funcional preservada a partir do app local anterior.
- `chatgpt-share.html`: cópia local do compartilhamento usado como referência de recuperação.

## O que a landing page comunica

- Gestão financeira familiar.
- Identidade visual com fundo escuro, logo circular dourado e acentos verdes/dourados.
- Painel com renda, gastos e saldo.
- Filtros por mês, pessoa, categoria, pagamento, dia e passeio.
- Cadastro de gastos, renda, membros, categorias e itens.
- Controle de passeios com limite, gasto, saldo e barra de progresso.
- Relatórios mensais, diários e por passeio.

## Como abrir localmente

Abra `index.html` no navegador para ver o site de divulgação.

Abra `app.html` para testar a demonstração do app. A demo usa `localStorage`, então os dados ficam salvos no próprio navegador.

## Publicação

Como os arquivos são estáticos, você pode publicar a pasta em Firebase Hosting, Netlify, Vercel ou outro serviço de hospedagem estática.

Se for usar Firebase Hosting, coloque `index.html` e `app.html` na pasta pública configurada no `firebase.json` e rode:

```bash
firebase deploy --only hosting
```

## Próximos ajustes recomendados

- Trocar os CTAs por links reais de WhatsApp, loja, lista de espera ou formulário.
- Definir se a marca pública será `LaviApp` ou `Lavínia`.
- Conectar a versão real com Firebase Auth e Firestore quando o app sair da demo local.
