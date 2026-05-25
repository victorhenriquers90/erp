using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmUsuarios : Form
{
    private readonly UsuarioService _svc;
    private TextBox txtBusca = null!;
    private CheckBox chkInativos = null!;
    private StyledGrid grid = null!;
    private Label lblTotal = null!;

    public FrmUsuarios(UsuarioService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Usuarios";
        Size = new Size(1120, 690);
        MinimumSize = new Size(960, 620);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Usuarios", "Acessos, perfis operacionais e troca de senha");
        lblTotal = Inputs.SubtituloHeader(header);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.CorFundo, Padding = new Padding(0, 10, 0, 10) };
        var (pnlBusca, tb) = Inputs.BarraBusca("Buscar por nome ou login...");
        pnlBusca.Dock = DockStyle.Fill;
        txtBusca = tb;
        txtBusca.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await CarregarAsync(); };

        chkInativos = new CheckBox
        {
            Text = "Inativos",
            Dock = DockStyle.Right,
            Width = 78,
            Font = Tema.FontPequenaBold,
            ForeColor = Tema.CorTextoMedio,
            BackColor = Tema.CorFundo,
            TextAlign = ContentAlignment.MiddleCenter,
            Checked = true
        };
        chkInativos.CheckedChanged += async (s, e) => await CarregarAsync();

        var pnlBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 548,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Tema.CorFundo
        };

        var btnNovo = Botoes.PrimarioIcone("Novo usuario", Tema.IconAdicionar, 156, 40);
        btnNovo.Click += (s, e) => Editar(null);
        var btnEditar = Botoes.GhostIcone("Editar", Tema.IconEditar, 96, 40);
        btnEditar.Click += (s, e) => EditarSelecionado();
        var btnSenha = Botoes.GhostIcone("Senha", Tema.IconConfig, 94, 40);
        btnSenha.Click += (s, e) => RedefinirSenha();
        var btnAtivo = Botoes.GhostIcone("Ativar/Inativar", Tema.IconUsuario, 142, 40, Tema.CorAlerta);
        btnAtivo.Click += async (s, e) => await AlternarAtivoAsync();
        Botoes.ParaToolbar(btnNovo, btnEditar, btnSenha, btnAtivo);
        pnlBotoes.Controls.Add(btnNovo);
        pnlBotoes.Controls.Add(btnAtivo);
        pnlBotoes.Controls.Add(btnSenha);
        pnlBotoes.Controls.Add(btnEditar);

        toolbar.Controls.Add(pnlBusca);
        toolbar.Controls.Add(chkInativos);
        toolbar.Controls.Add(pnlBotoes);

        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("Nome", "Nome");
        grid.Columns.Add("Login", "Login");
        grid.Columns.Add("Perfil", "Perfil");
        grid.Columns.Add("Status", "Status");
        grid.Columns.Add("UltimoAcesso", "Ultimo acesso");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns["Nome"]!.FillWeight = 200;
        grid.Columns["Login"]!.FillWeight = 120;
        grid.Columns["Perfil"]!.FillWeight = 120;
        grid.Columns["Status"]!.FillWeight = 80;
        grid.Columns["UltimoAcesso"]!.FillWeight = 120;
        grid.DoubleClick += (s, e) => EditarSelecionado();
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        var lista = await _svc.ListarAsync(txtBusca.Text, chkInativos.Checked);
        grid.Rows.Clear();

        foreach (var u in lista)
        {
            var rowIndex = grid.Rows.Add(
                u.Id,
                u.Nome,
                u.Login,
                NomePerfil(u.Perfil),
                u.Ativo ? "Ativo" : "Inativo",
                u.UltimoAcesso?.ToString("dd/MM/yyyy HH:mm") ?? "");

            var row = grid.Rows[rowIndex];
            row.Cells["Status"].Style.ForeColor = u.Ativo ? Tema.CorSucesso : Tema.CorErro;
            row.Cells["Status"].Style.SelectionForeColor = u.Ativo ? Tema.CorSucesso : Tema.CorErro;
            row.Cells["Status"].Style.Font = Tema.FontCorpoBold;
        }

        var ativos = lista.Count(u => u.Ativo);
        lblTotal.Text = $"{lista.Count} usuario(s) listado(s) | {ativos} ativo(s)";
    }

    private void EditarSelecionado()
    {
        if (grid.SelectedRows.Count == 0)
        {
            Toast.Mostrar("Selecione um usuario.", TipoToast.Info, owner: this);
            return;
        }
        Editar((int)grid.SelectedRows[0].Cells["Id"].Value);
    }

    private async void Editar(int? id)
    {
        Usuario? usuario = id.HasValue ? await _svc.BuscarPorIdAsync(id.Value) : new Usuario { Ativo = true, Perfil = PerfilUsuario.Caixa };
        if (usuario == null) return;

        using var dlg = new FrmUsuarioEdit(usuario, _svc);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await CarregarAsync();
            Toast.Mostrar("Usuario salvo.", TipoToast.Sucesso, owner: this);
        }
    }

    private void RedefinirSenha()
    {
        if (grid.SelectedRows.Count == 0)
        {
            Toast.Mostrar("Selecione um usuario.", TipoToast.Info, owner: this);
            return;
        }

        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
        var nome = grid.SelectedRows[0].Cells["Nome"].Value?.ToString() ?? "usuario";
        using var dlg = new FrmRedefinirSenha(nome, id, _svc);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            Toast.Mostrar("Senha redefinida.", TipoToast.Sucesso, owner: this);
    }

    private async Task AlternarAtivoAsync()
    {
        if (grid.SelectedRows.Count == 0)
        {
            Toast.Mostrar("Selecione um usuario.", TipoToast.Info, owner: this);
            return;
        }

        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
        var status = grid.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";
        var acao = status == "Ativo" ? "inativar" : "ativar";

        if (MessageBox.Show($"Confirma {acao} este usuario?", "Confirmacao",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        var res = await _svc.AlternarAtivoAsync(id);
        if (!res.Sucesso)
        {
            Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this);
            return;
        }

        await CarregarAsync();
        Toast.Mostrar("Status atualizado.", TipoToast.Sucesso, owner: this);
    }

    private static string NomePerfil(PerfilUsuario perfil) => perfil switch
    {
        PerfilUsuario.Administrador => "Administrador",
        PerfilUsuario.Gerente => "Gerente",
        PerfilUsuario.Caixa => "Caixa",
        PerfilUsuario.Estoquista => "Estoquista",
        _ => perfil.ToString()
    };
}

