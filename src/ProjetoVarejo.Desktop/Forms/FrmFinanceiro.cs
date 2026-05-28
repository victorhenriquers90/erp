using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Domain.Enums;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

[ModuloRequerido(ModuloSistema.Financeiro)]
public class FrmFinanceiro : Form
{
    private readonly IFinanceiroService _svc;
    private readonly ClienteService _clientes;
    private readonly FornecedorService _fornecedores;
    private ComboBox cboTipo = null!, cboStatus = null!;
    private DateTimePicker dtDe = null!, dtAte = null!;
    private StyledGrid grid = null!;
    private FlowLayoutPanel _kpis = null!;

    public FrmFinanceiro(IFinanceiroService svc, ClienteService clientes, FornecedorService fornecedores)
    {
        _svc = svc; _clientes = clientes; _fornecedores = fornecedores;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Financeiro";
        Size = new Size(1250, 750);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Financeiro", "Contas a pagar e a receber");

        _kpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 170,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Tema.CorFundo,
            Padding = new Padding(0, 4, 0, 12)
        };

        var filtros = new Card { Dock = DockStyle.Top, Height = 80, Padding = new Padding(16) };
        var pnlFiltros = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        Inputs.Rotulo("TIPO", 0, 0); pnlFiltros.Controls.Add(Inputs.Rotulo("TIPO", 0, 0));
        cboTipo = new ComboBox { Left = 0, Top = 18, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, Font = Tema.FontCorpo, FlatStyle = FlatStyle.Flat };
        cboTipo.Items.Add("Todos");
        foreach (TipoConta t in Enum.GetValues(typeof(TipoConta))) cboTipo.Items.Add(t);
        cboTipo.SelectedIndex = 0;
        pnlFiltros.Controls.Add(cboTipo);

        pnlFiltros.Controls.Add(Inputs.Rotulo("STATUS", 160, 0));
        cboStatus = new ComboBox { Left = 160, Top = 18, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Font = Tema.FontCorpo, FlatStyle = FlatStyle.Flat };
        cboStatus.Items.Add("Todos");
        foreach (StatusConta s in Enum.GetValues(typeof(StatusConta))) cboStatus.Items.Add(s);
        cboStatus.SelectedIndex = 0;
        pnlFiltros.Controls.Add(cboStatus);

        pnlFiltros.Controls.Add(Inputs.Rotulo("DE", 340, 0));
        dtDe = new DateTimePicker { Left = 340, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Font = Tema.FontCorpo };
        pnlFiltros.Controls.Add(dtDe);

        pnlFiltros.Controls.Add(Inputs.Rotulo("ATÉ", 485, 0));
        dtAte = new DateTimePicker { Left = 485, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(60), Font = Tema.FontCorpo };
        pnlFiltros.Controls.Add(dtAte);

        var btnFiltrar = Botoes.Primario("Filtrar", 110, 32);
        btnFiltrar.Top = 18; btnFiltrar.Left = 635;
        btnFiltrar.Click += async (s, e) => await CarregarAsync();
        pnlFiltros.Controls.Add(btnFiltrar);

        var btnQuitar = Botoes.Sucesso("Quitar", 110, 32);
        btnQuitar.Top = 18; btnQuitar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnQuitar.Click += async (s, e) => await QuitarSelAsync();
        var btnNovo = Botoes.Info("Nova conta", 130, 32);
        btnNovo.Top = 18; btnNovo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnNovo.Click += (s, e) => Editar(null);
        pnlFiltros.Controls.Add(btnNovo);
        pnlFiltros.Controls.Add(btnQuitar);
        pnlFiltros.Resize += (s, e) =>
        {
            btnQuitar.Left = pnlFiltros.Width - 120;
            btnNovo.Left = pnlFiltros.Width - 260;
        };

