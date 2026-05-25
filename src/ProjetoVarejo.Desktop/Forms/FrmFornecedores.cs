using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmFornecedores : Form
{
    private readonly FornecedorService _svc;
    private TextBox txtBusca = null!;
    private StyledGrid grid = null!;
    private Label lblTotal = null!;

    public FrmFornecedores(FornecedorService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Fornecedores";
        Size = new Size(1100, 680);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Fornecedores", "Empresas que fornecem mercadorias");
        lblTotal = Inputs.SubtituloHeader(header);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.CorFundo, Padding = new Padding(0, 10, 0, 10) };
        var (pnlBusca, tb) = Inputs.BarraBusca("Buscar por razão social, fantasia ou CNPJ...");
        pnlBusca.Dock = DockStyle.Fill;
        txtBusca = tb;
        txtBusca.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await CarregarAsync(); };

        var pnlBotoes = new FlowLayoutPanel { Dock = DockStyle.Right, Width = 410, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Tema.CorFundo };
        var btnNovo = Botoes.PrimarioIcone("Novo fornecedor", "\uE710", 180, 40);
        btnNovo.Click += (s, e) => Editar(null);
        var btnEditar = Botoes.GhostIcone("Editar", "\uE70F", 96, 40);
        btnEditar.Click += (s, e) => EditarSel();
        var btnExcluir = Botoes.GhostIcone("Excluir", "\uE74D", 104, 40, Tema.CorErro);
        btnExcluir.Click += async (s, e) => await ExcluirAsync();
        Botoes.ParaToolbar(btnNovo, btnEditar, btnExcluir);
        pnlBotoes.Controls.Add(btnNovo);
        pnlBotoes.Controls.Add(btnEditar);
        pnlBotoes.Controls.Add(btnExcluir);

        toolbar.Controls.Add(pnlBusca);
        toolbar.Controls.Add(pnlBotoes);

        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("RazaoSocial", "Razão Social");
        grid.Columns.Add("NomeFantasia", "Nome Fantasia");
        grid.Columns.Add("Cnpj", "CNPJ");
        grid.Columns.Add("Telefone", "Telefone");
        grid.Columns.Add("Cidade", "Cidade");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns["RazaoSocial"]!.FillWeight = 180;
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
        foreach (var f in lista)
            grid.Rows.Add(f.Id, f.RazaoSocial, f.NomeFantasia, f.Cnpj, f.Telefone, f.Cidade);
        lblTotal.Text = $"{lista.Count} fornecedor(es) cadastrado(s)";
    }

    private void EditarSel()
    {
        if (grid.SelectedRows.Count == 0) { Toast.Mostrar("Selecione um fornecedor.", TipoToast.Info, owner: this); return; }
        Editar((int)grid.SelectedRows[0].Cells["Id"].Value);
    }

    private async void Editar(int? id)
    {
        Fornecedor? f = id.HasValue ? await _svc.BuscarPorIdAsync(id.Value) : new Fornecedor();
        if (f == null) return;
        using var dlg = new FrmFornecedorEdit(f, _svc);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await CarregarAsync();
            Toast.Mostrar("Fornecedor salvo.", TipoToast.Sucesso, owner: this);
        }
    }

    private async Task ExcluirAsync()
    {
        if (grid.SelectedRows.Count == 0) { Toast.Mostrar("Selecione um fornecedor.", TipoToast.Info, owner: this); return; }
        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
        if (MessageBox.Show("Confirma exclusão?", "Confirmação",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var res = await _svc.ExcluirAsync(id);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        await CarregarAsync();
        Toast.Mostrar("Fornecedor excluído.", TipoToast.Sucesso, owner: this);
    }
}

public class FrmFornecedorEdit : Form
{
    private readonly Fornecedor _f;
    private readonly FornecedorService _svc;
    private TextBox txtRazao = null!, txtFantasia = null!, txtCnpj = null!, txtIe = null!;
    private TextBox txtTel = null!, txtEmail = null!, txtContato = null!;
    private TextBox txtCep = null!, txtLog = null!, txtNum = null!, txtBairro = null!, txtCidade = null!, txtUf = null!;

    public FrmFornecedorEdit(Fornecedor f, FornecedorService svc)
    {
        _f = f; _svc = svc;
        InitUi(); Preencher();
    }

    private void InitUi()
    {
        Text = _f.Id == 0 ? "Novo Fornecedor" : "Editar Fornecedor";
        Size = new Size(720, 640);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderModal(Text);
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;
        Inputs.Secao(pnl, "Identificação", ref y);
        txtRazao = Inputs.CampoTexto(pnl, "Razão Social*", 0, y, 620); y += 60;
        txtFantasia = Inputs.CampoTexto(pnl, "Nome Fantasia", 0, y, 620); y += 60;
        txtCnpj = Inputs.CampoTexto(pnl, "CNPJ*", 0, y, 200);
        txtIe = Inputs.CampoTexto(pnl, "Inscrição Estadual", 220, y, 200);
        txtContato = Inputs.CampoTexto(pnl, "Contato", 440, y, 180);
        y += 60;
        txtTel = Inputs.CampoTexto(pnl, "Telefone", 0, y, 200);
        txtEmail = Inputs.CampoTexto(pnl, "Email", 220, y, 400);
        y += 60;

        Inputs.Secao(pnl, "Endereço", ref y);
        txtCep = Inputs.CampoTexto(pnl, "CEP", 0, y, 120);
        txtLog = Inputs.CampoTexto(pnl, "Logradouro", 140, y, 400);
        txtNum = Inputs.CampoTexto(pnl, "Número", 560, y, 60);
        y += 60;
        txtBairro = Inputs.CampoTexto(pnl, "Bairro", 0, y, 220);
        txtCidade = Inputs.CampoTexto(pnl, "Cidade", 240, y, 340);
        txtUf = Inputs.CampoTexto(pnl, "UF", 600, y, 40);

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
        txtRazao.Text = _f.RazaoSocial;
        txtFantasia.Text = _f.NomeFantasia ?? "";
        txtCnpj.Text = _f.Cnpj;
        txtIe.Text = _f.InscricaoEstadual ?? "";
        txtTel.Text = _f.Telefone ?? "";
        txtEmail.Text = _f.Email ?? "";
        txtContato.Text = _f.Contato ?? "";
        txtCep.Text = _f.Cep ?? "";
        txtLog.Text = _f.Logradouro ?? "";
        txtNum.Text = _f.Numero ?? "";
        txtBairro.Text = _f.Bairro ?? "";
        txtCidade.Text = _f.Cidade ?? "";
        txtUf.Text = _f.Uf ?? "";
    }

    private async Task SalvarAsync()
    {
        _f.RazaoSocial = txtRazao.Text.Trim();
        _f.NomeFantasia = txtFantasia.Text.Trim();
        _f.Cnpj = txtCnpj.Text.Trim();
        _f.InscricaoEstadual = txtIe.Text.Trim();
        _f.Telefone = txtTel.Text.Trim();
        _f.Email = txtEmail.Text.Trim();
        _f.Contato = txtContato.Text.Trim();
        _f.Cep = txtCep.Text.Trim();
        _f.Logradouro = txtLog.Text.Trim();
        _f.Numero = txtNum.Text.Trim();
        _f.Bairro = txtBairro.Text.Trim();
        _f.Cidade = txtCidade.Text.Trim();
        _f.Uf = txtUf.Text.Trim();
        var res = await _svc.SalvarAsync(_f);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        DialogResult = DialogResult.OK;
        Close();
    }
}
