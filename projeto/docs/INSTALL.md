# Instalação — ProjetoVarejo

Guia passo a passo para instalar o sistema em uma loja.

## 1. Requisitos

### Servidor (PC central da loja)
- Windows 10 ou 11 (64 bits)
- 4 GB RAM mínimo
- 20 GB livres em disco
- .NET 8 Desktop Runtime ([download](https://dotnet.microsoft.com/download/dotnet/8.0))
- SQL Server (uma das opções abaixo)

### Banco de dados — escolha uma opção
| Opção | Quando usar | Como instalar |
|-------|-------------|---------------|
| **SQL Server LocalDB** | 1 caixa apenas, sem rede | Já vem com Visual Studio ou via [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) → marcar "LocalDB" |
| **SQL Server Express** | Até 3-4 caixas, banco até 10 GB | [Download](https://www.microsoft.com/sql-server/sql-server-downloads) — escolher "Express" |
| **SQL Server Standard/Enterprise** | Loja média/grande, multi-loja, multi-empresa | Licença paga, instalação corporativa |

### Caixas (PCs adicionais, opcional)
- Windows 10 ou 11
- .NET 8 Desktop Runtime
- Rede local com o servidor

## 2. Instalação do banco

### Opção A — LocalDB (mais simples)
```powershell
# Verifica se já está instalado
sqllocaldb info

# Se não, baixar SQL Server Express e marcar LocalDB durante instalação
# Iniciar a instância
sqllocaldb start MSSQLLocalDB
```
Connection string em `appsettings.json`:
```
Server=(localdb)\MSSQLLocalDB;Database=ProjetoVarejo;Trusted_Connection=True;TrustServerCertificate=True;
```

### Opção B — SQL Server Express com rede
1. Instalar SQL Server Express com modo de autenticação **Mixed** (SQL Server e Windows)
2. Definir senha do `sa` durante instalação
3. Abrir **SQL Server Configuration Manager**:
   - Habilitar protocolo **TCP/IP**
   - Habilitar **Named Pipes**
   - Reiniciar serviço SQL Server
4. Liberar porta **1433** no firewall do servidor
5. Connection string em `appsettings.json`:
   ```
   Server=NOME-DO-SERVIDOR\SQLEXPRESS;Database=ProjetoVarejo;User Id=sa;Password=SUA_SENHA;TrustServerCertificate=True;
   ```

## 3. Instalação do aplicativo

### Modo automatizado (instalador)
1. Executar `ProjetoVarejo-Setup-x.y.z.exe` (Inno Setup gerado)
2. Aceitar caminho padrão `C:\ProjetoVarejo`
3. Marcar criar atalho na área de trabalho
4. Concluir

### Modo manual
1. Copiar pasta `publish/` para `C:\ProjetoVarejo`
2. Editar `appsettings.json` com a connection string correta
3. Criar atalho em `ProjetoVarejo.Desktop.exe`

## 4. Primeira execução

1. Abrir o aplicativo
2. Login com:
   - Usuário: `admin`
   - Senha: `admin`
3. **Trocar a senha imediatamente** (Cadastros → Usuários — disponível em versões futuras; via SQL por enquanto)
4. Ir em **Configurações → Dados da Empresa**:
   - Preencher CNPJ, IE, endereço
   - Configurar certificado A1 e CSC (ver [NFCE_SETUP.md](NFCE_SETUP.md))

## 5. Multi-caixa (rede local)

Em cada caixa (PC cliente):
1. Instalar .NET 8 Desktop Runtime
2. Copiar a pasta `publish/`
3. Editar `appsettings.json` apontando para o servidor:
   ```
   Server=NOME-SERVIDOR\SQLEXPRESS;Database=ProjetoVarejo;User Id=usuario;Password=senha;TrustServerCertificate=True;
   ```
4. Testar conexão abrindo o app

## 6. Backup

Configurar em **Menu → Configurações → Backup do Banco**:
- Pasta de destino (recomendado: drive externo ou pasta de rede)
- Marcar "backup automático ao iniciar"
- Verifica antiguidade: só faz backup se último foi em outro dia

Backups antigos são removidos automaticamente após 20 arquivos.

## 7. Atualização

Para atualizar para nova versão:
1. Fazer backup do banco
2. Fechar o aplicativo em todos os caixas
3. Executar novo instalador OU substituir os arquivos da pasta
4. Migrations rodam automaticamente na primeira abertura

## 8. Desinstalação

1. Painel de Controle → Programas → Desinstalar
2. Banco de dados NÃO é removido automaticamente. Para remover:
   ```sql
   DROP DATABASE ProjetoVarejo;
   ```

## Solução de problemas

| Erro | Causa | Solução |
|------|-------|---------|
| "Servidor não foi encontrado" | SQL Server parado / TCP-IP desabilitado | Iniciar serviço, verificar Configuration Manager |
| "Login failed for user 'sa'" | Senha errada na connection string | Editar `appsettings.json` |
| App fecha sozinho ao abrir | Migration falhou | Ver log no Event Viewer Windows, ou reinstalar |
| Impressora não imprime | Driver não instalado / fora da rede | Testar via Configurações → Testar Impressão |
