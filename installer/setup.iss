; Inno Setup Script para ProjetoVarejo
; Compilar com Inno Setup 6+ (download: https://jrsoftware.org/isinfo.php)
; Antes de compilar:
;   dotnet publish src/ProjetoVarejo.Desktop -c Release -r win-x64 --self-contained false -o publish/desktop
;   dotnet publish src/ProjetoVarejo.Api     -c Release -r win-x64 --self-contained false -o publish/api

#define MyAppName "ProjetoVarejo"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sua Empresa Ltda"
#define MyAppURL "https://example.com"
#define MyAppExeName "ProjetoVarejo.Desktop.exe"

[Setup]
AppId={{F1A2B3C4-D5E6-7890-1234-56789ABCDEF0}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=output
OutputBaseFilename=ProjetoVarejo-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos adicionais:"; Flags: unchecked
Name: "instalarapi";  Description: "Instalar API REST como serviço (para apps mobile)"; GroupDescription: "Componentes opcionais:"; Flags: unchecked

[Files]
; Aplicação Desktop (PDV)
Source: "..\publish\desktop\*"; DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs
; API REST (opcional)
Source: "..\publish\api\*";     DestDir: "{app}\api";     Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: instalarapi
; Documentação
Source: "..\README.md";          DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\INSTALL.md";    DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\docs\USER_MANUAL.md"; DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\docs\NFCE_SETUP.md"; DestDir: "{app}\docs"; Flags: ignoreversion

[Dirs]
Name: "{app}\desktop\Backups";                     Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}";              Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}\Logs";         Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}";         Filename: "{app}\desktop\{#MyAppExeName}"
Name: "{group}\Manual do Usuário";   Filename: "{app}\docs\USER_MANUAL.md"
Name: "{group}\Setup NFC-e";          Filename: "{app}\docs\NFCE_SETUP.md"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";   Filename: "{app}\desktop\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Verificar/instalar .NET 8 Desktop Runtime se não houver
Filename: "{cmd}"; Parameters: "/c dotnet --list-runtimes | findstr ""Microsoft.WindowsDesktop.App 8."""; Flags: runhidden; StatusMsg: "Verificando .NET 8 Desktop Runtime..."; Check: NetRuntimeAusente
; Inicia o app após instalação (o arquivo firstrun.flag já foi criado pelo instalador)
Filename: "{app}\desktop\{#MyAppExeName}"; Description: "Abrir {#MyAppName} agora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\desktop\Backups"
Type: filesandordirs; Name: "{app}\desktop\backup.cfg"
Type: filesandordirs; Name: "{app}\desktop\backup.last"
Type: files;          Name: "{app}\desktop\firstrun.flag"

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
    // Criar arquivo sentinela — o app detecta na primeira abertura e exibe o
    // wizard de configuração de segmento, independente de como for aberto
    FlagFile := ExpandConstant('{app}\desktop\firstrun.flag');
    SaveStringToFile(FlagFile, '', False);

    if NetRuntimeAusente() then
    begin
      if MsgBox('O .NET 8 Desktop Runtime é necessário e não foi detectado.' + #13#10 +
                'Deseja abrir a página de download agora?',
                mbConfirmation, MB_YESNO) = IDYES then
        ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
    end;
  end;
end;
