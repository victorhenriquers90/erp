using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmClientes : Form
{
    private readonly ClienteService _svc;
    private TextBox txtBusca = null!;
    private StyledGrid grid = null!;
    private Label lblTotal = null!;

    public FrmClientes(ClienteService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Clientes";
        Size = new Size(1100, 680);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Clientes", "Cadastro de clientes pessoa física e jurídica");
        lblTotal = (Label)header.Controls[0]; // subtitle ref

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Tema.CorFundo, Padding = new Padding(0, 8, 0, 12) };
        var (pnlBusca, tb) = Inputs.BarraBusca("Buscar por nome ou CPF/CNPJ...");
        pnlBusca.Dock = DockStyle.Fill;
        txtBusca = tb;
        txtBusca.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await CarregarAsync(); };

        var pnlBotoes = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 380, FlowDirection = FlowDirection.RightToLeft, BackColor = Tema.CorFundo };
        var btnNovo = Botoes.Primario("Novo cliente", 170, 40);
        btnNovo.Click += (s, e) => Editar(null);
        var btnEditar = Botoes.Ghost("Editar", 90, 40);
        btnEditar.Click += (s, e) => EditarSel();
        var btnExcluir = Botoes.Ghost("Excluir", 100, 40);
        btnExcluir.ForeColor = Tema.CorErro;
        btnExcluir.FlatAppearance.BorderColor = Tema.CorErro;
        btnExcluir.Click += async (s, e) => await ExcluirAsync();
        pnlBotoes.Controls.Add(btnNovo);
        pnlBotoes.Controls.Add(btnEditar);
        pnlBotoes.Controls.Add(btnExcluir);

        toolbar.Controls.Add(pnlBusca);
        toolbar.Controls.Add(pnlBotoes);

        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("Nome", "Nome");
        grid.Columns.Add("CpfCnpj", "CPF/CNPJ");
        grid.Columns.Add("Telefone", "Telefone");
        grid.Columns.Add("Email", "Email");
        grid.Columns.Add("Cidade", "Cidade");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns["Nome"]!.FillWeight = 240;
        grid.DoubleClick += (s, e) => EditarSel();
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        var lista = await _svc.ListarAsync(txtBusca.Text);
        grid.Rows.Clear();
        foreach (var c in lista)
            grid.Rows.Add(c.Id, c.Nome, c.CpfCnpj, c.Telefone, c.Email, c.Cidade);
        lblTotal.Text = $"{lista.Count} cliente(s) cadastrado(s)";
    }

    private void EditarSel()
    {
        if (grid.SelectedRows.Count == 0) { Toast.Mostrar("Selecione um cliente.", TipoToast.Info, owner: this); return; }
        Editar((int)grid.SelectedRows[0].Cells["Id"].Value);
    }

    private async void Editar(int? id)
    {
        Cliente? c = id.HasValue ? await _svc.BuscarPorIdAsync(id.Value) : new Cliente();
        if (c == null) return;
        using var dlg = new FrmClienteEdit(c, _svc);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await CarregarAsync();
            Toast.Mostrar("Cliente salvo.", TipoToast.Sucesso, owner: this);
        }
    }

    private async Task ExcluirAsync()
    {
        if (grid.SelectedRows.Count == 0) { Toast.Mostrar("Selecione um cliente.", TipoToast.Info, owner: this); return; }
        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
        if (MessageBox.Show("Confirma exclusão?", "Confirmação",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var res = await _svc.ExcluirAsync(id);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        await CarregarAsync();
        Toast.Mostrar("Cliente excluído.", TipoToast.Sucesso, owner: this);
    }
}

public class FrmClienteEdit : Form
{
    private readonly Cliente _cli;
    private readonly ClienteService _svc;
    private TextBox txtNome = null!, txtCpf = null!, txtTel = null!, txtEmail = null!;
    private TextBox txtCep = null!, txtLog = null!, txtNum = null!, txtBairro = null!, txtCidade = null!, txtUf = null!;
    private CheckBox chkPJ = null!;

    public FrmClienteEdit(Cliente cli, ClienteService svc)
    {
        _cli = cli; _svc = svc;
        InitUi();
        Preencher();
    }

    private void InitUi()
    {
        Text = _cli.Id == 0 ? "Novo Cliente" : $"Editar Cliente";
        Size = new Size(700, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderModal(Text);
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;
        Inputs.Secao(pnl, "Dados pessoais", ref y);
        txtNome = Inputs.CampoTexto(pnl, "Nome completo*", 0, y, 600); y += 60;
        chkPJ = Inputs.CampoCheck(pnl, "Pessoa Jurídica", 0, y, 200);
        txtCpf = Inputs.CampoTexto(pnl, "CPF/CNPJ", 200, y - 20, 200);
        txtTel = Inputs.CampoTexto(pnl, "Telefone", 420, y - 20, 180);
        y += 50;
        txtEmail = Inputs.CampoTexto(pnl, "Email", 0, y, 600); y += 60;

        Inputs.Secao(pnl, "Endereço", ref y);
        txtCep = Inputs.CampoTexto(pnl, "CEP", 0, y, 120);
        txtLog = Inputs.CampoTexto(pnl, "Logradouro", 140, y, 380);
        txtNum = Inputs.CampoTexto(pnl, "Número", 540, y, 60);
        y += 60;
        txtBairro = Inputs.CampoTexto(pnl, "Bairro", 0, y, 200);
        txtCidade = Inputs.CampoTexto(pnl, "Cidade", 220, y, 320);
        txtUf = Inputs.CampoTexto(pnl, "UF", 560, y, 40);

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
        txtNome.Text = _cli.Nome;
        txtCpf.Text = _cli.CpfCnpj ?? "";
        txtTel.Text = _cli.Telefone ?? "";
        txtEmail.Text = _cli.Email ?? "";
        txtCep.Text = _cli.Cep ?? "";
        txtLog.Text = _cli.Logradouro ?? "";
        txtNum.Text = _cli.Numero ?? "";
        txtBairro.Text = _cli.Bairro ?? "";
        txtCidade.Text = _cli.Cidade ?? "";
        txtUf.Text = _cli.Uf ?? "";
        chkPJ.Checked = _cli.PessoaJuridica;
    }

    private async Task SalvarAsync()
    {
        _cli.Nome = txtNome.Text.Trim();
        _cli.CpfCnpj = txtCpf.Text.Trim();
        _cli.Telefone = txtTel.Text.Trim();
        _cli.Email = txtEmail.Text.Trim();
        _cli.Cep = txtCep.Text.Trim();
        _cli.Logradouro = txtLog.Text.Trim();
        _cli.Numero = txtNum.Text.Trim();
        _cli.Bairro = txtBairro.Text.Trim();
        _cli.Cidade = txtCidade.Text.Trim();
        _cli.Uf = txtUf.Text.Trim();
        _cli.PessoaJuridica = chkPJ.Checked;
        var res = await _svc.SalvarAsync(_cli);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        DialogResult = DialogResult.OK;
        Close();
    }
}
