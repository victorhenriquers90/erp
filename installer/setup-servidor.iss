; =============================================================================
; ProjetoVarejo ERP — Instalador SERVIDOR
; Instala na maquina que hospeda o SQL Server (maquina principal / retaguarda).
; Cria o banco de dados, configura permissoes e exibe o wizard de primeiro uso.
; =============================================================================
; Compilar:
;   dotnet publish src/ProjetoVarejo.Desktop -c Release -r win-x64 --self-contained false -o publish/desktop
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup-servidor.iss
; =============================================================================

#define MyAppName    "ProjetoVarejo"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sua Empresa Ltda"
#define MyAppURL     "https://example.com"
#define MyAppExeName "ProjetoVarejo.Desktop.exe"
#define TipoInstalacao "Servidor"

[Setup]
AppId={{F1A2B3C4-D5E6-7890-1234-56789ABCDEF1}
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
OutputBaseFilename=ProjetoVarejo-Servidor-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na area de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "firewall";   Description: "Abrir porta 1433 no Firewall do Windows (necessario para os terminais clientes se conectarem)"; GroupDescription: "Rede local:"

[Files]
Source: "..\publish\desktop\*"; DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md";           DestDir: "{app}";         Flags: ignoreversion
Source: "..\docs\INSTALL.md";     DestDir: "{app}\docs";    Flags: ignoreversion
Source: "..\docs\USER_MANUAL.md"; DestDir: "{app}\docs";    Flags: ignoreversion
Source: "..\docs\NFCE_SETUP.md";  DestDir: "{app}\docs";    Flags: ignoreversion

[Dirs]
Name: "{app}\desktop\Backups";            Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}";     Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}\Logs"; Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}";                                    Filename: "{app}\desktop\{#MyAppExeName}"
Name: "{group}\Manual do Usuario";                               Filename: "{app}\docs\USER_MANUAL.md"
Name: "{group}\Setup NFC-e";                                     Filename: "{app}\docs\NFCE_SETUP.md"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}";             Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";                              Filename: "{app}\desktop\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Abre porta 1433 para terminais clientes (opcional)
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""ProjetoVarejo - SQL Server"" protocol=TCP dir=in localport=1433 action=allow"; Tasks: firewall; Flags: runhidden; StatusMsg: "Configurando firewall para acesso da rede..."
; Inicia o app apos instalacao (firstrun.flag ja foi criado)
Filename: "{app}\desktop\{#MyAppExeName}"; Description: "Abrir {#MyAppName} agora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\desktop\Backups"
Type: filesandordirs; Name: "{app}\desktop\backup.cfg"
Type: filesandordirs; Name: "{app}\desktop\backup.last"
Type: files;          Name: "{app}\desktop\firstrun.flag"

[UninstallRun]
; Remove regra de firewall ao desinstalar
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""ProjetoVarejo - SQL Server"""; Flags: runhidden

[Code]
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
  FlagFile: String;
begin
  if CurStep = ssPostInstall then
  begin
    // Sentinela de primeiro uso — o app exibe o wizard de segmento na primeira abertura
    FlagFile := ExpandConstant('{app}\desktop\firstrun.flag');
    SaveStringToFile(FlagFile, '', False);

    if NetRuntimeAusente() then
    begin
      if MsgBox('O .NET 8 Desktop Runtime nao foi detectado.' + #13#10 +
                'Deseja abrir a pagina de download agora?',
                mbConfirmation, MB_YESNO) = IDYES then
        ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
    end;

    // Lembrete: habilitar TCP/IP no SQL Server para que os clientes se conectem
    MsgBox(
      'Instalacao do SERVIDOR concluida!' + #13#10 + #13#10 +
      'Para que os terminais clientes se conectem a este servidor:' + #13#10 +
      '  1. Abra o SQL Server Configuration Manager' + #13#10 +
      '  2. Em "Configuracao de Rede do SQL Server" > "Protocolos para SQLEXPRESS"' + #13#10 +
      '  3. Habilite o protocolo TCP/IP' + #13#10 +
      '  4. Reinicie o servico SQL Server' + #13#10 + #13#10 +
      'IP deste servidor (para configurar nos clientes):' + #13#10 +
      'Verifique em: Configuracoes > Rede > Propriedades do adaptador',
      mbInformation, MB_OK);
  end;
end;
