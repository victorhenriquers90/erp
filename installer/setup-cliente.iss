; =============================================================================
; ProjetoVarejo ERP — Instalador CLIENTE (Terminal / PDV)
; Instala nos terminais de caixa que se conectam ao servidor via rede local.
; Pergunta o IP do servidor durante a instalacao e grava a connection string.
; NAO cria banco de dados nem exibe wizard de configuracao de segmento.
; =============================================================================
; Compilar:
;   dotnet publish src/ProjetoVarejo.Desktop.Wpf -c Release -r win-x64 --self-contained false -o publish/desktop-wpf
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup-cliente.iss
; =============================================================================

#define MyAppName    "ProjetoVarejo"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sua Empresa Ltda"
#define MyAppURL     "https://example.com"
#define MyAppExeName "ProjetoVarejo.Desktop.Wpf.exe"
#define TipoInstalacao "Terminal"

[Setup]
AppId={{F1A2B3C4-D5E6-7890-1234-56789ABCDEF2}
AppName={#MyAppName} ({#TipoInstalacao})
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=output
OutputBaseFilename=ProjetoVarejo-Cliente-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na area de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "..\publish\desktop-wpf\*"; DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md";           DestDir: "{app}";         Flags: ignoreversion

[Dirs]
Name: "{app}\desktop\Backups";             Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}";      Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}\Logs"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}";                                Filename: "{app}\desktop\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}";         Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";                          Filename: "{app}\desktop\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\desktop\{#MyAppExeName}"; Description: "Abrir {#MyAppName} agora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\desktop\Backups"
Type: filesandordirs; Name: "{app}\desktop\backup.cfg"
Type: filesandordirs; Name: "{app}\desktop\backup.last"

[Code]
// ── Pagina customizada: dados de conexao com o servidor ──────────────────────
var
  PaginaServidor: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  PaginaServidor := CreateInputQueryPage(wpSelectDir,
    'Conexao com o Servidor',
    'Configure o servidor de banco de dados',
    'Informe os dados do servidor onde o ProjetoVarejo foi instalado como SERVIDOR:');

  PaginaServidor.Add('IP ou nome do servidor (ex: 192.168.1.10):', False);
  PaginaServidor.Add('Instancia SQL Server (padrao: SQLEXPRESS):', False);
  PaginaServidor.Values[0] := '';
  PaginaServidor.Values[1] := 'SQLEXPRESS';
end;

// ── Testa conexao SQL via PowerShell ─────────────────────────────────────────
// Retorna True se conseguiu abrir SqlConnection com timeout de 6 segundos.
// Usa System.Data.SqlClient (disponivel no PowerShell nativo do Windows).
function TestarConexaoSQL(ServerIP, Instancia: String): Boolean;
var
  DataSource, ConnStr, PsScript, PsCmd: String;
  ResCode: Integer;
