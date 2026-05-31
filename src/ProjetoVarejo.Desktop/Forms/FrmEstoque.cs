using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmEstoque : Form
{
    private readonly IEstoqueService _estoque;
    private readonly IProdutoService _produtos;
    private readonly FornecedorService _fornecedores;
    private TabControl tabs = null!;
    private StyledGrid gridMov = null!;
    private StyledGrid gridMinimo = null!;
    private DateTimePicker dtDe = null!, dtAte = null!;
    private TextBox txtFiltroProd = null!;

    public FrmEstoque(IEstoqueService estoque, IProdutoService produtos, FornecedorService fornecedores)
    {
        _estoque = estoque;
        _produtos = produtos;
        _fornecedores = fornecedores;
        InitUi();
        Shown += async (s, e) => { await CarregarMovimentosAsync(); await CarregarMinimoAsync(); };
    }

    private void InitUi()
    {
        Text = "Estoque";
        Size = new Size(1200, 720);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Estoque", "Movimentações de entrada/saída e produtos em alerta");

        var pnlAcoes = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Tema.CorFundo, Padding = new Padding(0, 0, 0, 12) };
        var btnEntrada = Botoes.Sucesso("Lançar entrada", 200, 40);
        btnEntrada.Click += async (s, e) => await LancarAsync(TipoMovimentoEstoque.Entrada);
        var btnSaida = Botoes.Perigo("Ajustar saída", 180, 40);
        btnSaida.Click += async (s, e) => await LancarAsync(TipoMovimentoEstoque.AjusteSaida);
        var fl = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, BackColor = Tema.CorFundo };
        Botoes.ParaPainelToolbar(fl, btnEntrada, btnSaida);
        fl.Controls.Add(btnEntrada);
        fl.Controls.Add(btnSaida);
        pnlAcoes.Controls.Add(fl);

        tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamily, 10),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(200, 40),
            Appearance = TabAppearance.Normal
        };
        EstilizarTabs(tabs);

        var tpMov = new TabPage("Movimentações") { BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };

        var filtrosMov = new Card { Dock = DockStyle.Top, Height = 80, Padding = new Padding(16) };
        var pnlFiltros = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        pnlFiltros.Controls.Add(Inputs.Rotulo("DE", 0, 0));
        dtDe = new DateTimePicker { Left = 0, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Font = Tema.FontCorpo };
        pnlFiltros.Controls.Add(dtDe);
        pnlFiltros.Controls.Add(Inputs.Rotulo("ATÉ", 145, 0));
        dtAte = new DateTimePicker { Left = 145, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1), Font = Tema.FontCorpo };
        pnlFiltros.Controls.Add(dtAte);
        pnlFiltros.Controls.Add(Inputs.Rotulo("PRODUTO", 290, 0));
        txtFiltroProd = new TextBox { Left = 290, Top = 18, Width = 280, Height = 28, Font = Tema.FontCorpo, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "código ou descrição..." };
        pnlFiltros.Controls.Add(txtFiltroProd);
        var btnFiltrar = Botoes.Primario("Filtrar", 100, 32);
        btnFiltrar.Top = 18; btnFiltrar.Left = 585;
        btnFiltrar.Click += async (s, e) => await CarregarMovimentosAsync();
        pnlFiltros.Controls.Add(btnFiltrar);
        filtrosMov.Controls.Add(pnlFiltros);

        var cardGridMov = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        gridMov = new StyledGrid();
        gridMov.Columns.Add("Data", "Data");
        gridMov.Columns.Add("Tipo", "Tipo");
        gridMov.Columns.Add("Produto", "Produto");
        gridMov.Columns.Add("Quantidade", "Qtd");
        gridMov.Columns.Add("SaldoAnt", "Saldo anterior");
        gridMov.Columns.Add("SaldoAtual", "Saldo atual");
        gridMov.Columns.Add("Documento", "Documento");
        gridMov.Columns.Add("Usuario", "Usuário");
        gridMov.Columns["Produto"]!.FillWeight = 220;
        gridMov.Columns["Quantidade"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        gridMov.Columns["SaldoAnt"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        gridMov.Columns["SaldoAtual"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        cardGridMov.Controls.Add(gridMov);

        tpMov.Controls.Add(cardGridMov);
        tpMov.Controls.Add(filtrosMov);

        var tpMin = new TabPage("Abaixo do mínimo") { BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };
        var cardGridMin = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        gridMinimo = new StyledGrid();
        gridMinimo.Columns.Add("Codigo", "Código");
        gridMinimo.Columns.Add("Descricao", "Descrição");
        gridMinimo.Columns.Add("Estoque", "Estoque atual");
        gridMinimo.Columns.Add("Minimo", "Estoque mínimo");
        gridMinimo.Columns["Descricao"]!.FillWeight = 300;
        gridMinimo.Columns["Estoque"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        gridMinimo.Columns["Minimo"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        cardGridMin.Controls.Add(gridMinimo);
        tpMin.Controls.Add(cardGridMin);

        tabs.TabPages.Add(tpMov);
        tabs.TabPages.Add(tpMin);

        Controls.Add(tabs);
        Controls.Add(pnlAcoes);
        Controls.Add(header);
    }

    private void EstilizarTabs(TabControl tc)
    {
        Abas.Modernizar(tc);
    }

    private async Task CarregarMovimentosAsync()
    {
        var movs = await _estoque.ListarMovimentosAsync(null, dtDe.Value.Date, dtAte.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(txtFiltroProd.Text))
            movs = movs.Where(m => m.Produto.Descricao.Contains(txtFiltroProd.Text, StringComparison.OrdinalIgnoreCase)
                                || m.Produto.Codigo.Contains(txtFiltroProd.Text, StringComparison.OrdinalIgnoreCase))
                       .ToList();
        gridMov.Rows.Clear();
        foreach (var m in movs)
        {
            int idx = gridMov.Rows.Add(
                m.CriadoEm.ToString("dd/MM/yyyy HH:mm"),
                m.Tipo.ToString(),
                m.Produto.Descricao,
                m.Quantidade.ToString("N3"),
                m.SaldoAnterior.ToString("N3"),
                m.SaldoAtual.ToString("N3"),
                m.Documento ?? "",
                m.Usuario.Nome);
            var tipoCell = gridMov.Rows[idx].Cells["Tipo"];
            tipoCell.Style.Font = Tema.FontCorpoBold;
            bool entrada = m.Tipo is TipoMovimentoEstoque.Entrada or TipoMovimentoEstoque.AjusteEntrada or TipoMovimentoEstoque.Devolucao;
            tipoCell.Style.ForeColor = entrada ? Tema.CorSucesso : Tema.CorErro;
        }
    }

    private async Task CarregarMinimoAsync()
    {
        var produtos = await _estoque.ProdutosAbaixoMinimoAsync();
        gridMinimo.Rows.Clear();
        foreach (var p in produtos)
        {
            int idx = gridMinimo.Rows.Add(p.Codigo, p.Descricao, p.Estoque.ToString("N3"), p.EstoqueMinimo.ToString("N3"));
            gridMinimo.Rows[idx].DefaultCellStyle.BackColor = Tema.CorAlertaSoft;
        }
    }

    private async Task LancarAsync(TipoMovimentoEstoque tipo)
    {
        using var dlg = new FrmLancamentoEstoque(tipo, _estoque, _produtos, _fornecedores);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await CarregarMovimentosAsync();
            await CarregarMinimoAsync();
            Toast.Mostrar("Movimento registrado.", TipoToast.Sucesso, owner: this);
        }
    }
}

public class FrmLancamentoEstoque : Form
{
    private readonly TipoMovimentoEstoque _tipo;
    private readonly IEstoqueService _estoque;
    private readonly IProdutoService _produtos;
    private readonly FornecedorService _fornecedores;
    private TextBox txtCodigo = null!, txtQtd = null!, txtCusto = null!, txtDoc = null!, txtObs = null!;
    private ComboBox cboForn = null!;
    private Label lblProduto = null!;
    private Produto? _selecionado;

    public FrmLancamentoEstoque(TipoMovimentoEstoque tipo, IEstoqueService estoque, IProdutoService produtos, FornecedorService fornecedores)
    {
        _tipo = tipo; _estoque = estoque; _produtos = produtos; _fornecedores = fornecedores;
        InitUi();
        if (tipo == TipoMovimentoEstoque.Entrada) Shown += async (s, e) => await CarregarFornecedoresAsync();
    }

    private void InitUi()
    {
        Text = _tipo == TipoMovimentoEstoque.Entrada ? "Lançar entrada" : "Ajustar saída";
        Size = new Size(640, 540);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderModal(Text);
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;
        txtCodigo = Inputs.CampoTexto(pnl, "Código / código de barras*", 0, y, 280);
        txtCodigo.Leave += async (s, e) => await BuscarProdutoAsync();
        txtCodigo.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await BuscarProdutoAsync(); };
        lblProduto = new Label
        {
            Left = 300, Top = y + 22, Width = 260, Height = 24,
            Font = new Font(Tema.FontFamily, 10, FontStyle.Italic),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent,
            Text = "(nenhum)",
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnl.Controls.Add(lblProduto);
        y += 60;

        txtQtd = Inputs.CampoTexto(pnl, "Quantidade*", 0, y, 200, right: true);
        txtQtd.Text = "1";
        if (_tipo == TipoMovimentoEstoque.Entrada)
            txtCusto = Inputs.CampoTexto(pnl, "Custo unitário (R$)", 220, y, 200, right: true);
        else
            txtCusto = new TextBox();
        y += 60;

        if (_tipo == TipoMovimentoEstoque.Entrada)
        {
            cboForn = Inputs.CampoCombo(pnl, "Fornecedor", 0, y, 560);
            y += 60;
        }
        else
        {
            cboForn = new ComboBox();
        }

        txtDoc = Inputs.CampoTexto(pnl, "Documento / observação curta", 0, y, 560); y += 60;
        Inputs.Rotulo("OBSERVAÇÃO", 0, y);
        pnl.Controls.Add(Inputs.Rotulo("OBSERVAÇÃO", 0, y));
        txtObs = new TextBox
        {
            Left = 0, Top = y + 20, Width = 560, Height = 70, Multiline = true,
            Font = Tema.FontCorpo, BorderStyle = BorderStyle.FixedSingle
        };
        pnl.Controls.Add(txtObs);

        card.Controls.Add(pnl);

        var (rodape, btnOk, btnCanc) = Inputs.RodapeSalvarCancelar("Lançar");
        btnOk.BackColor = _tipo == TipoMovimentoEstoque.Entrada ? Tema.CorSucesso : Tema.CorErro;
        btnOk.FlatAppearance.MouseOverBackColor = Botoes.Misturar(btnOk.BackColor, Color.Black, 0.10f);
        btnOk.Click += async (s, e) => await LancarAsync();
        btnCanc.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        AcceptButton = btnOk;
        CancelButton = btnCanc;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);
    }

    private async Task CarregarFornecedoresAsync()
    {
        cboForn.Items.Clear();
        cboForn.Items.Add("(nenhum)");
        foreach (var f in await _fornecedores.ListarAsync()) cboForn.Items.Add(f);
        cboForn.SelectedIndex = 0;
    }

    private async Task BuscarProdutoAsync()
    {
        if (string.IsNullOrWhiteSpace(txtCodigo.Text)) return;
        _selecionado = await _produtos.BuscarPorCodigoAsync(txtCodigo.Text.Trim());
        lblProduto.Text = _selecionado != null ? _selecionado.Descricao : "(não encontrado)";
        lblProduto.ForeColor = _selecionado != null ? Tema.CorSucesso : Tema.CorErro;
        lblProduto.Font = new Font(Tema.FontFamily, 10, FontStyle.Bold);
    }

    private async Task LancarAsync()
    {
        if (_selecionado == null) { Toast.Mostrar("Produto não selecionado.", TipoToast.Erro, owner: this); return; }
        if (!decimal.TryParse(txtQtd.Text.Replace('.', ','), out var qtd) || qtd <= 0)
        { Toast.Mostrar("Quantidade inválida.", TipoToast.Erro, owner: this); return; }
        decimal? custo = null;
        if (_tipo == TipoMovimentoEstoque.Entrada && !string.IsNullOrWhiteSpace(txtCusto.Text))
        {
            if (!decimal.TryParse(txtCusto.Text.Replace('.', ','), out var c))
            { Toast.Mostrar("Custo inválido.", TipoToast.Erro, owner: this); return; }
            custo = c;
        }
        int? fornId = cboForn.SelectedItem is Fornecedor f ? f.Id : null;
        var res = await _estoque.RegistrarMovimentoAsync(_selecionado.Id, _tipo, qtd, custo,
            string.IsNullOrWhiteSpace(txtDoc.Text) ? null : txtDoc.Text.Trim(),
            null, fornId, string.IsNullOrWhiteSpace(txtObs.Text) ? null : txtObs.Text.Trim());
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        DialogResult = DialogResult.OK;
        Close();
    }
}
