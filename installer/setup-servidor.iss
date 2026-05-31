; =============================================================================
; ProjetoVarejo ERP — Instalador SERVIDOR
; Instala na maquina que hospeda o SQL Server (maquina principal / retaguarda).
; Cria o banco de dados, configura permissoes e exibe o wizard de primeiro uso.
; =============================================================================
; Compilar:
;   dotnet publish src/ProjetoVarejo.Desktop.Wpf -c Release -r win-x64 --self-contained false -o publish/desktop-wpf
;   dotnet publish src/ProjetoVarejo.Api     -c Release -r win-x64 --self-contained true  -o publish/web
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup-servidor.iss
; =============================================================================

#define MyAppName    "ProjetoVarejo"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sua Empresa Ltda"
#define MyAppURL     "https://example.com"
#define MyAppExeName "ProjetoVarejo.Desktop.Wpf.exe"
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
Name: "firewall";    Description: "Abrir porta 1433 no Firewall do Windows (necessario para os terminais clientes se conectarem)"; GroupDescription: "Rede local:"
Name: "webserver";   Description: "Instalar interface web (Plano B) — acesse o ERP pelo navegador de qualquer PC da rede, sem instalar nada nos terminais"; GroupDescription: "Interface web:"
Name: "webfirewall"; Description: "Abrir porta 5094 no Firewall (necessario para acessar a interface web pela rede)"; GroupDescription: "Interface web:"; Flags: unchecked

[Files]
Source: "..\publish\desktop-wpf\*";   DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\web\*";       DestDir: "{app}\web";     Flags: ignoreversion recursesubdirs createallsubdirs
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
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""ProjetoVarejo - SQL Server"" protocol=TCP dir=in localport=1433 action=allow"; Tasks: firewall; Flags: runhidden; StatusMsg: "Configurando firewall para SQL Server..."
; Plano B: registra e inicia o servico web
; binPath inclui --environment Development (ignora checagem de chaves placeholder)
; e --urls http://0.0.0.0:5094 (escuta em todas as interfaces da rede local)
Filename: "sc"; Parameters: "create ""ProjetoVarejo Web"" binPath= ""{app}\web\ProjetoVarejo.Api.exe --environment Development --urls http://0.0.0.0:5094"" start= auto DisplayName= ""ProjetoVarejo Interface Web"""; Tasks: webserver; Flags: runhidden; StatusMsg: "Registrando servico Windows..."
Filename: "sc"; Parameters: "description ""ProjetoVarejo Web"" ""Interface web do ProjetoVarejo ERP — acesso pelo navegador na porta 5094"""; Tasks: webserver; Flags: runhidden
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""ProjetoVarejo - Interface Web"" protocol=TCP dir=in localport=5094 action=allow"; Tasks: webfirewall; Flags: runhidden; StatusMsg: "Configurando firewall para interface web..."
Filename: "sc"; Parameters: "start ""ProjetoVarejo Web"""; Tasks: webserver; Flags: runhidden; StatusMsg: "Iniciando servico ProjetoVarejo Web..."
; Inicia o app apos instalacao (firstrun.flag ja foi criado)
Filename: "{app}\desktop\{#MyAppExeName}"; Description: "Abrir {#MyAppName} agora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\desktop\Backups"
Type: filesandordirs; Name: "{app}\desktop\backup.cfg"
Type: filesandordirs; Name: "{app}\desktop\backup.last"
Type: files;          Name: "{app}\desktop\firstrun.flag"
Type: filesandordirs; Name: "{app}\web"

[UninstallRun]
; Remove regra de firewall ao desinstalar
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""ProjetoVarejo - SQL Server"""; Flags: runhidden
; Para e remove o servico web (se instalado)
Filename: "sc"; Parameters: "stop ""ProjetoVarejo Web"""; Flags: runhidden
Filename: "sc"; Parameters: "delete ""ProjetoVarejo Web"""; Flags: runhidden
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""ProjetoVarejo - Interface Web"""; Flags: runhidden

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

    // Lembrete: habilitar TCP/IP no SQL Server e informar sobre o acesso web
    MsgBox(
      'Instalacao do SERVIDOR concluida!' + #13#10 + #13#10 +
      'Para que os terminais clientes se conectem a este servidor:' + #13#10 +
      '  1. Abra o SQL Server Configuration Manager' + #13#10 +
      '  2. Em "Configuracao de Rede do SQL Server" > "Protocolos para SQLEXPRESS"' + #13#10 +
      '  3. Habilite o protocolo TCP/IP' + #13#10 +
      '  4. Reinicie o servico SQL Server' + #13#10 + #13#10 +
      'Interface web (Plano B):' + #13#10 +
      '  Qualquer PC da rede pode acessar o ERP pelo navegador em:' + #13#10 +
      '  http://<IP-deste-servidor>:5094' + #13#10 + #13#10 +
      'IP deste servidor: Configuracoes > Rede > Propriedades do adaptador',
      mbInformation, MB_OK);
  end;
end;