        filtros.Controls.Add(pnlFiltros);

        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("Tipo", "Tipo");
        grid.Columns.Add("Descricao", "Descrição");
        grid.Columns.Add("Pessoa", "Cliente/Fornecedor");
        grid.Columns.Add("Vencimento", "Vencimento");
        grid.Columns.Add("Valor", "Valor");
        grid.Columns.Add("Pago", "Pago");
        grid.Columns.Add("Status", "Status");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns["Descricao"]!.FillWeight = 240;
        grid.Columns["Valor"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["Pago"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.DoubleClick += (s, e) => EditarSel();
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(filtros);
        Controls.Add(_kpis);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        TipoConta? tipo = cboTipo.SelectedItem is TipoConta t ? t : null;
        StatusConta? status = cboStatus.SelectedItem is StatusConta s ? s : null;

        var lista = await _svc.ListarAsync(tipo, status, dtDe.Value.Date, dtAte.Value.Date.AddDays(1));
        grid.Rows.Clear();
        foreach (var c in lista)
        {
            var pessoa = c.Cliente?.Nome ?? c.Fornecedor?.RazaoSocial ?? "";
            int idx = grid.Rows.Add(c.Id, c.Tipo, c.Descricao, pessoa,
                c.DataVencimento.ToString("dd/MM/yyyy"),
                c.Valor.ToString("N2"), c.ValorPago.ToString("N2"), c.Status.ToString());

            var statusCell = grid.Rows[idx].Cells["Status"];
            statusCell.Style.Font = Tema.FontCorpoBold;
            statusCell.Style.ForeColor = c.Status switch
            {
                StatusConta.Paga => Tema.CorSucesso,
                StatusConta.Atrasada => Tema.CorErro,
                StatusConta.Cancelada => Tema.CorNeutro,
                _ => Tema.CorAlerta
            };

            var tipoCell = grid.Rows[idx].Cells["Tipo"];
            tipoCell.Style.ForeColor = c.Tipo == TipoConta.Receber ? Tema.CorSucesso : Tema.CorErro;
            tipoCell.Style.Font = Tema.FontCorpoBold;
        }

        var (rec, pag, saldo) = await _svc.ResumoAsync(dtDe.Value.Date, dtAte.Value.Date.AddDays(1));
        _kpis.Controls.Clear();
        _kpis.Controls.Add(new KpiCard("A Receber", rec.ToString("C"), Tema.IconArrowDown, Tema.CorSucesso));
        _kpis.Controls.Add(new KpiCard("A Pagar", pag.ToString("C"), Tema.IconArrowUp, Tema.CorErro));
        _kpis.Controls.Add(new KpiCard("Saldo previsto", saldo.ToString("C"), Tema.IconFinanceiro, saldo >= 0 ? Tema.CorPrimaria : Tema.CorErro));
        _kpis.Controls.Add(new KpiCard("Movimentações", lista.Count.ToString(), Tema.IconNotas, Tema.CorInfo));
    }

    private void EditarSel()
    {
        if (grid.SelectedRows.Count == 0) return;
        Editar((int)grid.SelectedRows[0].Cells["Id"].Value);
    }

    private async void Editar(int? id)
    {
        try
        {
            ContaFinanceira c;
            if (id.HasValue)
            {
                var lista = await _svc.ListarAsync();
                c = lista.FirstOrDefault(x => x.Id == id.Value) ?? new ContaFinanceira { DataVencimento = DateTime.Today.AddDays(30) };
            }
            else
            {
                c = new ContaFinanceira { DataVencimento = DateTime.Today.AddDays(30), Tipo = TipoConta.Pagar };
            }
            using var dlg = new FrmContaEdit(c, _svc, _clientes, _fornecedores);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await CarregarAsync();
                Toast.Mostrar("Conta salva.", TipoToast.Sucesso, owner: this);
            }
        }
        catch (Exception ex)
        {
            Toast.Mostrar(ex.Message, TipoToast.Erro, owner: this);
        }
    }

    private async Task QuitarSelAsync()
    {
        if (grid.SelectedRows.Count == 0) { Toast.Mostrar("Selecione uma conta.", TipoToast.Info, owner: this); return; }
        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
        using var dlg = new FrmQuitacao(id, _svc);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await CarregarAsync();
            Toast.Mostrar("Conta quitada.", TipoToast.Sucesso, owner: this);
        }
    }
}

