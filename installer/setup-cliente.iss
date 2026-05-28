; =============================================================================
; ProjetoVarejo ERP — Instalador CLIENTE (Terminal / PDV)
; Instala nos terminais de caixa que se conectam ao servidor via rede local.
; Pergunta o IP do servidor durante a instalacao e grava a connection string.
; NAO cria banco de dados nem exibe wizard de configuracao de segmento.
; =============================================================================
; Compilar:
;   dotnet publish src/ProjetoVarejo.Desktop -c Release -r win-x64 --self-contained false -o publish/desktop
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup-cliente.iss
; =============================================================================

#define MyAppName    "ProjetoVarejo"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sua Empresa Ltda"
#define MyAppURL     "https://example.com"
#define MyAppExeName "ProjetoVarejo.Desktop.exe"
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
Source: "..\publish\desktop\*"; DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs
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

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = PaginaServidor.ID then
  begin
    if Trim(PaginaServidor.Values[0]) = '' then
    begin
      MsgBox('O IP ou nome do servidor e obrigatorio.', mbError, MB_OK);
      Result := False;
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