begin
  if Trim(Instancia) = '' then Instancia := 'SQLEXPRESS';
  DataSource := ServerIP + '\' + Instancia;

  // Monta connection string
  ConnStr := 'Server=' + DataSource +
             ';Database=master;Trusted_Connection=True;' +
             'TrustServerCertificate=True;Encrypt=False;Connect Timeout=6';

  // Script PowerShell inline — usa aspas duplas internas escapadas
  PsScript :=
    'try {' +
    ' Add-Type -AssemblyName System.Data;' +
    ' $c = New-Object System.Data.SqlClient.SqlConnection(''' + ConnStr + ''');' +
    ' $c.Open();' +
    ' $c.Close();' +
    ' exit 0' +
    '} catch {' +
    ' exit 1' +
    '}';

  PsCmd := '/c powershell -NonInteractive -WindowStyle Hidden -Command "' + PsScript + '"';

  Exec(ExpandConstant('{cmd}'), PsCmd, '', SW_HIDE, ewWaitUntilTerminated, ResCode);
  Result := (ResCode = 0);
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ServerIP, Instancia: String;
begin
  Result := True;

  if CurPageID = PaginaServidor.ID then
  begin
    ServerIP  := Trim(PaginaServidor.Values[0]);
    Instancia := Trim(PaginaServidor.Values[1]);

    // Validacao: campo obrigatorio
    if ServerIP = '' then
    begin
      MsgBox('O IP ou nome do servidor e obrigatorio.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    // Teste real de conexao SQL
    WizardForm.NextButton.Enabled := False;
    WizardForm.NextButton.Caption := 'Testando...';

    if not TestarConexaoSQL(ServerIP, Instancia) then
    begin
      WizardForm.NextButton.Enabled := True;
      WizardForm.NextButton.Caption := 'Avan&car';

      if MsgBox(
        'Nao foi possivel conectar ao SQL Server em "' + ServerIP + '".' + #13#10 + #13#10 +
        'Verifique:' + #13#10 +
        '  - O servidor esta ligado e o SQL Server esta ativo' + #13#10 +
        '  - A instancia "' + Instancia + '" existe e aceita conexoes remotas' + #13#10 +
        '  - A porta 1433 esta aberta no firewall do servidor' + #13#10 +
        '  - O Servico "SQL Server Browser" esta ativo no servidor' + #13#10 + #13#10 +
        'Deseja tentar continuar mesmo sem conexao confirmada?',
        mbError, MB_YESNO) = IDNO then
      begin
        Result := False;
        Exit;
      end;
    end
    else
    begin
      WizardForm.NextButton.Enabled := True;
      WizardForm.NextButton.Caption := 'Avan&car';
    end;
  end;
end;

// ── Grava appsettings.json com a connection string do servidor ────────────────
procedure GravarConnectionString(ServerIP, Instancia: String);
var
  ConnStr, Conteudo, Arquivo: String;
begin
  if Trim(Instancia) = '' then Instancia := 'SQLEXPRESS';

  // Barra invertida deve ser escapada como \\ dentro do JSON
  ConnStr := 'Server=' + ServerIP + '\\' + Instancia +
             ';Database=ProjetoVarejo;Trusted_Connection=True;' +
             'TrustServerCertificate=True;Encrypt=False;';

  Conteudo :=
    '{' + #13#10 +
    '  "ConnectionStrings": {' + #13#10 +
    '    "Default": "' + ConnStr + '"' + #13#10 +
    '  },' + #13#10 +
    '  "Logging": {' + #13#10 +
    '    "LogLevel": {' + #13#10 +
    '      "Default": "Warning",' + #13#10 +
    '      "Microsoft": "Error",' + #13#10 +
    '      "System": "Error"' + #13#10 +
    '    }' + #13#10 +
    '  }' + #13#10 +
    '}';

  // Sobrescreve appsettings.json base — lido independente do ambiente
  Arquivo := ExpandConstant('{app}\desktop\appsettings.json');
  DeleteFile(Arquivo);
  SaveStringToFile(Arquivo, Conteudo, False);

  // Tambem grava no Production para garantir que sobrepoe o Development
  Arquivo := ExpandConstant('{app}\desktop\appsettings.Production.json');
  DeleteFile(Arquivo);
  SaveStringToFile(Arquivo, Conteudo, False);
end;

function NetRuntimeAusente: Boolean;
var
  ResCode: Integer;
begin
  Exec(ExpandConstant('{cmd}'), '/c dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 8."',
    '', SW_HIDE, ewWaitUntilTerminated, ResCode);
  Result := ResCode <> 0;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Grava connection string com IP informado pelo instalador
    GravarConnectionString(
      Trim(PaginaServidor.Values[0]),
      Trim(PaginaServidor.Values[1]));

    if NetRuntimeAusente() then
    begin
      if MsgBox('O .NET 8 Desktop Runtime nao foi detectado.' + #13#10 +
                'Deseja abrir a pagina de download agora?',
                mbConfirmation, MB_YESNO) = IDYES then
        ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
    end;
  end;
end;