public class FrmContaEdit : Form
{
    private readonly ContaFinanceira _c;
    private readonly IFinanceiroService _svc;
    private readonly ClienteService _clientes;
    private readonly FornecedorService _fornecedores;
    private ComboBox cboTipo = null!;
    private TextBox txtDescricao = null!, txtValor = null!, txtDoc = null!, txtObs = null!;
    private DateTimePicker dtVenc = null!, dtEmissao = null!;
    private ComboBox cboCliente = null!, cboFornecedor = null!;

    public FrmContaEdit(ContaFinanceira c, IFinanceiroService svc, ClienteService clientes, FornecedorService fornecedores)
    {
        _c = c; _svc = svc; _clientes = clientes; _fornecedores = fornecedores;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = _c.Id == 0 ? "Nova conta" : "Editar conta";
        Size = new Size(700, 640);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderModal(Text);
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;
        Inputs.Secao(pnl, "Dados da conta", ref y);
        cboTipo = Inputs.CampoCombo(pnl, "Tipo*", 0, y, 200);
        foreach (TipoConta t in Enum.GetValues(typeof(TipoConta))) cboTipo.Items.Add(t);
        txtValor = Inputs.CampoTexto(pnl, "Valor (R$)*", 220, y, 200, right: true);
        y += 60;
        txtDescricao = Inputs.CampoTexto(pnl, "Descrição*", 0, y, 620); y += 60;
        txtDoc = Inputs.CampoTexto(pnl, "Documento / Nota", 0, y, 300); y += 60;

        dtEmissao = Inputs.CampoData(pnl, "Data emissão", 0, y, 200, DateTime.Today);
        dtVenc = Inputs.CampoData(pnl, "Vencimento*", 220, y, 200, DateTime.Today.AddDays(30));
        y += 60;

        Inputs.Secao(pnl, "Vinculação", ref y);
        cboCliente = Inputs.CampoCombo(pnl, "Cliente", 0, y, 300);
        cboFornecedor = Inputs.CampoCombo(pnl, "Fornecedor", 320, y, 300);
        y += 60;

        Inputs.Rotulo("OBSERVAÇÃO", 0, y);
        pnl.Controls.Add(Inputs.Rotulo("OBSERVAÇÃO", 0, y));
        txtObs = new TextBox
        {
            Left = 0, Top = y + 20, Width = 620, Height = 70,
            Multiline = true, Font = Tema.FontCorpo,
            BorderStyle = BorderStyle.FixedSingle
        };
        pnl.Controls.Add(txtObs);

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

    private async Task CarregarAsync()
    {
        cboCliente.Items.Clear(); cboCliente.Items.Add("(nenhum)");
        foreach (var c in await _clientes.ListarAsync()) cboCliente.Items.Add(c);
        cboFornecedor.Items.Clear(); cboFornecedor.Items.Add("(nenhum)");
        foreach (var f in await _fornecedores.ListarAsync()) cboFornecedor.Items.Add(f);

        cboTipo.SelectedItem = _c.Tipo;
        txtDescricao.Text = _c.Descricao;
        txtDoc.Text = _c.DocumentoNumero ?? "";
        dtEmissao.Value = _c.DataEmissao == default ? DateTime.Today : _c.DataEmissao;
        dtVenc.Value = _c.DataVencimento == default ? DateTime.Today.AddDays(30) : _c.DataVencimento;
        txtValor.Text = _c.Valor.ToString("N2");
        cboCliente.SelectedIndex = 0;
        cboFornecedor.SelectedIndex = 0;
        if (_c.ClienteId.HasValue)
            for (int i = 1; i < cboCliente.Items.Count; i++)
                if (((Cliente)cboCliente.Items[i]!).Id == _c.ClienteId) { cboCliente.SelectedIndex = i; break; }
        if (_c.FornecedorId.HasValue)
            for (int i = 1; i < cboFornecedor.Items.Count; i++)
                if (((Fornecedor)cboFornecedor.Items[i]!).Id == _c.FornecedorId) { cboFornecedor.SelectedIndex = i; break; }
        txtObs.Text = _c.Observacao ?? "";
    }

    private async Task SalvarAsync()
    {
        _c.Tipo = (TipoConta)cboTipo.SelectedItem!;
        _c.Descricao = txtDescricao.Text.Trim();
        _c.DocumentoNumero = txtDoc.Text.Trim();
        _c.DataEmissao = dtEmissao.Value;
        _c.DataVencimento = dtVenc.Value;
        _c.Valor = decimal.TryParse(txtValor.Text.Replace('.', ','), out var v) ? v : 0;
        _c.ClienteId = cboCliente.SelectedItem is Cliente cl ? cl.Id : null;
        _c.FornecedorId = cboFornecedor.SelectedItem is Fornecedor fo ? fo.Id : null;
        _c.Observacao = txtObs.Text.Trim();
        var res = await _svc.SalvarAsync(_c);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        DialogResult = DialogResult.OK;
        Close();
    }
}

public class FrmQuitacao : Form
{
    private readonly int _contaId;
    private readonly IFinanceiroService _svc;
    private DateTimePicker dtPag = null!;
    private TextBox txtValor = null!, txtJuros = null!, txtMulta = null!, txtDesc = null!;
    private ComboBox cboForma = null!;

