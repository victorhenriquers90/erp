using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmProdutos : Form
{
    private readonly ProdutoService _produtos;
    private readonly CategoriaService _categorias;
    private TextBox txtBusca = null!;
    private StyledGrid grid = null!;
    private Label lblTotal = null!;

    public FrmProdutos(ProdutoService produtos, CategoriaService categorias)
    {
        _produtos = produtos;
        _categorias = categorias;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Produtos";
        Size = new Size(1200, 700);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Produtos", "Gerenciar catalogo de produtos");
        lblTotal = Inputs.SubtituloHeader(header);

        // === Toolbar ===
        var toolbar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.CorFundo, Padding = new Padding(0, 10, 0, 10) };

        var pnlBusca = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        pnlBusca.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = Tema.PathArredondado(new Rectangle(0, 0, pnlBusca.Width - 1, pnlBusca.Height - 1), 6);
            using var brush = new SolidBrush(Tema.CorCard);
            g.FillPath(brush, path);
            using var pen = new Pen(Tema.CorBorda, 1);
            g.DrawPath(pen, path);
        };
        var lblBuscaIcone = new Label
        {
            Text = Tema.IconBusca,
            Dock = DockStyle.Left, Width = 40,
            Font = new Font("Segoe MDL2 Assets", 12),
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        txtBusca = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font(Tema.FontFamily, 10),
            BackColor = Tema.CorCard,
            PlaceholderText = "Buscar por código, código de barras ou descrição..."
        };
        txtBusca.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await CarregarAsync(); };
        var inner = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard, Padding = new Padding(0, 12, 14, 0) };
        inner.Controls.Add(txtBusca);
        pnlBusca.Controls.Add(inner);
        pnlBusca.Controls.Add(lblBuscaIcone);

        var pnlBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 390,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Tema.CorFundo
        };
        var btnNovo = Botoes.PrimarioIcone("Novo produto", "\uE710", 160, 40);
        btnNovo.Font = new Font(Tema.FontFamily, 9, FontStyle.Bold);
        btnNovo.Click += (s, e) => EditarProduto(null);
        var btnEditar = Botoes.GhostIcone("Editar", "\uE70F", 96, 40);
        btnEditar.Click += (s, e) => EditarSelecionado();
        var btnExcluir = Botoes.GhostIcone("Excluir", "\uE74D", 104, 40, Tema.CorErro);
        btnExcluir.Click += async (s, e) => await ExcluirSelecionadoAsync();
        Botoes.ParaToolbar(btnNovo, btnEditar, btnExcluir);
        pnlBotoes.Controls.Add(btnNovo);
        pnlBotoes.Controls.Add(btnEditar);
        pnlBotoes.Controls.Add(btnExcluir);

        toolbar.Controls.Add(pnlBusca);
        toolbar.Controls.Add(pnlBotoes);

        // === Grid card ===
        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("Codigo", "Código");
        grid.Columns.Add("CodigoBarras", "Cód. Barras");
        grid.Columns.Add("Descricao", "Descrição");
        grid.Columns.Add("Categoria", "Categoria");
        grid.Columns.Add("Un", "Un");
        grid.Columns.Add("Preco", "Preço");
        grid.Columns.Add("Estoque", "Estoque");
        var colStatus = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "" };
        grid.Columns.Add(colStatus);
        grid.Columns["Id"]!.Visible = false;
        grid.Columns["Descricao"]!.FillWeight = 250;
        grid.Columns["Preco"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["Estoque"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.DoubleClick += (s, e) => EditarSelecionado();
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        var lista = await _produtos.ListarAsync(txtBusca.Text);
        grid.Rows.Clear();
        foreach (var p in lista)
        {
            string status = !p.Ativo ? "Inativo"
                : (p.ControlaEstoque && p.Estoque <= p.EstoqueMinimo) ? "Estoque baixo"
                : "Ativo";
            int idx = grid.Rows.Add(p.Id, p.Codigo, p.CodigoBarras, p.Descricao,
                p.Categoria?.Nome, p.Unidade, p.PrecoVenda.ToString("N2"),
                p.Estoque.ToString("N3"), status);

            // Cor da célula de status
            var cell = grid.Rows[idx].Cells["Status"];
            cell.Style.ForeColor = status switch
            {
                "Inativo" => Tema.CorTextoMedio,
                "Estoque baixo" => Tema.CorAlerta,
                _ => Tema.CorSucesso
            };
            cell.Style.Font = Tema.FontCorpoBold;
        }
        lblTotal.Text = $"{lista.Count} produto(s) listado(s)";
    }

    private void EditarSelecionado()
    {
        if (grid.SelectedRows.Count == 0)
        {
            Toast.Mostrar("Selecione um produto.", TipoToast.Info, owner: this);
            return;
        }
        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
        EditarProduto(id);
    }

    private async void EditarProduto(int? id)
    {
        Produto? produto = id.HasValue ? await _produtos.BuscarPorIdAsync(id.Value) : new Produto();
        if (produto == null) return;
        using var dlg = new FrmProdutoEdit(produto, _produtos, _categorias);
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            await CarregarAsync();
            Toast.Mostrar("Produto salvo com sucesso.", TipoToast.Sucesso, owner: this);
        }
    }

    private async Task ExcluirSelecionadoAsync()
    {
        if (grid.SelectedRows.Count == 0)
        {
            Toast.Mostrar("Selecione um produto.", TipoToast.Info, owner: this);
            return;
        }
        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
        if (MessageBox.Show("Confirma exclusão?", "Confirmação",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var res = await _produtos.ExcluirAsync(id);
        if (!res.Sucesso)
        {
            Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this);
            return;
        }
        await CarregarAsync();
        Toast.Mostrar("Produto excluído.", TipoToast.Sucesso, owner: this);
    }
}

