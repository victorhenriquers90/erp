; Inno Setup Script para ProjetoVarejo ERP
; Compilar com Inno Setup 6+ (download: https://jrsoftware.org/isinfo.php)
; Antes de compilar, rode: powershell -ExecutionPolicy Bypass -File installer\publish.ps1

#define MyAppName "Projeto ERP"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sua Empresa Ltda"
#define MyAppURL "https://example.com"
#define MyAppExeWpf "ProjetoVarejo.Desktop.Wpf.exe"
#define MyAppExePdv "ProjetoVarejo.Desktop.exe"

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
OutputBaseFilename=ProjetoERP-Setup-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon";  Description: "Criar atalho na área de trabalho";             GroupDescription: "Atalhos adicionais:"; Flags: unchecked
Name: "instalarpdv";  Description: "Instalar PDV (frente de caixa WinForms)";      GroupDescription: "Componentes opcionais:"; Flags: unchecked
Name: "instalarapi";  Description: "Instalar API REST (apps mobile / integrações)"; GroupDescription: "Componentes opcionais:"; Flags: unchecked

[Files]
; ── Aplicação principal WPF (ERP / gestão)
Source: "..\publish\wpf\*"; DestDir: "{app}\wpf"; Flags: ignoreversion recursesubdirs createallsubdirs

; ── PDV WinForms (opcional)
Source: "..\publish\desktop\*"; DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: instalarpdv

; ── API REST (opcional)
Source: "..\publish\api\*"; DestDir: "{app}\api"; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: instalarapi

; ── Documentação
Source: "..\README.md";           DestDir: "{app}";        Flags: ignoreversion
Source: "..\docs\INSTALL.md";     DestDir: "{app}\docs";   Flags: ignoreversion
Source: "..\docs\USER_MANUAL.md"; DestDir: "{app}\docs";   Flags: ignoreversion
Source: "..\docs\NFCE_SETUP.md";  DestDir: "{app}\docs";   Flags: ignoreversion

[Dirs]
Name: "{app}\wpf\Backups";                         Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}\Logs";          Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}\Certificados";  Permissions: users-modify

[Icons]
; ERP WPF — ícone principal
Name: "{group}\{#MyAppName}";                        Filename: "{app}\wpf\{#MyAppExeWpf}"
Name: "{autodesktop}\{#MyAppName}";                  Filename: "{app}\wpf\{#MyAppExeWpf}"; Tasks: desktopicon

; PDV (se instalado)
Name: "{group}\{#MyAppName} — PDV (Caixa)";          Filename: "{app}\desktop\{#MyAppExePdv}"; Tasks: instalarpdv

; Documentação
Name: "{group}\Manual do Usuário";                   Filename: "{app}\docs\USER_MANUAL.md"
Name: "{group}\Configurar NFC-e";                    Filename: "{app}\docs\NFCE_SETUP.md"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}";  Filename: "{uninstallexe}"

[Run]
Filename: "{app}\wpf\{#MyAppExeWpf}"; Description: "Abrir {#MyAppName} agora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\wpf\Backups"
Type: filesandordirs; Name: "{app}\wpf\backup.cfg"
Type: filesandordirs; Name: "{app}\wpf\backup.last"

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
begin
  if CurStep = ssPostInstall then
  begin
    if NetRuntimeAusente() then
    begin
      if MsgBox('O .NET 8 Desktop Runtime é necessário e não foi detectado.' + #13#10 +
                'Deseja abrir a página de download agora?',
                mbConfirmation, MB_YESNO) = IDYES then
        ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
    end;
  end;
end;