    public FrmQuitacao(int contaId, IFinanceiroService svc)
    {
        _contaId = contaId; _svc = svc;
        InitUi();
    }

    private void InitUi()
    {
        Text = "Quitar conta";
        Size = new Size(560, 480);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderModal("Quitação de Conta");
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;
        dtPag = Inputs.CampoData(pnl, "Data do pagamento*", 0, y, 200, DateTime.Today);
        cboForma = Inputs.CampoCombo(pnl, "Forma de pagamento*", 220, y, 260);
        foreach (FormaPagamentoTipo f in Enum.GetValues(typeof(FormaPagamentoTipo))) cboForma.Items.Add(f);
        cboForma.SelectedIndex = 0;
        y += 60;
        txtValor = Inputs.CampoTexto(pnl, "Valor pago (R$)*", 0, y, 200, right: true); y += 60;
        txtJuros = Inputs.CampoTexto(pnl, "Juros", 0, y, 150, right: true);
        txtMulta = Inputs.CampoTexto(pnl, "Multa", 170, y, 150, right: true);
        txtDesc = Inputs.CampoTexto(pnl, "Desconto", 340, y, 150, right: true);
        txtJuros.Text = "0,00"; txtMulta.Text = "0,00"; txtDesc.Text = "0,00";

        card.Controls.Add(pnl);

        var (rodape, btnOk, btnCanc) = Inputs.RodapeSalvarCancelar("Confirmar quitação");
        btnOk.BackColor = Tema.CorSucesso;
        btnOk.FlatAppearance.MouseOverBackColor = Botoes.Misturar(Tema.CorSucesso, Color.Black, 0.10f);
        btnOk.Click += async (s, e) => await QuitarAsync();
        btnCanc.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        AcceptButton = btnOk;
        CancelButton = btnCanc;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);
    }

    private async Task QuitarAsync()
    {
        var v = decimal.TryParse(txtValor.Text.Replace('.', ','), out var vp) ? vp : 0;
        var j = decimal.TryParse(txtJuros.Text.Replace('.', ','), out var jv) ? jv : 0;
        var m = decimal.TryParse(txtMulta.Text.Replace('.', ','), out var mv) ? mv : 0;
        var d = decimal.TryParse(txtDesc.Text.Replace('.', ','), out var dv) ? dv : 0;
        if (v <= 0) { Toast.Mostrar("Valor inválido.", TipoToast.Erro, owner: this); return; }
        var res = await _svc.QuitarAsync(_contaId, dtPag.Value, v, (FormaPagamentoTipo)cboForma.SelectedItem!, j, m, d);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        DialogResult = DialogResult.OK;
        Close();
    }
}
