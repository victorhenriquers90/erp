using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Domain.Enums;
using ProjetoVarejo.Shared;
using System.Text;

namespace ProjetoVarejo.Desktop.Forms;

/// <summary>
/// Relatório de fechamento do dia: resume vendas, caixa, contas e permite fechar sessão.
/// </summary>
[ModuloRequerido(ModuloSistema.PDV | ModuloSistema.Financeiro)]
public class FrmFechamentoDia : Form
{
    private readonly IVendaService _vendaSvc;
    private readonly ICaixaService _caixaSvc;
    private readonly IFinanceiroService _financeiroSvc;

    private Label _lblData = null!;
    private FlowLayoutPanel _pnlKpis = null!;
    private StyledGrid _gridVendas = null!;
    private StyledGrid _gridFormas = null!;
    private Panel _pnlStatus = null!;
    private Button _btnFecharCaixa = null!;
    private CaixaSessao? _caixaAberto;

    public FrmFechamentoDia(IVendaService vendaSvc, ICaixaService caixaSvc, IFinanceiroService financeiroSvc)
    {
        _vendaSvc = vendaSvc;
        _caixaSvc = caixaSvc;
        _financeiroSvc = financeiroSvc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Fechamento do Dia";
        Size = new Size(1080, 740);
        MinimumSize = new Size(960, 680);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Fechamento do Dia", "Resumo de vendas, caixa e financeiro");
        _lblData = Inputs.SubtituloHeader(header);

        // === Status do caixa ===
        _pnlStatus = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Tema.CorCardAlt,
            Padding = new Padding(16, 0, 16, 0)
        };
        var lblStatus = new Label
        {
            Text = "Verificando caixa...",
            Dock = DockStyle.Fill,
            Font = Tema.FontCorpoBold,
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _btnFecharCaixa = Botoes.SucessoIcone("Fechar Caixa Agora", Tema.IconCaixa, 180, 36);
        _btnFecharCaixa.Dock = DockStyle.Right;
        _btnFecharCaixa.Visible = false;
        _btnFecharCaixa.Click += async (s, e) => await FecharCaixaAsync();
        _pnlStatus.Tag = lblStatus;
        _pnlStatus.Controls.Add(_btnFecharCaixa);
        _pnlStatus.Controls.Add(lblStatus);

        // === KPIs ===
        _pnlKpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 130,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Tema.CorFundo,
            Padding = new Padding(0, 8, 0, 8)
        };

