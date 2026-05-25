using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmBuscarProdutoPdv : Form
{
    private readonly ProdutoService _produtos;
    private readonly string _termoInicial;
    private TextBox txtBusca = null!;
    private StyledGrid grid = null!;
    private Label lblTotal = null!;

    public Produto? ProdutoSelecionado { get; private set; }

    public FrmBuscarProdutoPdv(ProdutoService produtos, string? termoInicial = null)
    {
        _produtos = produtos;
        _termoInicial = termoInicial?.Trim() ?? "";
        InitUi();
        Shown += async (s, e) =>
        {
            txtBusca.Text = _termoInicial;
            await CarregarAsync();
            txtBusca.Focus();
            txtBusca.SelectAll();
        };
    }

    private void InitUi()
    {
        Text = "Buscar Produto";
        Size = new Size(980, 640);
        MinimumSize = new Size(860, 560);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Buscar produto", "Pesquise por nome, codigo interno ou codigo de barras");
        lblTotal = Inputs.SubtituloHeader(header);

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Tema.CorFundo, Padding = new Padding(0, 10, 0, 10) };
        var (pnlBusca, tb) = Inputs.BarraBusca("Digite parte do nome, codigo ou barras...");
        pnlBusca.Dock = DockStyle.Fill;
        txtBusca = tb;
        txtBusca.KeyDown += async (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            await CarregarAsync();
            if (grid.Rows.Count == 1) SelecionarAtual();
            else grid.Focus();
        };

        var pnlBotoes = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 308,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Tema.CorFundo
        };

        var btnSelecionar = Botoes.SucessoIcone("Selecionar", Tema.IconSucesso, 132, 40);
        btnSelecionar.Click += (s, e) => SelecionarAtual();
        var btnPesquisar = Botoes.PrimarioIcone("Pesquisar", Tema.IconBusca, 126, 40);
        btnPesquisar.Click += async (s, e) => await CarregarAsync();
        Botoes.ParaToolbar(btnSelecionar, btnPesquisar);
        pnlBotoes.Controls.Add(btnSelecionar);
        pnlBotoes.Controls.Add(btnPesquisar);

        toolbar.Controls.Add(pnlBusca);
        toolbar.Controls.Add(pnlBotoes);

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("Codigo", "Codigo");
        grid.Columns.Add("CodigoBarras", "Barras");
        grid.Columns.Add("Descricao", "Produto");
        grid.Columns.Add("Categoria", "Categoria");
        grid.Columns.Add("Preco", "Preco");
        grid.Columns.Add("Estoque", "Estoque");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns["Codigo"]!.FillWeight = 75;
        grid.Columns["CodigoBarras"]!.FillWeight = 95;
        grid.Columns["Descricao"]!.FillWeight = 270;
        grid.Columns["Categoria"]!.FillWeight = 130;
        grid.Columns["Preco"]!.FillWeight = 70;
        grid.Columns["Estoque"]!.FillWeight = 70;
        grid.Columns["Preco"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["Estoque"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.DoubleClick += (s, e) => SelecionarAtual();
        grid.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SelecionarAtual();
            }
        };
        card.Controls.Add(grid);

        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };
        var btnCancelar = Botoes.Ghost("Cancelar", 120, 36);
        btnCancelar.Dock = DockStyle.Right;
        btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        rodape.Controls.Add(btnCancelar);

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(toolbar);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        UseWaitCursor = true;
        try
        {
            var lista = await _produtos.ListarParaVendaAsync(txtBusca.Text.Trim());
            grid.Rows.Clear();

            foreach (var p in lista)
            {
                var rowIndex = grid.Rows.Add(
                    p.Id,
                    p.Codigo,
                    p.CodigoBarras ?? "",
                    p.Descricao,
                    p.Categoria?.Nome ?? "",
                    p.PrecoVenda.ToString("N2"),
                    p.Estoque.ToString("N3"));

                var row = grid.Rows[rowIndex];
                row.Tag = p;
                if (p.ControlaEstoque && p.Estoque <= p.EstoqueMinimo)
                    row.Cells["Estoque"].Style.ForeColor = Tema.CorAlerta;
            }

            if (grid.Rows.Count > 0)
                grid.Rows[0].Selected = true;

            lblTotal.Text = $"{lista.Count} produto(s) encontrado(s)";
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private async void SelecionarAtual()
    {
        if (grid.SelectedRows.Count == 0)
        {
            Toast.Mostrar("Selecione um produto.", TipoToast.Info, owner: this);
            return;
        }

        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
        ProdutoSelecionado = await _produtos.BuscarPorIdAsync(id);
        if (ProdutoSelecionado == null)
        {
            Toast.Mostrar("Produto nao encontrado.", TipoToast.Erro, owner: this);
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
