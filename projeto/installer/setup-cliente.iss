; Inno Setup — Instalador CLIENTE
; Inclui: apenas WPF ERP (gestão) ou PDV — conecta via API ao servidor
; Antes: powershell -ExecutionPolicy Bypass -File installer\publish.ps1

#define MyAppName "Projeto ERP — Cliente"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Sua Empresa Ltda"
#define MyAppExeWpf "ProjetoVarejo.Desktop.Wpf.exe"
#define MyAppExePdv "ProjetoVarejo.Desktop.exe"

[Setup]
AppId={{F1A2B3C4-D5E6-7890-1234-CLIENT-0002}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\ProjetoERP
DefaultGroupName=Projeto ERP
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=ProjetoERP-Cliente-{#MyAppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "pdv";         Description: "Instalar também o PDV (frente de caixa)"; GroupDescription: "Componentes:"; Flags: unchecked

[Files]
; ERP WPF — sempre incluído
Source: "..\publish\wpf\*"; DestDir: "{app}\wpf"; Flags: ignoreversion recursesubdirs createallsubdirs
; PDV WinForms — opcional
Source: "..\publish\desktop\*"; DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs; Tasks: pdv

[Icons]
Name: "{group}\Projeto ERP";                       Filename: "{app}\wpf\{#MyAppExeWpf}"
Name: "{group}\PDV (Caixa)";                       Filename: "{app}\desktop\{#MyAppExePdv}"; Tasks: pdv
Name: "{group}\{cm:UninstallProgram,Projeto ERP}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Projeto ERP";                 Filename: "{app}\wpf\{#MyAppExeWpf}"; Tasks: desktopicon

[Run]
Filename: "{app}\wpf\{#MyAppExeWpf}"; Description: "Abrir Projeto ERP agora (configure o servidor no primeiro uso)"; \
  Flags: nowait postinstall skipifsilent

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
