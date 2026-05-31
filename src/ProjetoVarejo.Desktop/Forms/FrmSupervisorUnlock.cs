using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;

namespace ProjetoVarejo.Desktop.Forms;

/// <summary>
/// Dialog de autorização de supervisor.
/// Exibido sempre que o operador tenta executar uma ação restrita no PDV.
/// O operador principal continua logado; o supervisor/admin digita as credenciais
/// apenas para autorizar aquela ação específica — a sessão ativa não é alterada.
/// </summary>
public sealed class FrmSupervisorUnlock : Form
{
    // ── Configuração ──────────────────────────────────────────────────────────
    private const int MaxTentativas = 3;

    // ── Serviços ──────────────────────────────────────────────────────────────
    private readonly IAutenticacaoService _autenticacao;
    private readonly Permissao _permissaoRequerida;

    // ── UI ────────────────────────────────────────────────────────────────────
    private readonly TextBox _txtLogin = null!;
    private readonly TextBox _txtSenha = null!;
    private readonly Label _lblErro = null!;
    private readonly Button _btnAutorizar = null!;
    private int _tentativas;

    // ── Resultado ─────────────────────────────────────────────────────────────
    /// <summary>Usuário que autorizou a ação. Preenchido quando DialogResult = OK.</summary>
    public Usuario? SupervisorAutorizado { get; private set; }

    public FrmSupervisorUnlock(
        IAutenticacaoService autenticacao,
        string descricaoAcao,
        Permissao permissaoRequerida)
    {
        _autenticacao = autenticacao;
        _permissaoRequerida = permissaoRequerida;

        // Janela
        Text = "Autorização Necessária";
        Size = new Size(460, 320);
        MinimumSize = new Size(460, 320);
        MaximumSize = new Size(460, 320);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        KeyPreview = true;
        KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
            if (e.KeyCode == Keys.Enter)  { e.SuppressKeyPress = true; _ = AutorizarAsync(); }
        };

