# Varejo Flow - SaaS vendavel

Este projeto ja tem a base para operar como SaaS:

- login local de demonstracao e login Firebase quando configurado;
- multiempresa;
- cargos e permissoes;
- planos com limites;
- sincronizacao Firestore por empresa;
- leitor de codigo de barras via teclado;
- impressao de cupom pelo dialogo do navegador.

## 1. Firebase

Crie um projeto Firebase novo para o Varejo Flow, separado do sistema antigo.

Ative:

- Authentication com e-mail e senha;
- Firestore Database;
- Hosting, se quiser publicar como web app.

Depois copie os dados do app web do Firebase para `firebase-config.js`:

```js
window.VAREJO_FIREBASE_CONFIG = {
  apiKey: "...",
  authDomain: "...firebaseapp.com",
  projectId: "...",
  storageBucket: "...appspot.com",
  messagingSenderId: "...",
  appId: "..."
};
```

Enquanto esse arquivo estiver com `null`, o app roda em modo local/demo.

## 2. Regras

Use as regras em `firebase/firestore.rules` no projeto Firebase do Varejo Flow.

A versao atual sincroniza um snapshot operacional em:

```txt
companies/{companyId}/snapshots/main
```

Para escalar com muitos clientes, o proximo passo e gravar tambem colecoes normalizadas:

```txt
companies/{companyId}/products/{productId}
companies/{companyId}/sales/{saleId}
companies/{companyId}/movements/{movementId}
companies/{companyId}/cashSessions/{sessionId}
companies/{companyId}/members/{uid}
```

## 3. Assinatura

O app ja guarda `planId` e `subscriptionStatus` em cada empresa.

Para vender de verdade, conecte um gateway por Firebase Functions:

1. Botao de checkout chama uma Function HTTPS.
2. A Function cria uma assinatura no Stripe, Mercado Pago ou Asaas.
3. O webhook do gateway atualiza:

```txt
companies/{companyId}.planId
companies/{companyId}.subscriptionStatus
```

## 4. Leitor de codigo

A maioria dos leitores USB funciona como teclado. No PDV:

1. Abra a tela PDV.
2. Clique no campo de busca.
3. Bipe o produto.
4. O app adiciona automaticamente quando o leitor envia Enter.

## 5. Impressora termica

A primeira versao usa `window.print()` com cupom em 58mm ou 80mm.

Para impressao direta sem dialogo, sera necessario criar um modulo desktop com Electron/Tauri ou integrar WebUSB/WebSerial para modelos compativeis.
