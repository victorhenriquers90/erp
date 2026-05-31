using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmFiliais : Form
{
    private readonly FilialService _svc;
    private StyledGrid grid = null!;
    private Label lblTotal = null!;
    private Button btnFiltroInativas = null!;

    public FrmFiliais(FilialService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Filiais";
        Size = new Size(980, 640);
        MinimumSize = new Size(860, 560);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Filiais", "Unidades e pontos de venda da empresa");
        lblTotal = Inputs.SubtituloHeader(header);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Tema.CorFundo, Padding = new Padding(0, 16, 0, 16) };

        btnFiltroInativas = Botoes.Toggle("Inativas", true);
        btnFiltroInativas.Click += async (s, e) => await CarregarAsync();

        var pnlBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 640,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Tema.CorFundo
        };

        var btnNova = Botoes.PrimarioIcone("Nova filial", Tema.IconAdicionar, 164, 40);
        btnNova.Click += (s, e) => Editar(null);
        var btnEditar = Botoes.GhostIcone("Editar", Tema.IconEditar, 118, 40);
        btnEditar.Click += (s, e) => EditarSelecionada();
        var btnAtivo = Botoes.GhostIcone("Ativar/Inativar", Tema.IconConfig, 178, 40, Tema.CorAlerta);
        btnAtivo.Click += async (s, e) => await AlternarAtivoAsync();
        btnFiltroInativas.Width = 128;
        Botoes.ParaPainelToolbar(pnlBotoes, btnFiltroInativas, btnEditar, btnAtivo, btnNova);
        pnlBotoes.Controls.Add(btnFiltroInativas);
        pnlBotoes.Controls.Add(btnEditar);
        pnlBotoes.Controls.Add(btnAtivo);
        pnlBotoes.Controls.Add(btnNova);
        toolbar.Controls.Add(pnlBotoes);

        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("Codigo", "Código");
        grid.Columns.Add("Nome", "Nome");
        grid.Columns.Add("Cnpj", "CNPJ");
        grid.Columns.Add("Matriz", "Matriz");
        grid.Columns.Add("Status", "Status");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns["Codigo"]!.FillWeight = 80;
        grid.Columns["Nome"]!.FillWeight = 220;
        grid.Columns["Cnpj"]!.FillWeight = 140;
        grid.Columns["Matriz"]!.FillWeight = 70;
        grid.Columns["Status"]!.FillWeight = 80;
        grid.DoubleClick += (s, e) => EditarSelecionada();
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        var lista = await _svc.ListarAsync(incluirInativas: Botoes.ToggleAtivo(btnFiltroInativas));
        grid.Rows.Clear();

        foreach (var f in lista)
        {
            var rowIndex = grid.Rows.Add(
                f.Id,
                f.Codigo,
                f.Nome,
                f.Cnpj ?? "",
                f.IsMatriz ? "Sim" : "",
                f.Ativo ? "Ativa" : "Inativa");

            var row = grid.Rows[rowIndex];
            row.Cells["Status"].Style.ForeColor = f.Ativo ? Tema.CorSucesso : Tema.CorErro;
            row.Cells["Status"].Style.SelectionForeColor = f.Ativo ? Tema.CorSucesso : Tema.CorErro;
            row.Cells["Status"].Style.Font = Tema.FontCorpoBold;

            if (f.IsMatriz)
            {
                row.Cells["Matriz"].Style.ForeColor = Tema.CorPrimaria;
                row.Cells["Matriz"].Style.Font = Tema.FontCorpoBold;
            }
        }

        var ativas = lista.Count(f => f.Ativo);
        lblTotal.Text = $"{lista.Count} filial(is) | {ativas} ativa(s)";
    }

    private void EditarSelecionada()
    {
        if (grid.SelectedRows.Count == 0)
        {
            Toast.Mostrar("Selecione uma filial.", TipoToast.Info, owner: this);
            return;
        }
        Editar((int)grid.SelectedRows[0].Cells["Id"].Value);
    }

    private async void Editar(int? id)
    {
        try
        {
            Filial? filial = id.HasValue
                ? await _svc.BuscarPorIdAsync(id.Value)
                : new Filial { Ativo = true };
            if (filial == null) return;

            using var dlg = new FrmFilialEdit(filial, _svc);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await CarregarAsync();
                Toast.Mostrar("Filial salva.", TipoToast.Sucesso, owner: this);
            }
        }
        catch (Exception ex)
        {
            Toast.Mostrar(ex.Message, TipoToast.Erro, owner: this);
        }
    }

    private async Task AlternarAtivoAsync()
    {
        if (grid.SelectedRows.Count == 0)
        {
            Toast.Mostrar("Selecione uma filial.", TipoToast.Info, owner: this);
            return;
        }

        var id     = (int)grid.SelectedRows[0].Cells["Id"].Value;
        var status = grid.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";
        var acao   = status == "Ativa" ? "inativar" : "ativar";

        if (MessageBox.Show($"Confirma {acao} esta filial?", "Confirmação",
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
}

public class FrmFilialEdit : Form
{
    private readonly Filial _filial;
    private readonly FilialService _svc;
    private TextBox txtCodigo = null!, txtNome = null!, txtCnpj = null!, txtEndereco = null!, txtTelefone = null!;
    private CheckBox chkMatriz = null!, chkAtivo = null!;

    public FrmFilialEdit(Filial filial, FilialService svc)
    {
        _filial = filial;
        _svc    = svc;
        InitUi();
        Preencher();
    }

    private void InitUi()
    {
        Text = _filial.Id == 0 ? "Nova Filial" : "Editar Filial";
        Size = new Size(680, 520);
        MinimumSize = new Size(640, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false;
        MinimizeBox = false;
        Padding = new Padding(18);

        var header = Inputs.HeaderModal(Text);
        var card   = new Card { Dock = DockStyle.Fill, Padding = new Padding(18) };
        var pnl    = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard, AutoScroll = true };

        int y = 0;
        Inputs.Secao(pnl, "Identificação", ref y, width: 580);
        txtCodigo = Inputs.CampoTexto(pnl, "Código*", 0, y, 180);
        txtNome   = Inputs.CampoTexto(pnl, "Nome*", 200, y, 360);
        y += 64;
        txtCnpj = Inputs.CampoTexto(pnl, "CNPJ", 0, y, 260);
        txtTelefone = Inputs.CampoTexto(pnl, "Telefone", 280, y, 260);
        y += 64;
        txtEndereco = Inputs.CampoTexto(pnl, "Endereço", 0, y, 560);
        y += 64;
        chkMatriz = Inputs.CampoCheck(pnl, "É a filial Matriz", 0, y, 200, false);
        chkAtivo  = Inputs.CampoCheck(pnl, "Filial ativa", 220, y, 160, true);

        // Protege a checkbox Matriz se já for Matriz persistida
        if (_filial.IsMatriz && _filial.Id != 0)
        {
            chkMatriz.Enabled = false;
            var dica = new Label
            {
                Text = "A Matriz não pode ser alterada.",
                Left = 0, Top = y + 28, Width = 560, Height = 20,
                Font = Tema.FontPequena, ForeColor = Tema.CorTextoMedio,
                BackColor = Color.Transparent
            };
            pnl.Controls.Add(dica);
        }

        pnl.AutoScrollMinSize = new Size(600, y + 80);
        card.Controls.Add(pnl);

        var (rodape, btnSalvar, btnCancelar) = Inputs.RodapeSalvarCancelar();
        btnSalvar.Click  += async (s, e) => await SalvarAsync();
        btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        AcceptButton = btnSalvar;
        CancelButton = btnCancelar;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);
    }

    private void Preencher()
    {
        txtCodigo.Text   = _filial.Codigo;
        txtNome.Text     = _filial.Nome;
        txtCnpj.Text     = _filial.Cnpj ?? "";
        txtTelefone.Text = _filial.Telefone ?? "";
        txtEndereco.Text = _filial.Endereco ?? "";
        chkMatriz.Checked = _filial.IsMatriz;
        chkAtivo.Checked  = _filial.Ativo;
    }

    private async Task SalvarAsync()
    {
        _filial.Codigo   = txtCodigo.Text.Trim();
        _filial.Nome     = txtNome.Text.Trim();
        _filial.Cnpj     = string.IsNullOrWhiteSpace(txtCnpj.Text) ? null : txtCnpj.Text.Trim();
        _filial.Telefone = string.IsNullOrWhiteSpace(txtTelefone.Text) ? null : txtTelefone.Text.Trim();
        _filial.Endereco = string.IsNullOrWhiteSpace(txtEndereco.Text) ? null : txtEndereco.Text.Trim();
        _filial.IsMatriz = chkMatriz.Checked;
        _filial.Ativo    = chkAtivo.Checked;

        var res = await _svc.SalvarAsync(_filial);
        if (!res.Sucesso)
        {
            Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
