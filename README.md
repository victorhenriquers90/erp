# ProjetoVarejo

Sistema de gestão para varejo (lojas, supermercados, padarias, açougues) em C# WinForms .NET 8.

## Módulos implementados

- **PDV / Frente de Caixa** — venda com leitor de código de barras, múltiplas formas de pagamento, troco automático
- **Cadastros** — produtos (com dados fiscais NCM/CFOP/CST), clientes, fornecedores
- **Estoque** — entrada/saída, ajustes, alerta de estoque mínimo, histórico de movimentações
- **Financeiro** — contas a pagar/receber, quitação com juros/multa/desconto, fluxo de caixa
- **Multi-usuário** — login com senha (PBKDF2), perfis (Admin, Gerente, Caixa, Estoquista)
- **NFC-e SEFAZ-SP** — estrutura preparada (entidades, configuração de empresa, certificado), integração na próxima fase

## Stack

- C# .NET 8 + WinForms
- SQL Server Express (LocalDB também funciona)
- Entity Framework Core 8 (Code-First com migrations)
- Microsoft.Extensions.DependencyInjection

## Arquitetura

```
src/
├── ProjetoVarejo.Domain/         entidades + regras de negócio
├── ProjetoVarejo.Application/    services (VendaService, EstoqueService, etc)
├── ProjetoVarejo.Infrastructure/ EF Core, DbContext, migrations, hashing
├── ProjetoVarejo.Shared/         Result, enums
└── ProjetoVarejo.Desktop/        WinForms — telas
```

## Setup

### 1. Pré-requisitos

- Windows 10/11
- .NET 8 SDK
- SQL Server Express (ou LocalDB já incluso no Visual Studio)

### 2. Configurar conexão

Edite `src/ProjetoVarejo.Desktop/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=.\\SQLEXPRESS;Database=ProjetoVarejo;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Para LocalDB use:
```
Server=(localdb)\\MSSQLLocalDB;Database=ProjetoVarejo;Trusted_Connection=True;
```

### 3. Aplicar migrations

```powershell
dotnet ef database update --project src/ProjetoVarejo.Infrastructure --startup-project src/ProjetoVarejo.Infrastructure
```

O `DbInitializer` cria automaticamente:
- Usuário `admin` / senha `admin`
- Empresa de exemplo (CNPJ 00000000000000 — substituir na primeira execução)
- Categorias básicas (Geral, Bebidas, Alimentos, Limpeza, Higiene)

### 4. Rodar

```powershell
dotnet run --project src/ProjetoVarejo.Desktop
```

## Atalhos do PDV

| Tecla | Ação |
|-------|------|
| Enter (campo código) | Adiciona item |
| F2 | Buscar produto por descrição |
| F4 | Aplicar desconto |
| F10 | Finalizar venda (abre tela de pagamento) |
| F12 | Nova venda |
| Del | Remover item selecionado |
| Esc | Cancelar venda |

## Emissão NFC-e SEFAZ-SP

Implementada e integrada ao PDV. Após finalizar venda, o sistema pergunta se deseja emitir NFC-e.

**Pré-requisitos** (configure em **Menu → Configurações → Dados da Empresa**):
- Certificado A1 (.pfx) com chave privada
- Senha do certificado
- CSC ID e Token (obtidos no portal SAT/SEFAZ-SP — "Código de Segurança do Contribuinte")
- Dados da empresa: CNPJ, IE, endereço, código IBGE do município
- Ambiente: Homologação (testes) ou Produção

**Endpoints usados:**
- Hom: `https://homologacao.nfce.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx`
- Prod: `https://nfce.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx`

**Fluxo:**
1. `NfceXmlGenerator` monta XML v4.00 + chave de acesso com DV módulo 11
2. `NfceAssinador` assina `<infNFe>` com SignedXml (RSA-SHA1 + C14N)
3. `SefazSpClient` envia SOAP 1.2 síncrono ao webservice
4. Resposta parseada (cStat 100 = autorizada), protocolo salvo
5. `QrCodeNfce` gera URL do QR Code conforme NT 2015/003

**Limitações da versão atual:**
- Apenas autorização síncrona (sem contingência offline)
- Sem cancelamento / inutilização (eventos separados)
- Tributação assume Simples Nacional (CSOSN 102) — ajustar `ICMSSN102` para outros regimes
- Sem impressão DANFE NFC-e em impressora térmica (ESC/POS)

## Módulos adicionais entregues

- [x] Cancelamento de NFC-e (evento 110111, 24h)
- [x] Inutilização de faixa de numeração
- [x] Modo contingência offline (tpEmis=9) + reenvio em lote
- [x] Impressão térmica ESC/POS (Spooler/Rede/Serial) com QR Code DANFE NFC-e
- [x] Relatórios (Vendas por dia, forma de pagamento, vendedor, curva ABC, top produtos, fluxo de caixa) com export CSV
- [x] Sangria, suprimento e fechamento de caixa com conferência por forma de pagamento
- [x] Importação de XML de NF-e do fornecedor (entrada de estoque + conta a pagar)
- [x] Backup automático do banco SQL Server (manual + diário ao iniciar)
- [x] Multi-empresa (seleção no login quando >1 empresa cadastrada)
- [x] **REST API** (ProjetoVarejo.Api) com Swagger e API Key para apps mobile

## REST API (ProjetoVarejo.Api)

Projeto ASP.NET Core minimal API expondo consultas para apps mobile/terceiros.

**Endpoints:**
- `GET /api/produtos?q=...` — listagem com filtro
- `GET /api/produtos/{id}` — detalhe
- `GET /api/produtos/barras/{codigo}` — busca por código de barras (uso em app de balança/conferência)
- `GET /api/estoque/abaixo-minimo` — alerta de reposição
- `GET /api/estoque/movimentos?produtoId=...&de=...&ate=...`
- `GET /api/clientes?q=...`
- `GET /api/relatorios/vendas-por-dia?de=...&ate=...`
- `GET /api/relatorios/por-forma-pagamento?de=...&ate=...`
- `GET /api/relatorios/top-produtos?de=...&ate=...&n=20`
- `GET /api/relatorios/curva-abc?de=...&ate=...`
- `GET /api/relatorios/fluxo-caixa?de=...&ate=...`

**Autenticação:** header `X-Api-Key: <chave>` (configurar em `src/ProjetoVarejo.Api/appsettings.json` → `ApiKeys`).

**Para rodar:**
```powershell
dotnet run --project src/ProjetoVarejo.Api
```
Acesse `https://localhost:7xxx/swagger` para testar via UI.

## Próximos passos (sugestões)

- [ ] Sincronização offline-first em app mobile (cache + retry)
- [ ] NF-e 55 (Nota Fiscal Eletrônica, modelo diferente de NFC-e) — habilita Carta de Correção
- [ ] Refatoração completa multi-empresa (adicionar `EmpresaId` em todas entidades transacionais)
- [ ] SAT-CF-e (estados que ainda usam SAT)
- [ ] PIX integrado (cobrança dinâmica via API do banco)
- [ ] Tela de PDV em modo touch (tablets)

## Multi-terminal (rede local)

Para usar em múltiplos caixas:

1. Instalar SQL Server Express **no servidor** (PC central da loja)
2. Habilitar TCP/IP no SQL Server Configuration Manager
3. Liberar porta 1433 no firewall do servidor
4. Em cada caixa, ajustar `appsettings.json` para apontar ao servidor:
   ```
   Server=NOME-DO-SERVIDOR\\SQLEXPRESS;Database=ProjetoVarejo;User Id=sa;Password=...;TrustServerCertificate=True;
   ```
