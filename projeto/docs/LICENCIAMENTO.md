# Guia de Licenciamento — Projeto ERP

Este documento explica como **gerar e gerenciar licenças** do Projeto ERP.
É de uso **interno do fornecedor** — não distribua junto com o sistema do cliente.

---

## 1. Como funciona (visão geral)

O Projeto ERP usa **licenciamento offline, por máquina, assinado com RSA-2048**:

- Cada cópia do ERP só funciona em uma máquina **ativada**.
- A licença é uma **chave de texto** assinada digitalmente com a sua **chave privada**.
- O ERP só tem a **chave pública** (embutida), que serve para *verificar* a licença — não para criá-la.
- **Ninguém consegue forjar uma licença** sem a chave privada, que fica somente com você.

Modelo configurado: **licença perpétua** (compra única) com **trava por máquina** e **ativação offline** (sem internet, sem servidor).

---

## 2. Arquivos importantes

| Arquivo | Onde fica | Versionar? |
|--------|-----------|-----------|
| `chave-publica.key` | embutida no ERP (`src/ProjetoVarejo.Desktop.Wpf/`) | ✅ Sim |
| `chave-privada.key` | só com você (`tools/ProjetoVarejo.LicenseGen/`) | ❌ **NUNCA** (no `.gitignore`) |
| `licenca.key` | gerado na máquina do cliente | ❌ Não |
| `fingerprint.txt` | gerado na máquina do cliente (código da máquina) | ❌ Não |

> ⚠️ **FAÇA BACKUP DA `chave-privada.key`** em local seguro (fora da máquina, ex.: cofre/pendrive).
> Se você perdê-la, **não conseguirá mais gerar licenças** e teria que trocar a chave pública
> de **todos** os clientes já instalados.

---

## 3. Configuração inicial (uma única vez)

As chaves já foram geradas. Caso precise gerar um **novo par** (atenção: invalida licenças antigas):

```bash
cd tools/ProjetoVarejo.LicenseGen
dotnet run -- genkeys
```

Isso cria `chave-publica.key` e `chave-privada.key` na pasta atual.
Depois: copie a `chave-publica.key` para `src/ProjetoVarejo.Desktop.Wpf/` (ela é embutida no build)
e guarde a `chave-privada.key` em segurança.

---

## 4. Ativar a licença de um cliente (passo a passo)

1. O cliente instala e abre o ERP. Sem licença, aparece a **tela de Ativação** mostrando o
   **Código da máquina** (ex.: `8F59-6796-8E25-025D`).
2. O cliente envia esse código para você (WhatsApp, e-mail, etc.).
3. Você gera a chave:

   ```bash
   cd tools/ProjetoVarejo.LicenseGen
   dotnet run -- gen "8F59-6796-8E25-025D" "Mercado do João Ltda" PERPETUA
   ```

   A ferramenta imprime a **CHAVE DE LICENÇA** (um texto longo).
4. Você envia a chave ao cliente.
5. O cliente cola a chave na tela de Ativação e clica **Ativar**. Pronto — ativado para sempre
   naquela máquina.

---

## 5. Licença anual (opcional)

Se quiser vender por assinatura (em vez de perpétua):

```bash
dotnet run -- gen "<codigo-da-maquina>" "<cliente>" ANUAL 365
```

A licença expira no número de dias informado (365 = 1 ano). Após expirar, o ERP volta a
pedir ativação. Para renovar, gere uma nova chave com a mesma máquina.

---

## 6. Troca de máquina do cliente

Cada máquina tem um código diferente. Se o cliente trocar de computador/servidor:
1. Ele abre o ERP no computador novo → obtém o **novo código**.
2. Você gera uma **nova chave** para esse código.
3. Ele ativa no computador novo.

(A licença antiga simplesmente deixa de ser usada — está presa à máquina antiga.)

---

## 7. Problemas comuns

| Mensagem na ativação | Causa | Solução |
|----------------------|-------|---------|
| "Licença emitida para outra máquina." | A chave foi gerada para um código diferente | Confirme o código da máquina e gere a chave de novo |
| "Assinatura da licença inválida." | A chave foi copiada incompleta/alterada | Reenvie a chave **completa** (sem quebrar linhas/espaços) |
| "Licença expirada em ..." | Licença anual venceu | Gere uma nova chave (renovação) |
| "Nenhuma licença encontrada." | Primeira execução, ainda não ativada | Normal — siga o passo a passo de ativação |

---

## 8. Resumo dos comandos

```bash
# Gerar par de chaves (só na configuração inicial)
dotnet run --project tools/ProjetoVarejo.LicenseGen -- genkeys

# Gerar licença perpétua para um cliente
dotnet run --project tools/ProjetoVarejo.LicenseGen -- gen "<codigo>" "<cliente>" PERPETUA

# Gerar licença anual (365 dias)
dotnet run --project tools/ProjetoVarejo.LicenseGen -- gen "<codigo>" "<cliente>" ANUAL 365
```
