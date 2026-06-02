using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmPdv : Form
{
    private readonly VendaService _vendaService;
    private readonly ProdutoService _produtoService;
    private readonly NfceService _nfceService;
    private readonly CaixaService _caixaService;
    private readonly CupomPrinterService _printer;
    private Venda? _vendaAtual;

    private TextBox txtCodigo = null!;
    private TextBox txtQuantidade = null!;
    private StyledGrid grid = null!;
    private Label lblTotal = null!;
    private Label lblSubtotal = null!;
    private Label lblDesconto = null!;
    private Label lblItens = null!;
    private Label lblNumero = null!;
    private Button btnFinalizar = null!;

    public FrmPdv(VendaService vendaService, ProdutoService produtoService, NfceService nfceService, CaixaService caixaService, CupomPrinterService printer)
    {
        _vendaService = vendaService;
        _produtoService = produtoService;
        _nfceService = nfceService;
        _caixaService = caixaService;
        _printer = printer;
        InitUi();
        Shown += async (s, e) => await IniciarNovaVendaAsync();
    }

    private void InitUi()
    {
        Text = "PDV - Frente de Caixa";
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Tema.CorFundo;
        KeyPreview = true;
        KeyDown += FrmPdv_KeyDown;

        // === Topbar do PDV (azul) ===
        var topo = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Tema.CorPrimaria };
        var lblTitulo = new Label
        {
            Text = "FRENTE DE CAIXA",
            Font = new Font(Tema.FontFamily, 16, FontStyle.Bold),
            ForeColor = Tema.Branco,
            Dock = DockStyle.Left, Width = 400,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(20, 0, 0, 0)
        };
        lblNumero = new Label
        {
            Text = "Venda: -",
            Font = new Font(Tema.FontFamily, 13),
            ForeColor = Color.FromArgb(220, 230, 240),
            Dock = DockStyle.Right, Width = 400,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 20, 0)
        };
        topo.Controls.Add(lblTitulo);
        topo.Controls.Add(lblNumero);

        // === Container principal ===
        var container = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(Tema.EspacamentoGrande) };

        // === Entrada (código + qtd + add) ===
        var painelEntrada = new Card { Dock = DockStyle.Top, Height = 100, Padding = new Padding(20) };
        var pnlEntrada = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        pnlEntrada.Controls.Add(Inputs.Rotulo("CÓDIGO / CÓD. BARRAS (F2 buscar)", 0, 0, 320));
        txtCodigo = new TextBox
        {
            Left = 0, Top = 22, Width = 500, Height = 42,
            Font = new Font(Tema.FontFamily, 18),
            BorderStyle = BorderStyle.FixedSingle
        };
        txtCodigo.KeyDown += TxtCodigo_KeyDown;
        pnlEntrada.Controls.Add(txtCodigo);

        pnlEntrada.Controls.Add(Inputs.Rotulo("QUANTIDADE", 520, 0, 120));
        txtQuantidade = new TextBox
        {
            Left = 520, Top = 22, Width = 130, Height = 42,
            Font = new Font(Tema.FontFamily, 18),
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Right,
            Text = "1"
        };
        pnlEntrada.Controls.Add(txtQuantidade);

        var btnAdd = Botoes.Sucesso("ADICIONAR (Enter)", 270, 42);
        btnAdd.Top = 22; btnAdd.Left = 670;
        btnAdd.Click += async (s, e) => await AdicionarItemAsync();
        pnlEntrada.Controls.Add(btnAdd);

        painelEntrada.Controls.Add(pnlEntrada);

        // === Body: grid (esquerda) + total card (direita) ===
        var body = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(0, 16, 0, 0) };

        var rightPanel = new Panel { Dock = DockStyle.Right, Width = 360, BackColor = Tema.CorFundo };

        // Card totais (grande, destaque)
        var cardTotal = new Card { Dock = DockStyle.Top, Height = 280, Padding = new Padding(20) };
        cardTotal.BackColor = Tema.CorPrimaria;
        cardTotal.CorBorda = Tema.CorPrimariaDark;
        // Para que o card pinte fundo azul, vamos sobrepor um label
        cardTotal.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(3, 3, cardTotal.Width - 7, cardTotal.Height - 7);
            using var path = Tema.PathArredondado(rect, Tema.RaioCard);
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, Tema.CorPrimaria, Tema.CorPrimariaDark, System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            g.FillPath(brush, path);
        };

        var pnlTotal = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var lblTotalLbl = new Label
        {
            Text = "TOTAL",
            Dock = DockStyle.Top, Height = 22,
            Font = new Font(Tema.FontFamily, 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 200, 230),
            BackColor = Color.Transparent
        };
        lblTotal = new Label
        {
            Text = "R$ 0,00",
            Dock = DockStyle.Top, Height = 70,
            Font = new Font(Tema.FontFamily, 36, FontStyle.Bold),
            ForeColor = Tema.Branco,
            BackColor = Color.Transparent
        };
        lblSubtotal = new Label
        {
            Text = "Subtotal: R$ 0,00",
            Dock = DockStyle.Top, Height = 26,
            Font = new Font(Tema.FontFamily, 11),
            ForeColor = Color.FromArgb(220, 230, 245),
            BackColor = Color.Transparent
        };
        lblDesconto = new Label
        {
            Text = "Desconto: R$ 0,00",
            Dock = DockStyle.Top, Height = 26,
            Font = new Font(Tema.FontFamily, 11),
            ForeColor = Color.FromArgb(220, 230, 245),
            BackColor = Color.Transparent
        };
        lblItens = new Label
        {
            Text = "Itens: 0",
            Dock = DockStyle.Top, Height = 26,
            Font = new Font(Tema.FontFamily, 11),
            ForeColor = Color.FromArgb(220, 230, 245),
            BackColor = Color.Transparent
        };
        pnlTotal.Controls.Add(lblItens);
        pnlTotal.Controls.Add(lblDesconto);
        pnlTotal.Controls.Add(lblSubtotal);
        pnlTotal.Controls.Add(lblTotal);
        pnlTotal.Controls.Add(lblTotalLbl);
        cardTotal.Controls.Add(pnlTotal);

        // Botões grandes ao lado direito
        btnFinalizar = Botoes.Sucesso("FINALIZAR (F10)", 340, 70);
        btnFinalizar.Font = new Font(Tema.FontFamily, 14, FontStyle.Bold);
        btnFinalizar.Top = 290; btnFinalizar.Left = 0;
        btnFinalizar.Click += async (s, e) => await FinalizarAsync();

        var fl = new FlowLayoutPanel { Top = 370, Left = 0, Width = 340, Height = 200, FlowDirection = FlowDirection.LeftToRight, BackColor = Tema.CorFundo, WrapContents = true };
        var btnDesconto = Botoes.Aviso("Desconto (F4)", 165, 50);
        btnDesconto.Click += async (s, e) => await AplicarDescontoAsync();
        var btnRemover = Botoes.Ghost("Remover item (Del)", 165, 50);
        btnRemover.Click += async (s, e) => await RemoverItemAsync();
        var btnNova = Botoes.Info("Nova venda (F12)", 165, 50);
        btnNova.Click += async (s, e) => await IniciarNovaVendaAsync();
        var btnCancelar = Botoes.Perigo("Cancelar (Esc)", 165, 50);
        btnCancelar.Click += async (s, e) => await CancelarAsync();
        fl.Controls.Add(btnDesconto);
        fl.Controls.Add(btnRemover);
        fl.Controls.Add(btnNova);
        fl.Controls.Add(btnCancelar);

        rightPanel.Controls.Add(fl);
        rightPanel.Controls.Add(btnFinalizar);
        rightPanel.Controls.Add(cardTotal);

        // Grid central
        var spacer = new Panel { Dock = DockStyle.Right, Width = 16, BackColor = Tema.CorFundo };
        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("Codigo", "Código");
        grid.Columns.Add("Descricao", "Descrição");
        grid.Columns.Add("Quantidade", "Qtd");
        grid.Columns.Add("Preco", "Preço Unit.");
        grid.Columns.Add("Total", "Total");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns["Descricao"]!.FillWeight = 300;
        foreach (var c in new[] { "Quantidade", "Preco", "Total" })
            grid.Columns[c]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        cardGrid.Controls.Add(grid);

        body.Controls.Add(cardGrid);
        body.Controls.Add(spacer);
        body.Controls.Add(rightPanel);

        container.Controls.Add(body);
        container.Controls.Add(painelEntrada);

        Controls.Add(container);
        Controls.Add(topo);
    }

    private void FrmPdv_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.F2: BuscarProdutoDialog(); e.Handled = true; break;
            case Keys.F4: _ = AplicarDescontoAsync(); e.Handled = true; break;
            case Keys.F10: _ = FinalizarAsync(); e.Handled = true; break;
            case Keys.F12: _ = IniciarNovaVendaAsync(); e.Handled = true; break;
            case Keys.Delete:
                if (grid.Focused) { _ = RemoverItemAsync(); e.Handled = true; }
                break;
            case Keys.Escape: _ = CancelarAsync(); e.Handled = true; break;
        }
    }

    private async void TxtCodigo_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            await AdicionarItemAsync();
        }
    }

    private async Task IniciarNovaVendaAsync()
    {
        var caixa = await _caixaService.ObterCaixaAbertoAsync();
        if (caixa == null)
        {
            Toast.Mostrar("Abra o caixa antes de iniciar vendas.", TipoToast.Aviso, owner: this);
            Close();
            return;
        }

        if (_vendaAtual is { Status: StatusVenda.EmAberto, Itens.Count: > 0 })
        {
            var ok = MessageBox.Show("Existe venda em aberto. Deseja cancelar e iniciar nova?",
                "Confirmação", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (ok != DialogResult.Yes) return;
            await _vendaService.CancelarAsync(_vendaAtual.Id, "Cancelada para iniciar nova");
        }

        var res = await _vendaService.NovaVendaAsync();
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        _vendaAtual = res.Valor;
        lblNumero.Text = $"Venda: {_vendaAtual!.Numero}";
        grid.Rows.Clear();
        AtualizarTotais();
        txtQuantidade.Text = "1";
        txtCodigo.Focus();
    }

    private async Task AdicionarItemAsync()
    {
        if (_vendaAtual == null) { await IniciarNovaVendaAsync(); }
        var codigo = txtCodigo.Text.Trim();
        if (string.IsNullOrEmpty(codigo)) { txtCodigo.Focus(); return; }
        if (!decimal.TryParse(txtQuantidade.Text.Replace('.', ','), out var qtd) || qtd <= 0)
        {
            Toast.Mostrar("Quantidade inválida.", TipoToast.Erro, owner: this);
            txtQuantidade.Focus(); txtQuantidade.SelectAll();
            return;
        }

        var produto = await _produtoService.BuscarPorCodigoAsync(codigo);
        if (produto == null)
        {
            Toast.Mostrar($"Produto '{codigo}' não encontrado.", TipoToast.Aviso, owner: this);
            txtCodigo.SelectAll(); txtCodigo.Focus(); return;
        }

        var res = await _vendaService.AdicionarItemAsync(_vendaAtual!.Id, produto.Id, qtd);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }

        await RecarregarItensAsync();
        txtCodigo.Clear();
        txtQuantidade.Text = "1";
        txtCodigo.Focus();
    }

    private async Task RecarregarItensAsync()
    {
        if (_vendaAtual == null) return;
        var v = await _vendaService.BuscarAsync(_vendaAtual.Id);
        if (v == null) return;
        _vendaAtual = v;
        grid.Rows.Clear();
        foreach (var it in v.Itens)
        {
            grid.Rows.Add(it.Id, it.Produto.Codigo, it.Produto.Descricao,
                it.Quantidade.ToString("N3"), it.PrecoUnitario.ToString("N2"), it.Total.ToString("N2"));
        }
        AtualizarTotais();
    }

    private void AtualizarTotais()
    {
        lblItens.Text = $"Itens: {_vendaAtual?.Itens.Count ?? 0}";
        lblSubtotal.Text = $"Subtotal: {(_vendaAtual?.SubTotal ?? 0):C}";
        lblDesconto.Text = $"Desconto: {(_vendaAtual?.Desconto ?? 0):C}";
        lblTotal.Text = (_vendaAtual?.Total ?? 0).ToString("C");
    }

    private async Task RemoverItemAsync()
    {
        if (grid.SelectedRows.Count == 0) return;
        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;
        var res = await _vendaService.RemoverItemAsync(id);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        await RecarregarItensAsync();
    }

    private async Task AplicarDescontoAsync()
    {
        if (_vendaAtual == null || _vendaAtual.SubTotal == 0) return;
        var s = Microsoft.VisualBasic.Interaction.InputBox(
            "Informe o valor do desconto (R$):", "Desconto", _vendaAtual.Desconto.ToString("N2"));
        if (string.IsNullOrWhiteSpace(s)) return;
        if (!decimal.TryParse(s.Replace('.', ','), out var d)) { Toast.Mostrar("Valor inválido.", TipoToast.Erro, owner: this); return; }
        var res = await _vendaService.AplicarDescontoAsync(_vendaAtual.Id, d);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        await RecarregarItensAsync();
    }

    private void BuscarProdutoDialog()
    {
        var s = Microsoft.VisualBasic.Interaction.InputBox(
            "Buscar produto por descrição/código:", "Busca de Produto", "");
        if (string.IsNullOrWhiteSpace(s)) return;
        txtCodigo.Text = s.Trim();
        _ = AdicionarItemAsync();
    }

    private async Task CancelarAsync()
    {
        if (_vendaAtual == null || _vendaAtual.Status != StatusVenda.EmAberto) return;
        if (!_vendaAtual.Itens.Any())
        {
            await _vendaService.CancelarAsync(_vendaAtual.Id, "Vazia");
            await IniciarNovaVendaAsync();
            return;
        }
        if (MessageBox.Show("Cancelar venda atual?", "Confirmação",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        var res = await _vendaService.CancelarAsync(_vendaAtual.Id, "Cancelada pelo operador");
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        await IniciarNovaVendaAsync();
    }

    private async Task FinalizarAsync()
    {
        if (_vendaAtual == null || !_vendaAtual.Itens.Any())
        {
            Toast.Mostrar("Adicione itens antes de finalizar.", TipoToast.Aviso, owner: this);
            return;
        }

        using var dlg = new FrmPagamento(_vendaAtual.Total);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var res = await _vendaService.FinalizarAsync(_vendaAtual.Id, dlg.Pagamentos);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }

        var venda = res.Valor!;
        var caixaAberto = await _caixaService.ObterCaixaAbertoAsync();
        if (caixaAberto != null)
            await _caixaService.RegistrarVendaAsync(caixaAberto.Id, venda.Id, dlg.Pagamentos);

        var msg = $"Venda finalizada!\n\nTotal: {venda.Total:C}\nPago: {venda.ValorPago:C}\nTroco: {venda.Troco:C}\n\nEmitir NFC-e agora?";
        var emitir = MessageBox.Show(msg, "Sucesso", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (emitir == DialogResult.Yes)
        {
            await EmitirNfceAsync(venda.Id);
        }
        else
        {
            var empresa = await _nfceService.ObterEmpresaAsync();
            var vendaCompleta = await _vendaService.BuscarAsync(venda.Id);
            if (empresa != null && vendaCompleta != null && empresa.ImprimirAutomatico
                && !string.IsNullOrWhiteSpace(empresa.ImpressoraDestino))
            {
                var resPrint = await _printer.ImprimirVendaAsync(vendaCompleta, empresa, null);
                if (!resPrint.Sucesso)
                    Toast.Mostrar("Aviso na impressão: " + resPrint.Erro, TipoToast.Aviso, owner: this);
            }
        }

        await IniciarNovaVendaAsync();
    }

    private async Task EmitirNfceAsync(int vendaId)
    {
        UseWaitCursor = true;
        try
        {
            bool contingencia = false;
            if (!await _nfceService.SefazOnlineAsync())
            {
                var r = MessageBox.Show(
                    "SEFAZ aparentemente OFFLINE.\nEmitir em CONTINGÊNCIA (tpEmis=9)?\nA nota será enviada quando o serviço voltar.",
                    "Contingência", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (r == DialogResult.Cancel) return;
                contingencia = r == DialogResult.Yes;
            }
            var res = contingencia
                ? await _nfceService.EmitirContingenciaAsync(vendaId)
                : await _nfceService.EmitirAsync(vendaId);
            if (!res.Sucesso)
            {
                Toast.Mostrar("Falha NFC-e: " + res.Erro, TipoToast.Erro, owner: this);
                return;
            }

            var nota = res.Valor!;
            var empresa = await _nfceService.ObterEmpresaAsync();
            var venda = await _vendaService.BuscarAsync(vendaId);
            if (empresa != null && venda != null)
            {
                if (nota.Status == StatusNotaFiscal.Autorizada && empresa.ImprimirAutomatico
                    && !string.IsNullOrWhiteSpace(empresa.ImpressoraDestino))
                {
                    var resPrint = await _printer.ImprimirVendaAsync(venda, empresa, nota);
                    if (!resPrint.Sucesso)
                        Toast.Mostrar("Aviso impressão: " + resPrint.Erro, TipoToast.Aviso, owner: this);
                }

                using var dlg = new FrmNfceResultado(nota, empresa, venda);
                dlg.ShowDialog(this);
            }
        }
        catch (Exception ex)
        {
            Toast.Mostrar("Erro: " + ex.Message, TipoToast.Erro, owner: this);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }
}
