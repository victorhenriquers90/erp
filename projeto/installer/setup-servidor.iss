; Inno Setup — Instalador SERVIDOR
; Inclui: WPF ERP, PDV, API registrada como serviço Windows
; Antes: powershell -ExecutionPolicy Bypass -File installer\publish.ps1

#define MyAppName "Projeto ERP — Servidor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sua Empresa Ltda"
#define MyAppExeWpf "ProjetoVarejo.Desktop.Wpf.exe"
#define MyAppExePdv "ProjetoVarejo.Desktop.exe"
#define MyAppExeApi "ProjetoVarejo.Api.exe"

[Setup]
AppId={{F1A2B3C4-D5E6-7890-1234-SERVER-0001}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ProjetoERP
DefaultGroupName=Projeto ERP
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=output
OutputBaseFilename=ProjetoERP-Servidor-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "autostart";   Description: "Iniciar API automaticamente com o Windows (recomendado)"; GroupDescription: "Serviço:"; Flags: checkedonce

[Files]
; ERP WPF (gestão)
Source: "..\publish\wpf\*";     DestDir: "{app}\wpf";     Flags: ignoreversion recursesubdirs createallsubdirs
; PDV WinForms
Source: "..\publish\desktop\*"; DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs
; API REST
Source: "..\publish\api\*";     DestDir: "{app}\api";     Flags: ignoreversion recursesubdirs createallsubdirs
; Documentação
Source: "..\docs\INSTALL.md";     DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\docs\NFCE_SETUP.md";  DestDir: "{app}\docs"; Flags: ignoreversion
Source: "..\docs\USER_MANUAL.md"; DestDir: "{app}\docs"; Flags: ignoreversion

[Dirs]
Name: "{app}\wpf\Backups";                        Permissions: users-modify
Name: "{commonappdata}\ProjetoERP\Logs";           Permissions: users-modify
Name: "{commonappdata}\ProjetoERP\Certificados";   Permissions: users-modify

[Icons]
Name: "{group}\Projeto ERP (Gestão)";              Filename: "{app}\wpf\{#MyAppExeWpf}"
Name: "{group}\Projeto ERP (PDV — Caixa)";         Filename: "{app}\desktop\{#MyAppExePdv}"
Name: "{group}\Manual do Usuário";                 Filename: "{app}\docs\USER_MANUAL.md"
Name: "{group}\{cm:UninstallProgram,Projeto ERP}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Projeto ERP";                 Filename: "{app}\wpf\{#MyAppExeWpf}"; Tasks: desktopicon

[Run]
; Registrar API como serviço Windows
Filename: "sc"; Parameters: "create ProjetoERP-Api binPath= ""{app}\api\{#MyAppExeApi}"" start= auto DisplayName= ""Projeto ERP — API"""; \
  Flags: runhidden; Tasks: autostart; StatusMsg: "Registrando serviço da API..."
Filename: "sc"; Parameters: "start ProjetoERP-Api"; Flags: runhidden; Tasks: autostart; StatusMsg: "Iniciando API..."
; Abrir ERP após instalação
Filename: "{app}\wpf\{#MyAppExeWpf}"; Description: "Abrir Projeto ERP agora"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "sc"; Parameters: "stop ProjetoERP-Api";   Flags: runhidden
Filename: "sc"; Parameters: "delete ProjetoERP-Api"; Flags: runhidden

[UninstallDelete]
Type: filesandordirs; Name: "{app}\wpf\Backups"

[Code]
function NetRuntimeAusente: Boolean;
var ResCode: Integer;
begin
  Exec(ExpandConstant('{cmd}'), '/c dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 8."',
    '', SW_HIDE, ewWaitUntilTerminated, ResCode);
  Result := ResCode <> 0;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
    if NetRuntimeAusente() then
      if MsgBox('O .NET 8 Desktop Runtime não foi detectado.' + #13#10 +
                'Deseja abrir a página de download?', mbConfirmation, MB_YESNO) = IDYES then
        ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
end;
