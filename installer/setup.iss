; =============================================================================
; ProjetoVarejo ERP — Instalador Unificado
; Substitui setup-servidor.iss e setup-cliente.iss.
; Um único arquivo que pergunta o tipo de instalação e configura tudo.
;
; Compilar:
;   dotnet publish src/ProjetoVarejo.Api -c Release -r win-x64 --self-contained true -o publish/web
;   dotnet publish src/ProjetoVarejo.Desktop.Wpf -c Release -r win-x64 --self-contained false -o publish/desktop-wpf
;   "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup.iss
; =============================================================================

#define MyAppName      "ProjetoVarejo"
#define MyAppVersion   "1.0.0"
#define MyAppPublisher "Sua Empresa Ltda"
#define MyAppURL       "https://example.com"
#define MyAppExeName   "ProjetoVarejo.Desktop.Wpf.exe"

[Setup]
AppId={{F1A2B3C4-D5E6-7890-1234-56789ABCDEF0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=admin
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
; Atalho — aparece para todos
Name: "desktopicon"; Description: "Criar atalho na area de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
; Opções de servidor/retaguarda — visíveis apenas quando pertinente (Check= filtra)
Name: "firewall";    Description: "Abrir porta 1433 no Firewall (para terminais clientes se conectarem)";           GroupDescription: "Rede local:";     Check: EhServidorOuMatrizCheck
Name: "webserver";   Description: "Instalar interface web — acesse pelo navegador de qualquer PC da rede (porta 5094)"; GroupDescription: "Interface web:"; Check: WebServerDisponivel
Name: "webfirewall"; Description: "Abrir porta 5094 no Firewall (necessario para acesso web pela rede)";            GroupDescription: "Interface web:"; Check: WebServerDisponivel; Flags: unchecked

[Files]
; Desktop WPF moderno: aplicacao principal.
; Rode antes: dotnet publish src/ProjetoVarejo.Desktop.Wpf -c Release -r win-x64 --self-contained false -o publish/desktop-wpf
#if DirExists("..\publish\desktop-wpf")
Source: "..\publish\desktop-wpf\*"; DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs
#endif

; Desktop/WinForms legado: não empacotar no fluxo principal.
; Source: "..\publish\desktop\*"; DestDir: "{app}\desktop"; Flags: ignoreversion recursesubdirs createallsubdirs

; Web/API — aplicação principal.
; Rode antes: dotnet publish src/ProjetoVarejo.Api -c Release -r win-x64 --self-contained true -o publish/web
#if DirExists("..\publish\web")
Source: "..\publish\web\*"; DestDir: "{app}\web"; Flags: ignoreversion recursesubdirs createallsubdirs
#endif

; Docs — ignorados se não existirem
Source: "..\README.md";           DestDir: "{app}";      Flags: ignoreversion skipifsourcedoesntexist
Source: "..\docs\INSTALL.md";     DestDir: "{app}\docs"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\docs\USER_MANUAL.md"; DestDir: "{app}\docs"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\docs\NFCE_SETUP.md";  DestDir: "{app}\docs"; Flags: ignoreversion skipifsourcedoesntexist

[Dirs]
Name: "{commonappdata}\{#MyAppName}";      Permissions: users-modify
Name: "{commonappdata}\{#MyAppName}\Logs"; Permissions: users-modify
Name: "{app}\desktop";                     Permissions: users-modify
Name: "{app}\web\logs";                    Permissions: users-modify

[Icons]
Name: "{group}\{#MyAppName}";                             Filename: "{app}\desktop\{#MyAppExeName}"
Name: "{group}\Manual do Usuario";                        Filename: "{app}\docs\USER_MANUAL.md";  Check: ArquivoExiste('{app}\docs\USER_MANUAL.md')
Name: "{group}\Setup NFC-e";                              Filename: "{app}\docs\NFCE_SETUP.md";   Check: ArquivoExiste('{app}\docs\NFCE_SETUP.md')
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}";       Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";                       Filename: "{app}\desktop\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Firewall SQL Server (modo servidor / retaguarda)
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""ProjetoVarejo - SQL Server"" protocol=TCP dir=in localport=1433 action=allow"; Tasks: firewall;    Flags: runhidden; StatusMsg: "Configurando firewall para SQL Server..."
; Interface web — criação do serviço é feita via [Code] para evitar problema
; de escape de aspas no binPath= quando o caminho contém espaços.
; Veja: procedure RegistrarServico() mais abaixo.
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""ProjetoVarejo - Interface Web"" protocol=TCP dir=in localport=5094 action=allow"; Tasks: webfirewall; Flags: runhidden; StatusMsg: "Configurando firewall para interface web..."
; Abre o app após instalação
Filename: "{app}\desktop\{#MyAppExeName}"; Description: "Abrir {#MyAppName} agora"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\desktop"
Type: filesandordirs; Name: "{app}\web"

[UninstallRun]
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""ProjetoVarejo - SQL Server""";   Flags: runhidden
Filename: "netsh"; Parameters: "advfirewall firewall delete rule name=""ProjetoVarejo - Interface Web"""; Flags: runhidden
Filename: "sc";    Parameters: "stop ""ProjetoVarejo Web""";   Flags: runhidden
Filename: "sc";    Parameters: "delete ""ProjetoVarejo Web"""; Flags: runhidden

; =============================================================================
[Code]
// ── Constantes de tipo ────────────────────────────────────────────────────────
const
  TIPO_UNICA      = 0;   // Maquina unica (mais comum — pequenos estabelecimentos)
  TIPO_RETAGUARDA = 1;   // Retaguarda / servidor (hospeda banco para terminais)
  TIPO_TERMINAL   = 2;   // Terminal de venda (conecta a retaguarda existente)

// ── Variáveis globais das páginas ─────────────────────────────────────────────
var
  PaginaTipo:      TInputOptionWizardPage;
  PaginaServidor:  TInputQueryWizardPage;
  PaginaWebInfo:   TOutputMsgWizardPage;

// ── Helpers de tipo ───────────────────────────────────────────────────────────
function TipoSelecionado: Integer;
begin
  Result := PaginaTipo.SelectedValueIndex;
end;

function EhTerminal: Boolean;
begin
  Result := TipoSelecionado = TIPO_TERMINAL;
end;

function EhServidorOuMatriz: Boolean;
begin
  Result := TipoSelecionado <> TIPO_TERMINAL;
end;

// Versões com assinatura compatível com Check= em [Tasks] e [Icons]
function EhServidorOuMatrizCheck: Boolean;
begin
  Result := EhServidorOuMatriz;
end;

// Task de interface web só aparece se: é servidor/matriz E publish\web foi compilado
// (verificado em tempo de instalação pelo caminho relativo ao instalador)
function WebServerDisponivel: Boolean;
begin
  Result := EhServidorOuMatriz;
end;

function ArquivoExiste(Caminho: String): Boolean;
begin
  Result := FileExists(ExpandConstant(Caminho));
end;

// ── Criação das páginas customizadas ─────────────────────────────────────────
procedure InitializeWizard;
begin
  // Página 1: Tipo de instalação (aparece logo depois da boas-vindas)
  PaginaTipo := CreateInputOptionPage(wpWelcome,
    'Tipo de instalação',
    'Como este computador será usado?',
    'Escolha o perfil que melhor descreve este computador:',
    True,   // exclusivo (radio buttons)
    False); // sem scroll

  PaginaTipo.Add(
    'Estabelecimento com uma única máquina' + #13#10 +
    '    Tudo neste computador: banco de dados, caixa e gestão.' + #13#10 +
    '    Ideal para pequenos comércios, quiosques e salões.');

  PaginaTipo.Add(
    'Retaguarda / servidor' + #13#10 +
    '    Esta máquina hospeda o banco de dados para outros terminais da rede.' + #13#10 +
    '    Instale primeiro no servidor; depois instale os terminais.');

  PaginaTipo.Add(
    'Terminal de venda (caixa)' + #13#10 +
    '    Este computador conecta a uma retaguarda já instalada na rede.' + #13#10 +
    '    Você precisará do IP do servidor.');

  PaginaTipo.SelectedValueIndex := TIPO_UNICA; // padrão: máquina única

  // Página 2: IP do servidor (exibida apenas para Terminal)
  PaginaServidor := CreateInputQueryPage(PaginaTipo.ID,
    'Conexão com o servidor',
    'Informe onde o banco de dados está instalado',
    'Digite os dados da retaguarda (máquina que tem o banco de dados):');

  PaginaServidor.Add('IP ou nome do servidor (ex: 192.168.1.10):', False);
  PaginaServidor.Add('Instância SQL Server (padrão: SQLEXPRESS):', False);
  PaginaServidor.Values[0] := '';
  PaginaServidor.Values[1] := 'SQLEXPRESS';
end;

// ── Pula a página de IP se não for Terminal ───────────────────────────────────
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if PageID = PaginaServidor.ID then
    Result := not EhTerminal;
end;

// ── Testa conexão SQL Server via PowerShell ───────────────────────────────────
function TestarConexaoSQL(ServerIP, Instancia: String): Boolean;
var
  DataSource, ConnStr, PsScript, PsCmd: String;
  ResCode: Integer;
begin
  if Trim(Instancia) = '' then Instancia := 'SQLEXPRESS';
  DataSource := ServerIP + '\' + Instancia;

  ConnStr :=
    'Server=' + DataSource +
    ';Database=master;Trusted_Connection=True;' +
    'TrustServerCertificate=True;Encrypt=False;Connect Timeout=6';

  PsScript :=
    'try {' +
    ' Add-Type -AssemblyName System.Data;' +
    ' $c = New-Object System.Data.SqlClient.SqlConnection(''' + ConnStr + ''');' +
    ' $c.Open(); $c.Close(); exit 0' +
    '} catch { exit 1 }';

  PsCmd := '/c powershell -NonInteractive -WindowStyle Hidden -Command "' + PsScript + '"';

  Exec(ExpandConstant('{cmd}'), PsCmd, '', SW_HIDE, ewWaitUntilTerminated, ResCode);
  Result := (ResCode = 0);
end;

// ── Validação e teste de conexão ao clicar em Avançar ────────────────────────
function NextButtonClick(CurPageID: Integer): Boolean;
var
  ServerIP, Instancia: String;
begin
  Result := True;

  if CurPageID = PaginaServidor.ID then
  begin
    ServerIP  := Trim(PaginaServidor.Values[0]);
    Instancia := Trim(PaginaServidor.Values[1]);

    if ServerIP = '' then
    begin
      MsgBox('O IP ou nome do servidor é obrigatório.', mbError, MB_OK);
      Result := False;
      Exit;
    end;

    if Instancia = '' then
      PaginaServidor.Values[1] := 'SQLEXPRESS';

    // Testa a conexão antes de avançar
    WizardForm.NextButton.Enabled := False;
    WizardForm.NextButton.Caption := 'Testando conexão...';

    if TestarConexaoSQL(ServerIP, Instancia) then
    begin
      WizardForm.NextButton.Enabled := True;
      WizardForm.NextButton.Caption := 'Avan&çar';
      MsgBox(
        '✔  Conexão bem-sucedida!' + #13#10 +
        'O servidor "' + ServerIP + '" foi encontrado e está respondendo.',
        mbInformation, MB_OK);
    end
    else
    begin
      WizardForm.NextButton.Enabled := True;
      WizardForm.NextButton.Caption := 'Avan&çar';

      if MsgBox(
        'Não foi possível conectar ao servidor "' + ServerIP + '".' + #13#10 + #13#10 +
        'Verifique:' + #13#10 +
        '  • O servidor está ligado e o SQL Server está ativo' + #13#10 +
        '  • A instância "' + Instancia + '" existe e aceita conexões remotas' + #13#10 +
        '  • A porta 1433 está aberta no firewall do servidor' + #13#10 +
        '  • O serviço "SQL Server Browser" está ativo no servidor' + #13#10 + #13#10 +
        'Deseja continuar mesmo assim?',
        mbError, MB_YESNO) = IDNO then
      begin
        Result := False;
        Exit;
      end;
    end;
  end;
end;

// ── Grava appsettings.json com a connection string correta ───────────────────
// DataSource deve conter UMA barra invertida (ex: ".\SQLEXPRESS" ou "192.168.1.10\SQLEXPRESS").
// A função se encarrega de escapar para JSON: \ → \\ antes de gravar.
procedure GravarConnectionString(DataSource: String);
var
  ConnStr, ConnStrJson, Conteudo, ConteudoDesktop, Arquivo: String;
begin
  ConnStr :=
    'Server=' + DataSource +
    ';Database=ProjetoVarejo;Trusted_Connection=True;' +
    'TrustServerCertificate=True;Encrypt=False;';

  // JSON não aceita \ literal — precisa ser \\.
  // Ex.: "Server=.\SQLEXPRESS" → inválido (\S é escape desconhecido)
  //       "Server=.\\SQLEXPRESS" → válido (.NET lê como .\SQLEXPRESS)
  ConnStrJson := ConnStr;
  StringChangeEx(ConnStrJson, '\', '\\', False);

  Conteudo :=
    '{' + #13#10 +
    '  "ConnectionStrings": {' + #13#10 +
    '    "Default": "' + ConnStrJson + '"' + #13#10 +
    '  }' + #13#10 +
    '}';

  Arquivo := ExpandConstant('{app}\web\appsettings.json');
  DeleteFile(Arquivo);
  SaveStringToFile(Arquivo, Conteudo, False);

  // Sobrescreve o Production para garantir precedência
  Arquivo := ExpandConstant('{app}\web\appsettings.Production.json');
  DeleteFile(Arquivo);
  SaveStringToFile(Arquivo, Conteudo, False);

  ConteudoDesktop :=
    '{' + #13#10 +
    '  "ConnectionStrings": {' + #13#10 +
    '    "Default": "' + ConnStrJson + '"' + #13#10 +
    '  },' + #13#10 +
    '  "Logging": {' + #13#10 +
    '    "LogLevel": {' + #13#10 +
    '      "Default": "Warning"' + #13#10 +
    '    }' + #13#10 +
    '  }' + #13#10 +
    '}';

  Arquivo := ExpandConstant('{app}\desktop\appsettings.json');
  DeleteFile(Arquivo);
  SaveStringToFile(Arquivo, ConteudoDesktop, False);
end;

// ── Registra e inicia o Windows Service da interface web ─────────────────────
// Feito em código porque o [Run] não consegue escapar corretamente as aspas
// aninhadas do binPath= quando o caminho de instalação contém espaços.
//
// O comando equivalente seria:
//   sc create "ProjetoVarejo Web" binPath= "\"C:\...\ProjetoVarejo.Api.exe\" --args"
//
// No Pascal do Inno Setup, '\"' dentro de uma string entre aspas simples
// produz literalmente \", que o parser de linha de comando do Windows
// interpreta como aspas escapadas dentro de um argumento entre aspas.
procedure GravarShellSettings(AppUrl: String; StartApi: Boolean);
var
  Conteudo, Arquivo, StartApiJson: String;
begin
  if StartApi then
    StartApiJson := 'true'
  else
    StartApiJson := 'false';

  Conteudo :=
    '{' + #13#10 +
    '  "Url": "' + AppUrl + '",' + #13#10 +
    '  "StartApi": ' + StartApiJson + ',' + #13#10 +
    '  "ApiExeRelativePath": "..\\web\\ProjetoVarejo.Api.exe",' + #13#10 +
    '  "StartupTimeoutSeconds": 90' + #13#10 +
    '}';

  Arquivo := ExpandConstant('{app}\desktop\appsettings.json');
  DeleteFile(Arquivo);
  SaveStringToFile(Arquivo, Conteudo, False);
end;

procedure RegistrarServico(AppPath: String);
var
  Sc, Params: String;
  ResultCode: Integer;
begin
  Sc := ExpandConstant('{sys}\sc.exe');

  // binPath= precisa que o exe fique entre \" para caminhos com espaços
  Exec(Sc, 'stop "ProjetoVarejo Web"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Exec(Sc, 'delete "ProjetoVarejo Web"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  Params :=
    'create "ProjetoVarejo Web"' +
    ' binPath= "\"' + AppPath + '\web\ProjetoVarejo.Api.exe\"' +
    ' --environment Development --urls http://0.0.0.0:5094"' +
    ' start= auto' +
    ' DisplayName= "ProjetoVarejo Interface Web"';

  Exec(Sc, Params, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  Exec(Sc,
    'description "ProjetoVarejo Web"' +
    ' "Interface web do ProjetoVarejo ERP — acesso pelo navegador na porta 5094"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);

  Exec(Sc, 'start "ProjetoVarejo Web"',
    '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
end;

function NetRuntimeAusente: Boolean;
begin
  Result := False;
end;

// ── Ações de pós-instalação por tipo ─────────────────────────────────────────
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  DataSource: String;
begin
  if CurStep <> ssPostInstall then Exit;

  // ── 1. Connection string ──────────────────────────────────────────────────
  case TipoSelecionado of
    TIPO_UNICA, TIPO_RETAGUARDA:
    begin
      // Banco local — usa instância padrão do SQL Server Express
      GravarConnectionString('.\SQLEXPRESS');
    end;
    TIPO_TERMINAL:
    begin
      // Banco remoto — IP informado pelo usuário na página de servidor
      // Usa \ simples; GravarConnectionString escapa para \\ no JSON
      DataSource :=
        Trim(PaginaServidor.Values[0]) + '\' +
        Trim(PaginaServidor.Values[1]);
      GravarConnectionString(DataSource);
    end;
  end;

  // ── 2. Windows Service da interface web (se task webserver foi marcada) ────
  if EhServidorOuMatriz and WizardIsTaskSelected('webserver') then
    RegistrarServico(ExpandConstant('{app}'));

  // ── 3. Sentinela de primeiro uso (apenas servidor/máquina única) ──────────
  // O Desktop detecta este arquivo e exibe o wizard de configuração inicial.
  // Terminais não passam pelo wizard — já herdam as configs do servidor.
  if EhTerminal then
  begin
    // Terminal usa o mesmo atalho Desktop; a connection string aponta para o servidor.
  end;

  // ── 5. .NET Runtime ───────────────────────────────────────────────────────
  if NetRuntimeAusente then
  begin
    if MsgBox(
      'O .NET 8 Desktop Runtime não foi detectado.' + #13#10 +
      'O sistema não funcionará sem ele.' + #13#10 + #13#10 +
      'Deseja abrir a página de download agora?',
      mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0',
        '', '', SW_SHOWNORMAL, ewNoWait, ResultCode);
  end;

  // ── 4. Mensagem final por tipo ────────────────────────────────────────────
  case TipoSelecionado of
    TIPO_UNICA:
      MsgBox(
        'Instalação concluída!' + #13#10 + #13#10 +
        'O ProjetoVarejo está pronto para uso neste computador.' + #13#10 + #13#10 +
        'Na primeira abertura você configurará o tipo de negócio,' + #13#10 +
        'os dados da empresa e criará o usuário administrador.',
        mbInformation, MB_OK);

    TIPO_RETAGUARDA:
      MsgBox(
        'Servidor instalado com sucesso!' + #13#10 + #13#10 +
        'Para que os terminais se conectem a este servidor:' + #13#10 +
        '  1. Abra o SQL Server Configuration Manager' + #13#10 +
        '  2. Em "Protocolos para SQLEXPRESS", habilite TCP/IP' + #13#10 +
        '  3. Reinicie o serviço SQL Server' + #13#10 + #13#10 +
        'Interface web (se instalada):' + #13#10 +
        '  Acesse pelo navegador em: http://<IP-deste-PC>:5094' + #13#10 + #13#10 +
        'IP deste servidor: Configurações > Rede > Propriedades do adaptador',
        mbInformation, MB_OK);

    TIPO_TERMINAL:
      MsgBox(
        'Terminal configurado!' + #13#10 + #13#10 +
        'Este terminal está configurado para conectar ao servidor:' + #13#10 +
        '  ' + Trim(PaginaServidor.Values[0]) + '\' + Trim(PaginaServidor.Values[1]) + #13#10 + #13#10 +
        'Use as mesmas credenciais de login criadas na retaguarda.',
        mbInformation, MB_OK);
  end;
end;