public class FrmProdutoEdit : Form
{
    private readonly Produto _produto;
    private readonly ProdutoService _produtos;
    private readonly CategoriaService _categorias;

    private TextBox txtCodigo = null!, txtBarras = null!, txtDescricao = null!;
    private ComboBox cboCategoria = null!, cboUnidade = null!;
    private TextBox txtCusto = null!, txtVenda = null!, txtEstoque = null!, txtMinimo = null!;
    private TextBox txtNcm = null!, txtCfop = null!, txtCstIcms = null!, txtAliqIcms = null!;
    private CheckBox chkAtivo = null!, chkControla = null!;

    public FrmProdutoEdit(Produto produto, ProdutoService produtos, CategoriaService categorias)
    {
        _produto = produto;
        _produtos = produtos;
        _categorias = categorias;
        InitUi();
        Shown += async (s, e) => await PreencherAsync();
    }

    private void InitUi()
    {
        Text = _produto.Id == 0 ? "Novo Produto" : $"Editar Produto #{_produto.Id}";
        Size = new Size(780, 680);
        MinimumSize = new Size(740, 620);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;

        var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Tema.CorFundo };
        var lblTitulo = new Label
        {
            Text = Text,
            Dock = DockStyle.Fill,
            Font = Tema.FontTituloGrande,
            ForeColor = Tema.CorTextoEscuro,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 10, 0, 0)
        };
        header.Controls.Add(lblTitulo);

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(18) };

        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard, AutoScroll = true };

        int y = 0;
        AddSecao(pnl, "Identificação", ref y);
        AddCampo(pnl, "Código*", out txtCodigo, 10, y, 200); AddCampo(pnl, "Cód. Barras", out txtBarras, 240, y, 240); y += 65;
        AddCampo(pnl, "Descrição*", out txtDescricao, 10, y, 580); y += 65;

        cboCategoria = AddCombo(pnl, "Categoria", 10, y, 280);
        cboUnidade = AddCombo(pnl, "Unidade", 310, y, 180);
        foreach (UnidadeMedida u in Enum.GetValues(typeof(UnidadeMedida))) cboUnidade.Items.Add(u);
        y += 65;

        AddSecao(pnl, "Preço e estoque", ref y);
        AddCampo(pnl, "Custo (R$)", out txtCusto, 10, y, 130, alignRight: true);
        AddCampo(pnl, "Venda (R$)", out txtVenda, 160, y, 130, alignRight: true);
        AddCampo(pnl, "Estoque", out txtEstoque, 310, y, 130, alignRight: true);
        AddCampo(pnl, "Estq. mínimo", out txtMinimo, 460, y, 130, alignRight: true);
        y += 65;

        AddSecao(pnl, "Dados fiscais", ref y);
        AddCampo(pnl, "NCM", out txtNcm, 10, y, 140);
        AddCampo(pnl, "CFOP", out txtCfop, 160, y, 100);
        AddCampo(pnl, "CST ICMS", out txtCstIcms, 270, y, 100);
        AddCampo(pnl, "Alíq. ICMS %", out txtAliqIcms, 380, y, 130, alignRight: true);
        y += 65;

        chkAtivo = new CheckBox { Text = "Produto ativo", Left = 10, Top = y, Width = 160, Font = Tema.FontCorpo, Checked = true, BackColor = Color.Transparent };
        chkControla = new CheckBox { Text = "Controlar estoque", Left = 180, Top = y, Width = 200, Font = Tema.FontCorpo, Checked = true, BackColor = Color.Transparent };
        pnl.Controls.Add(chkAtivo);
        pnl.Controls.Add(chkControla);
        pnl.AutoScrollMinSize = new Size(660, y + 60);

        card.Controls.Add(pnl);

        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Tema.CorFundo, Padding = new Padding(0, 15, 20, 15) };
        var btnSalvar = Botoes.Primario("Salvar (F10)", 160, 40);
        btnSalvar.Dock = DockStyle.Right;
        btnSalvar.Click += async (s, e) => await SalvarAsync();
        var btnCancelar = Botoes.Ghost("Cancelar", 130, 40);
        btnCancelar.Dock = DockStyle.Right;
        btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        var spacer = new Panel { Dock = DockStyle.Right, Width = 10 };
        rodape.Controls.Add(btnSalvar);
        rodape.Controls.Add(spacer);
        rodape.Controls.Add(btnCancelar);

        AcceptButton = btnSalvar;
        CancelButton = btnCancelar;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);
    }

    private static void AddSecao(Control parent, string txt, ref int y)
    {
        parent.Controls.Add(new Label
        {
            Text = txt.ToUpper(),
            Left = 10, Top = y, Width = 600, Height = 22,
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        });
        var sep = new Panel { Left = 10, Top = y + 22, Width = 580, Height = 1, BackColor = Tema.CorBorda };
        parent.Controls.Add(sep);
        y += 32;
    }

    private static void AddCampo(Control parent, string label, out TextBox tb, int left, int top, int width, bool alignRight = false)
    {
        parent.Controls.Add(new Label
        {
            Text = label,
            Left = left, Top = top, Width = width, Height = 18,
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        });
        tb = new TextBox
        {
            Left = left, Top = top + 20, Width = width, Height = 28,
            Font = new Font(Tema.FontFamily, 10),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Tema.Branco,
            TextAlign = alignRight ? HorizontalAlignment.Right : HorizontalAlignment.Left
        };
        parent.Controls.Add(tb);
    }

    private static ComboBox AddCombo(Control parent, string label, int left, int top, int width)
    {
        parent.Controls.Add(new Label
        {
            Text = label,
            Left = left, Top = top, Width = width, Height = 18,
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        });
        var cb = new ComboBox
        {
            Left = left, Top = top + 20, Width = width,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(Tema.FontFamily, 10),
            FlatStyle = FlatStyle.Flat
        };
        parent.Controls.Add(cb);
        return cb;
    }

    private async Task PreencherAsync()
    {
        cboCategoria.Items.Clear();
        cboCategoria.Items.Add("(nenhuma)");
        var cats = await _categorias.ListarAsync();
        foreach (var c in cats) cboCategoria.Items.Add(c);

        txtCodigo.Text = _produto.Codigo;
        txtBarras.Text = _produto.CodigoBarras ?? "";
        txtDescricao.Text = _produto.Descricao;
        cboCategoria.SelectedIndex = 0;
        if (_produto.CategoriaId.HasValue)
        {
            for (int i = 1; i < cboCategoria.Items.Count; i++)
                if (((Categoria)cboCategoria.Items[i]!).Id == _produto.CategoriaId) { cboCategoria.SelectedIndex = i; break; }
        }
        cboUnidade.SelectedItem = _produto.Unidade;
        txtCusto.Text = _produto.PrecoCusto.ToString("N2");
        txtVenda.Text = _produto.PrecoVenda.ToString("N2");
        txtEstoque.Text = _produto.Estoque.ToString("N3");
        txtMinimo.Text = _produto.EstoqueMinimo.ToString("N3");
        txtNcm.Text = _produto.Ncm ?? "";
        txtCfop.Text = _produto.Cfop;
        txtCstIcms.Text = _produto.CstIcms;
        txtAliqIcms.Text = _produto.AliquotaIcms.ToString("N2");
        chkAtivo.Checked = _produto.Ativo;
        chkControla.Checked = _produto.ControlaEstoque;
    }

    private async Task SalvarAsync()
    {
        try
        {
            _produto.Codigo = txtCodigo.Text.Trim();
            _produto.CodigoBarras = string.IsNullOrWhiteSpace(txtBarras.Text) ? null : txtBarras.Text.Trim();
            _produto.Descricao = txtDescricao.Text.Trim();
            _produto.CategoriaId = cboCategoria.SelectedItem is Categoria c ? c.Id : null;
            _produto.Unidade = (UnidadeMedida)cboUnidade.SelectedItem!;
            _produto.PrecoCusto = ParseDec(txtCusto.Text);
            _produto.PrecoVenda = ParseDec(txtVenda.Text);
            _produto.Estoque = ParseDec(txtEstoque.Text);
            _produto.EstoqueMinimo = ParseDec(txtMinimo.Text);
            _produto.Ncm = string.IsNullOrWhiteSpace(txtNcm.Text) ? null : txtNcm.Text.Trim();
            _produto.Cfop = txtCfop.Text.Trim();
            _produto.CstIcms = txtCstIcms.Text.Trim();
            _produto.AliquotaIcms = ParseDec(txtAliqIcms.Text);
            _produto.Ativo = chkAtivo.Checked;
            _produto.ControlaEstoque = chkControla.Checked;

            var res = await _produtos.SalvarAsync(_produto);
            if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            Toast.Mostrar("Erro: " + ex.Message, TipoToast.Erro, owner: this);
        }
    }

    private static decimal ParseDec(string s) =>
        decimal.TryParse((s ?? "").Replace('.', ','), out var v) ? v : 0;
}