        // ── Faixa de topo (cor primária) ─────────────────────────────────────
        var topo = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Tema.CorPrimaria
        };
        var lblIco = new Label
        {
            Text = "",   // ícone de cadeado (Segoe MDL2 Assets)
            Font = new Font("Segoe MDL2 Assets", 22),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Dock = DockStyle.Left, Width = 64,
            TextAlign = ContentAlignment.MiddleCenter
        };
        var lblTitulo = new Label
        {
            Text = "Autorização Necessária",
            Font = new Font(Tema.FontFamily, 14, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        topo.Controls.Add(lblTitulo);
        topo.Controls.Add(lblIco);

        // ── Corpo ─────────────────────────────────────────────────────────────
        var corpo = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 14, 24, 14),
            BackColor = Tema.CorFundo
        };

        // Descrição da ação
        var lblAcaoLabel = Inputs.Rotulo("AÇÃO SOLICITADA", 0, 0, 400);
        lblAcaoLabel.Dock = DockStyle.Top;
        lblAcaoLabel.Height = 18;
        lblAcaoLabel.Margin = new Padding(0, 0, 0, 2);

        var lblAcao = new Label
        {
            Text = descricaoAcao,
            Dock = DockStyle.Top,
            Height = 26,
            Font = new Font(Tema.FontFamily, 10, FontStyle.Bold),
            ForeColor = Tema.CorTextoEscuro,
            AutoEllipsis = true
        };

        // Campos de credencial
        var pnlCampos = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            Height = 90,
            BackColor = Tema.CorFundo,
            Padding = new Padding(0, 10, 0, 0)
        };
        pnlCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        pnlCampos.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        pnlCampos.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        pnlCampos.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var lblL = Inputs.Rotulo("USUÁRIO DO SUPERVISOR", 0, 0, 200);
        lblL.Dock = DockStyle.Fill; lblL.Margin = new Padding(0, 0, 8, 0);
        var lblS = Inputs.Rotulo("SENHA", 0, 0, 200);
        lblS.Dock = DockStyle.Fill;

        _txtLogin = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamily, 11),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 8, 0)
        };
        _txtSenha = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamily, 11),
            BorderStyle = BorderStyle.FixedSingle,
            PasswordChar = '•'
        };
        pnlCampos.Controls.Add(lblL, 0, 0);
        pnlCampos.Controls.Add(lblS, 1, 0);
        pnlCampos.Controls.Add(_txtLogin, 0, 1);
        pnlCampos.Controls.Add(_txtSenha, 1, 1);

        // Erro
        _lblErro = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font(Tema.FontFamily, 9.5f),
            ForeColor = Color.FromArgb(180, 30, 30),
            BackColor = Tema.CorFundo,
            Visible = false
        };

        // Botões
        var pnlBtns = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            BackColor = Tema.CorFundo,
            Padding = new Padding(0, 8, 0, 0)
        };
        var btnCancelar = Botoes.Ghost("Cancelar", 120, 42);
        btnCancelar.Dock = DockStyle.Left;
        btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        _btnAutorizar = Botoes.Sucesso("Autorizar", 140, 42);
        _btnAutorizar.Dock = DockStyle.Right;
        _btnAutorizar.Click += async (s, e) => await AutorizarAsync();
        _btnAutorizar.Font = new Font(Tema.FontFamily, 10, FontStyle.Bold);

        pnlBtns.Controls.Add(_btnAutorizar);
        pnlBtns.Controls.Add(btnCancelar);

        // Montar hierarquia (reverso: Dock.Top adiciona de baixo pra cima)
        corpo.Controls.Add(pnlBtns);
        corpo.Controls.Add(_lblErro);
        corpo.Controls.Add(pnlCampos);
        corpo.Controls.Add(lblAcao);
        corpo.Controls.Add(lblAcaoLabel);

        Controls.Add(corpo);
        Controls.Add(topo);

        Shown += (s, e) => _txtLogin.Focus();
    }

    // ── Lógica ────────────────────────────────────────────────────────────────

    private async Task AutorizarAsync()
    {
        var login = _txtLogin.Text.Trim();
        var senha = _txtSenha.Text;

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            MostrarErro("Informe o usuário e a senha do supervisor.");
            return;
        }

        _btnAutorizar.Enabled = false;
        _lblErro.Visible = false;

        var resultado = await _autenticacao.ValidarCredenciaisAsync(login, senha);

        if (!resultado.Sucesso)
        {
            _tentativas++;
            var restantes = MaxTentativas - _tentativas;
            if (restantes <= 0)
            {
                Log.Warning("FrmSupervisorUnlock: {MaxTentativas} tentativas falhas para login '{Login}'", MaxTentativas, login);
                MostrarErro("Número máximo de tentativas atingido.");
                _btnAutorizar.Enabled = false;
                return;
            }
            MostrarErro($"Credenciais inválidas. {restantes} tentativa(s) restante(s).");
            _txtSenha.Clear();
            _btnAutorizar.Enabled = true;
            _txtSenha.Focus();
            return;
        }

        var supervisor = resultado.Valor!;

        // Verifica se o supervisor tem a permissão necessária (perfil padrão)
        var temPermissao = PermissaoService.PermissoesPadrao.TryGetValue(supervisor.Perfil, out var perms)
                           && perms.Contains(_permissaoRequerida);

        if (!temPermissao)
        {
            Log.Warning("FrmSupervisorUnlock: usuário '{Login}' (perfil {Perfil}) não tem permissão {Permissao}",
                login, supervisor.Perfil, _permissaoRequerida);
            MostrarErro($"O usuário '{supervisor.Nome}' não tem permissão para esta ação.");
            _txtSenha.Clear();
            _btnAutorizar.Enabled = true;
            _txtSenha.Focus();
            return;
        }

        // Autorizado!
        Log.Information("Autorização de supervisor: '{SupervisorLogin}' ({Perfil}) autorizou {Permissao}",
            login, supervisor.Perfil, _permissaoRequerida);

        SupervisorAutorizado = supervisor;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void MostrarErro(string mensagem)
    {
        _lblErro.Text = "⚠  " + mensagem;
        _lblErro.Visible = true;
        _btnAutorizar.Enabled = _tentativas < MaxTentativas;
    }
}
