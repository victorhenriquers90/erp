# Configuração de NFC-e (SEFAZ-SP)

## O que você precisa antes de começar

1. **Inscrição Estadual** ativa (CNPJ habilitado para emissão)
2. **Certificado digital A1** (.pfx ou .p12) — comprado de AC (Certisign, Serasa, Soluti, etc.)
3. **CSC** (Código de Segurança do Contribuinte) — gerado no portal SAT-SP
4. Conexão com internet

## Passo 1 — Obter o certificado A1

- Comprar de uma Autoridade Certificadora ICP-Brasil
- Receber arquivo `.pfx` (ou `.p12`) — guarda **chave privada**
- Anotar a senha do PFX

**Backup**: copie o `.pfx` para um pendrive seguro. Se perder, terá que comprar novo.

## Passo 2 — Obter CSC

1. Acessar [SAT-SP](https://www.fazenda.sp.gov.br/sat/) ou portal NFe da SEFAZ-SP
2. Login com certificado digital A1
3. Menu "Gerenciamento de CSC"
4. **Gerar token** — anotar:
   - **CSC ID** (geralmente "1" ou "2")
   - **CSC Token** (string longa, 36+ caracteres)

> ⚠ NUNCA compartilhe o CSC Token. Ele assina a URL do QR Code.

## Passo 3 — Configurar no sistema

**Menu → Configurações → Dados da Empresa / NFC-e**

### Aba "Dados da Empresa"
| Campo | Como preencher |
|-------|----------------|
| Razão Social | Conforme CNPJ |
| Nome Fantasia | Conforme cadastro |
| CNPJ | Só dígitos |
| Inscrição Estadual | Só dígitos (ou "ISENTO" se MEI) |
| Regime Tributário | **1** = Simples Nacional, **3** = Regime Normal, **4** = MEI |

### Aba "Endereço"
| Campo | Como preencher |
|-------|----------------|
| Código IBGE Município | Buscar em [IBGE](https://www.ibge.gov.br/explica/codigos-dos-municipios.php) (ex: São Paulo = 3550308) |
| UF | "SP" (apenas SP por enquanto) |

### Aba "NFC-e (Certificado + SEFAZ)"
| Campo | Como preencher |
|-------|----------------|
| Ambiente | **Homologação** (testes) ou **Produção** (real) |
| Certificado A1 | Caminho completo do `.pfx` (botão `...` para navegar) |
| Senha Certificado | Senha do PFX |
| CSC ID | Geralmente "1" |
| CSC Token | Cole o token gerado na SEFAZ-SP |
| Próximo nNF | "1" para começar do zero |
| Série | "1" (padrão para NFC-e) |

Clique **Salvar**.

## Passo 4 — Testar em Homologação

> ⚠ **Sempre testar primeiro em Homologação.** Notas emitidas valem zero, mas a SEFAZ valida tudo.

1. Garanta que **Ambiente = Homologação** está selecionado
2. Cadastre alguns produtos com NCM, CFOP, CST ICMS
3. Abra o caixa
4. Faça uma venda no PDV
5. Ao finalizar, escolha "Sim" para emitir NFC-e
6. Sistema:
   - Gera o XML
   - Assina com seu certificado
   - Envia à SEFAZ-SP
   - Recebe autorização → status "AUTORIZADA"
7. Aparece tela com chave de acesso + QR Code

### Códigos de retorno comuns
| cStat | Significado | Ação |
|-------|-------------|------|
| 100 | Autorizado o uso da NF-e | ✓ tudo certo |
| 110 | Uso denegado | Verificar dados do destinatário |
| 204 | Duplicidade de NF-e | Mesmo número já enviado (chave duplicada) |
| 215 | Falha no schema XML | Algum campo obrigatório errado |
| 225 | Falha na assinatura | Certificado inválido ou expirado |
| 539 | Duplicidade com chave de acesso diferente | Mesma série + número, diferente |
| 539+ | Vários | [Tabela completa SEFAZ](https://www.fazenda.sp.gov.br/) |

## Passo 5 — Migrar para Produção

Quando os testes em homologação estiverem OK:

1. Voltar em **Configurações → Dados da Empresa**
2. Mudar **Ambiente** para **Produção**
3. **IMPORTANTE**: o **CSC de produção** geralmente é diferente do de homologação. Gerar um novo no SAT-SP para produção.
4. Voltar **Próximo nNF** para "1" (nova numeração)
5. Salvar
6. Fazer **uma venda de teste** real (pode cancelar depois em 24h se der errado)

## Cancelamento de NFC-e

**Menu → Vendas → Notas Fiscais → selecionar nota → Cancelar Nota**

Regras:
- Apenas notas **autorizadas**
- Dentro de **24 horas** da autorização
- Justificativa obrigatória entre **15 e 255** caracteres

cStat de sucesso: 135, 136 (registrado fora do prazo) ou 155 (cancelamento extemporâneo)

## Inutilização de Numeração

Use quando há **quebra na sequência** de NFC-e (ex: tentou emitir n° 5, 6, 7 e deu erro, e os números não foram usados em nenhuma nota autorizada/cancelada).

**Menu → Vendas → Notas Fiscais → Inutilizar Faixa**
1. Série, número inicial, número final
2. Justificativa (≥ 15 chars)
3. Enviar — SEFAZ retorna cStat **102** se aceito

⚠ Após inutilização, esses números **nunca mais** poderão ser usados.

## Contingência (offline)

Se a SEFAZ estiver fora do ar e o PDV detectar:
1. Sistema pergunta "Emitir em CONTINGÊNCIA?"
2. Se "Sim":
   - Nota é salva com status **Contingência** e `tpEmis=9`
   - Cliente leva o cupom (DANFE em contingência tem indicação obrigatória)
3. Quando a SEFAZ voltar:
   - **Menu → Vendas → Notas Fiscais → Reenviar Contingência**
   - Sistema envia em lote todas as notas pendentes
   - Notas autorizadas mudam para status **Autorizada**

## Boas práticas

- **Backup do certificado** em pendrive seguro
- **Não compartilhe a senha do PFX** — quem tiver assina notas em seu nome
- **Aumente o estoque mínimo** antes de períodos de pico (Natal, Black Friday) — relatório ABC ajuda
- **Concilia diariamente** o caixa antes de fechar
- **Confira o XML autorizado** se houver dúvida — campo "XML Retorno" da nota guarda a resposta SEFAZ
- **Renovação do A1** — certificado A1 dura 1 ano; agende renovação 1 mês antes
- **Faça backup do banco diariamente** (já automático no app)

## Troubleshooting

| Sintoma | Causa provável |
|---------|----------------|
| "Certificado não contém chave privada" | Importou um `.cer` (público) em vez do `.pfx` (privado) |
| "Erro de comunicação" | Internet caiu OU SEFAZ-SP offline |
| "Rejeição 539" | Mesmo número usado antes com chave diferente — incrementar `Próx. nNF` |
| "Rejeição 215" + campo X | Campo X com formato errado (verifique NCM, CFOP, datas) |
| QR Code não aparece | CSC ID/Token errados |
| "AC ICPB - certificado expirado" | Renovar A1 |

## Status normativos

- **NFC-e** modelo 65, layout 4.00
- **Schema** baseado em PL_009r1 (versão atual SP)
- **Webservice síncrono** (resposta imediata, sem polling)
- **Apenas SP** por enquanto — para outros estados, alterar `SefazSpClient` para os endpoints correspondentes