public class FrmUsuarioEdit : Form
{
    private readonly Usuario _usuario;
    private readonly UsuarioService _svc;
    private TextBox txtNome = null!, txtLogin = null!, txtSenha = null!, txtConfirmar = null!;
    private ComboBox cboPerfil = null!;
    private CheckBox chkAtivo = null!;

    public FrmUsuarioEdit(Usuario usuario, UsuarioService svc)
    {
        _usuario = usuario;
        _svc = svc;
        InitUi();
        Preencher();
    }

    private void InitUi()
    {
        Text = _usuario.Id == 0 ? "Novo Usuario" : "Editar Usuario";
        Size = new Size(680, 540);
        MinimumSize = new Size(640, 500);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false;
        MinimizeBox = false;
        Padding = new Padding(18);

        var header = Inputs.HeaderModal(Text);
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(18) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard, AutoScroll = true };

        int y = 0;
        Inputs.Secao(pnl, "Identificacao", ref y, width: 580);
        txtNome = Inputs.CampoTexto(pnl, "Nome*", 0, y, 560);
        y += 60;
        txtLogin = Inputs.CampoTexto(pnl, "Login*", 0, y, 260);
        cboPerfil = Inputs.CampoCombo(pnl, "Perfil", 280, y, 280);
        cboPerfil.Items.AddRange(Enum.GetValues<PerfilUsuario>().Cast<object>().ToArray());
        y += 64;
        chkAtivo = Inputs.CampoCheck(pnl, "Usuario ativo", 0, y, 180, true);
        y += 42;