        // === Corpo com duas grades ===
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(0, 8, 0, 0) };

        // Grade vendas do dia
        var cardVendas = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        var lblTitVendas = new Label
        {
            Text = "  Vendas do dia",
            Dock = DockStyle.Top,
            Height = 32,
            Font = Tema.FontSubtitulo,
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Tema.CorCard
        };
        _gridVendas = new StyledGrid();
        _gridVendas.Columns.Add("Hora", "Hora");
        _gridVendas.Columns.Add("Numero", "Nº Venda");
        _gridVendas.Columns.Add("Cliente", "Cliente");
        _gridVendas.Columns.Add("Itens", "Itens");
        _gridVendas.Columns.Add("Total", "Total");
        _gridVendas.Columns.Add("Status", "Status");
        _gridVendas.Columns["Hora"]!.FillWeight = 60;
        _gridVendas.Columns["Numero"]!.FillWeight = 80;
        _gridVendas.Columns["Cliente"]!.FillWeight = 160;
        _gridVendas.Columns["Itens"]!.FillWeight = 50;
        _gridVendas.Columns["Total"]!.FillWeight = 80;
        _gridVendas.Columns["Total"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        _gridVendas.Columns["Status"]!.FillWeight = 80;
        cardVendas.Controls.Add(_gridVendas);
        cardVendas.Controls.Add(lblTitVendas);

        // Grade formas de pagamento
        var colDir = new Panel { Dock = DockStyle.Right, Width = 280, BackColor = Tema.CorFundo };
        var cardFormas = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        var lblTitFormas = new Label
        {
            Text = "  Por forma de pagamento",
            Dock = DockStyle.Top,
            Height = 32,
            Font = Tema.FontSubtitulo,
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Tema.CorCard
        };
        _gridFormas = new StyledGrid();
        _gridFormas.Columns.Add("Forma", "Forma");
        _gridFormas.Columns.Add("Total", "Total (R$)");
        _gridFormas.Columns["Total"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        cardFormas.Controls.Add(_gridFormas);
        cardFormas.Controls.Add(lblTitFormas);
        colDir.Controls.Add(cardFormas);

        corpo.Controls.Add(cardVendas);
        corpo.Controls.Add(colDir);

        // === Rodapé ===
        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };
        var btnExportar = Botoes.GhostIcone("Exportar TXT", Tema.IconUpload, 150, 36);
        btnExportar.Click += ExportarTxt;
        var btnFechar = Botoes.Ghost("Fechar", 112, 36);
        btnFechar.Click += (s, e) => Close();
        var acoesRodape = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Tema.CorFundo
        };
        Botoes.ParaPainelToolbar(acoesRodape, btnExportar, btnFechar);
        acoesRodape.Controls.Add(btnExportar);
        acoesRodape.Controls.Add(btnFechar);
        rodape.Controls.Add(acoesRodape);

        Controls.Add(corpo);
        Controls.Add(rodape);
        Controls.Add(_pnlKpis);
        Controls.Add(_pnlStatus);
        Controls.Add(header);

        rodape.BringToFront();
    }

    private async Task CarregarAsync()
    {
        try
        {
            UseWaitCursor = true;
            var hoje = DateTime.Today;
            var amanha = hoje.AddDays(1);
            _lblData.Text = $"Data: {hoje:dddd, dd/MM/yyyy}";

            // Carregar dados em paralelo
            var tarefaVendas = _vendaSvc.ListarAsync(de: hoje, ate: amanha.AddSeconds(-1));
            var tarefaCaixa = _caixaSvc.ObterCaixaAbertoAsync();
            var tarefaFinanceiro = _financeiroSvc.ResumoAsync(hoje, amanha.AddSeconds(-1));
            var tarefaTotalDia = _financeiroSvc.TotalVendasDoDiaAsync(hoje);

            await Task.WhenAll(tarefaVendas, tarefaCaixa, tarefaFinanceiro, tarefaTotalDia);

            var vendas = tarefaVendas.Result;
            _caixaAberto = tarefaCaixa.Result;
            var (receber, pagar, saldo) = tarefaFinanceiro.Result;
            var totalDia = tarefaTotalDia.Result;

            // Caixa
            AtualizarStatusCaixa();

            // KPIs
            var vendaFinalizadas = vendas.Where(v => v.Status == StatusVenda.Finalizada).ToList();
            var vendaCanceladas = vendas.Count(v => v.Status == StatusVenda.Cancelada);
            var ticketMedio = vendaFinalizadas.Count > 0
                ? vendaFinalizadas.Average(v => v.Total) : 0m;

            _pnlKpis.Controls.Clear();
            _pnlKpis.Controls.Add(new KpiCard("Vendas", vendaFinalizadas.Count.ToString(), Tema.IconVendas, Tema.CorPrimaria));
            _pnlKpis.Controls.Add(new KpiCard("Faturamento", totalDia.ToString("C"), Tema.IconFinanceiro, Tema.CorSucesso));
            _pnlKpis.Controls.Add(new KpiCard("Ticket Médio", ticketMedio.ToString("C"), Tema.IconNotas, Tema.CorInfo));
            _pnlKpis.Controls.Add(new KpiCard("Canceladas", vendaCanceladas.ToString(), Tema.IconAlerta, Tema.CorAlerta));
            if (_caixaAberto != null)
            {
                var resumoCaixa = await _caixaSvc.ResumoAsync(_caixaAberto.Id);
                _pnlKpis.Controls.Add(new KpiCard("Saldo Caixa", resumoCaixa.SaldoDinheiroEsperado.ToString("C"), Tema.IconCaixa, Tema.CorPrimaria));
            }

            // Grade vendas
            _gridVendas.Rows.Clear();
            foreach (var v in vendas.OrderByDescending(v => v.DataVenda))
            {
                var idx = _gridVendas.Rows.Add(
                    v.DataVenda.ToString("HH:mm"),
                    v.Numero,
                    v.Cliente?.Nome ?? "Consumidor",
                    v.Itens?.Count ?? 0,
                    v.Total.ToString("N2"),
                    v.Status == StatusVenda.Finalizada ? "Finalizada" : v.Status == StatusVenda.Cancelada ? "Cancelada" : v.Status.ToString());

                var row = _gridVendas.Rows[idx];
                if (v.Status == StatusVenda.Cancelada)
                    row.Cells["Status"].Style.ForeColor = Tema.CorErro;
                else if (v.Status == StatusVenda.Finalizada)
                    row.Cells["Status"].Style.ForeColor = Tema.CorSucesso;
            }

            // Grade formas de pagamento
            _gridFormas.Rows.Clear();
            var porForma = vendaFinalizadas
                .SelectMany(v => v.Pagamentos ?? Enumerable.Empty<PagamentoVenda>())
                .GroupBy(p => p.FormaPagamento)
                .Select(g => new { Forma = g.Key, Total = g.Sum(p => p.Valor) })
                .OrderByDescending(x => x.Total);

            foreach (var f in porForma)
                _gridFormas.Rows.Add(NomeForma(f.Forma), f.Total.ToString("N2"));

            _gridFormas.Rows.Add("─────────────", "──────────");
            _gridFormas.Rows.Add("TOTAL", totalDia.ToString("N2"));
        }
        catch (Exception ex)
        {
            Toast.Mostrar($"Erro ao carregar fechamento: {ex.Message}", TipoToast.Erro, owner: this);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void AtualizarStatusCaixa()
    {
        if (_pnlStatus.Tag is not Label lbl) return;
        if (_caixaAberto != null)
        {
            lbl.Text = $"⚠️  Caixa aberto desde {_caixaAberto.AbertaEm:HH:mm} — valor abertura: {_caixaAberto.ValorAbertura:C}";
            lbl.ForeColor = Tema.CorAlerta;
            _btnFecharCaixa.Visible = true;
            _pnlStatus.BackColor = Color.FromArgb(255, 248, 230);
        }
        else
        {
            lbl.Text = "✅  Caixa fechado";
            lbl.ForeColor = Tema.CorSucesso;
            _btnFecharCaixa.Visible = false;
            _pnlStatus.BackColor = Color.FromArgb(230, 250, 235);
        }
    }

    private async Task FecharCaixaAsync()
    {
        if (_caixaAberto == null) return;
        try
        {
            var resumo = await _caixaSvc.ResumoAsync(_caixaAberto.Id);
            var msg = $"Resumo do caixa:\n\n" +
                      $"Abertura:      {resumo.ValorAbertura:C}\n" +
                      $"Suprimentos:   {resumo.TotalSuprimentos:C}\n" +
                      $"Sangrias:      {resumo.TotalSangrias:C}\n" +
                      $"Total vendas:  {resumo.TotalVendas:C}\n" +
                      $"Saldo esperado: {resumo.SaldoDinheiroEsperado:C}\n\n" +
                      "Confirma o fechamento do caixa com o saldo acima?";

            if (MessageBox.Show(msg, "Fechar Caixa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var res = await _caixaSvc.FecharAsync(_caixaAberto.Id, resumo.SaldoDinheiroEsperado);
            if (!res.Sucesso)
            {
                Toast.Mostrar(res.Erro ?? "Erro ao fechar caixa.", TipoToast.Erro, owner: this);
                return;
            }

            _caixaAberto = null;
            AtualizarStatusCaixa();
            Toast.Mostrar("Caixa fechado com sucesso.", TipoToast.Sucesso, owner: this);
        }
        catch (Exception ex)
        {
            Toast.Mostrar(ex.Message, TipoToast.Erro, owner: this);
        }
    }

    private void ExportarTxt(object? sender, EventArgs e)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("=".PadRight(48, '='));
            sb.AppendLine($"  FECHAMENTO DO DIA — {DateTime.Today:dd/MM/yyyy}");
            sb.AppendLine("=".PadRight(48, '='));

            // KPIs
            foreach (DataGridViewRow row in _gridFormas.Rows)
            {
                var forma = row.Cells["Forma"].Value?.ToString() ?? "";
                var total = row.Cells["Total"].Value?.ToString() ?? "";
                sb.AppendLine($"{forma,-28} {total,14}");
            }
            sb.AppendLine();

            // Vendas
            sb.AppendLine($"{"HORA",-6} {"NUMERO",-14} {"CLIENTE",-22} {"TOTAL",10}");
            sb.AppendLine("-".PadRight(56, '-'));
            foreach (DataGridViewRow row in _gridVendas.Rows)
            {
                var hora = row.Cells["Hora"].Value?.ToString() ?? "";
                var num = row.Cells["Numero"].Value?.ToString() ?? "";
                var cli = (row.Cells["Cliente"].Value?.ToString() ?? "").PadRight(22).Substring(0, 22);
                var tot = row.Cells["Total"].Value?.ToString() ?? "";
                sb.AppendLine($"{hora,-6} {num,-14} {cli} {tot,10}");
            }

            sb.AppendLine();
            sb.AppendLine($"  Emitido em {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine("=".PadRight(48, '='));

            using var dlg = new SaveFileDialog
            {
                FileName = $"fechamento_{DateTime.Today:yyyyMMdd}.txt",
                Filter = "Texto|*.txt",
                Title = "Salvar fechamento do dia"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            Toast.Mostrar("Arquivo salvo com sucesso.", TipoToast.Sucesso, owner: this);
        }
        catch (Exception ex)
        {
            Toast.Mostrar(ex.Message, TipoToast.Erro, owner: this);
        }
    }

    private static string NomeForma(FormaPagamentoTipo forma) => forma switch
    {
        FormaPagamentoTipo.Dinheiro     => "Dinheiro",
        FormaPagamentoTipo.Credito      => "Cartão Crédito",
        FormaPagamentoTipo.Debito       => "Cartão Débito",
        FormaPagamentoTipo.Pix          => "PIX",
        FormaPagamentoTipo.Boleto       => "Boleto",
        FormaPagamentoTipo.Crediario    => "Crediário",
        FormaPagamentoTipo.ValeRefeicao => "Vale Refeição",
        _                               => forma.ToString()
    };
}