        Inputs.Secao(pnl, _usuario.Id == 0 ? "Senha inicial" : "Nova senha opcional", ref y, width: 580);
        txtSenha = Inputs.CampoTexto(pnl, _usuario.Id == 0 ? "Senha*" : "Nova senha", 0, y, 260);
        txtSenha.UseSystemPasswordChar = true;
        txtConfirmar = Inputs.CampoTexto(pnl, "Confirmar senha", 280, y, 260);
        txtConfirmar.UseSystemPasswordChar = true;
        y += 60;

        var dica = new Label
        {
            Text = _usuario.Id == 0
                ? "Use uma senha com pelo menos 6 caracteres."
                : "Deixe em branco para manter a senha atual.",
            Left = 0,
            Top = y,
            Width = 560,
            Height = 24,
            Font = Tema.FontPequena,
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        };
        pnl.Controls.Add(dica);
        pnl.AutoScrollMinSize = new Size(600, y + 40);

        card.Controls.Add(pnl);

        var (rodape, btnSalvar, btnCancelar) = Inputs.RodapeSalvarCancelar();
        btnSalvar.Click += async (s, e) => await SalvarAsync();
        btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        AcceptButton = btnSalvar;
        CancelButton = btnCancelar;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);
    }

    private void Preencher()
    {
        txtNome.Text = _usuario.Nome;
        txtLogin.Text = _usuario.Login;
        cboPerfil.SelectedItem = _usuario.Perfil == 0 ? PerfilUsuario.Caixa : _usuario.Perfil;
        chkAtivo.Checked = _usuario.Ativo;
    }

    private async Task SalvarAsync()
    {
        var senha = txtSenha.Text;
        var confirmar = txtConfirmar.Text;

        if (!string.IsNullOrWhiteSpace(senha) || !string.IsNullOrWhiteSpace(confirmar) || _usuario.Id == 0)
        {
            if (senha != confirmar)
            {
                Toast.Mostrar("As senhas nao conferem.", TipoToast.Erro, owner: this);
                return;
            }
        }

        _usuario.Nome = txtNome.Text.Trim();
        _usuario.Login = txtLogin.Text.Trim();
        _usuario.Perfil = (PerfilUsuario)(cboPerfil.SelectedItem ?? PerfilUsuario.Caixa);
        _usuario.Ativo = chkAtivo.Checked;

        var res = await _svc.SalvarAsync(_usuario, string.IsNullOrWhiteSpace(senha) ? null : senha);
        if (!res.Sucesso)
        {
            Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}

public class FrmRedefinirSenha : Form
{
    private readonly int _usuarioId;
    private readonly UsuarioService _svc;
    private TextBox txtSenha = null!, txtConfirmar = null!;

    public FrmRedefinirSenha(string nomeUsuario, int usuarioId, UsuarioService svc)
    {
        _usuarioId = usuarioId;
        _svc = svc;
        InitUi(nomeUsuario);
    }

    private void InitUi(string nomeUsuario)
    {
        Text = "Redefinir Senha";
        Size = new Size(560, 340);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false;
        MinimizeBox = false;
        Padding = new Padding(18);

        var header = Inputs.HeaderModal($"Redefinir senha - {nomeUsuario}");
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(18) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        txtSenha = Inputs.CampoTexto(pnl, "Nova senha*", 0, 0, 460);
        txtSenha.UseSystemPasswordChar = true;
        txtConfirmar = Inputs.CampoTexto(pnl, "Confirmar senha*", 0, 62, 460);
        txtConfirmar.UseSystemPasswordChar = true;

        card.Controls.Add(pnl);
        var (rodape, btnSalvar, btnCancelar) = Inputs.RodapeSalvarCancelar("Redefinir");
        btnSalvar.Click += async (s, e) => await SalvarAsync();
        btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        AcceptButton = btnSalvar;
        CancelButton = btnCancelar;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);
    }

    private async Task SalvarAsync()
    {
        if (txtSenha.Text != txtConfirmar.Text)
        {
            Toast.Mostrar("As senhas nao conferem.", TipoToast.Erro, owner: this);
            return;
        }

        var res = await _svc.RedefinirSenhaAsync(_usuarioId, txtSenha.Text);
        if (!res.Sucesso)
        {
            Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
