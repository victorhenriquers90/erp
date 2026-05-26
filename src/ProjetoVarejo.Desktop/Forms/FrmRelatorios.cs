using System.Globalization;
using System.Text;
using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Desktop.Forms;

[ModuloRequerido(ModuloSistema.Relatorios)]
public class FrmRelatorios : Form
{
    private readonly IRelatorioService _svc;
    private DateTimePicker dtDe = null!, dtAte = null!;
    private TabControl tabs = null!;
    private StyledGrid gridDia = null!, gridForma = null!, gridVendedor = null!, gridAbc = null!, gridTop = null!, gridFluxo = null!;
    private Label _resumo = null!;

    public FrmRelatorios(IRelatorioService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Relatórios";
        Size = new Size(1280, 760);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Relatórios gerenciais", "Análises de vendas, produtos e fluxo de caixa");

        var filtros = new Card { Dock = DockStyle.Top, Height = 80, Padding = new Padding(16) };
        var pnlFiltros = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        pnlFiltros.Controls.Add(Inputs.Rotulo("DE", 0, 0));
        dtDe = new DateTimePicker { Left = 0, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Font = Tema.FontCorpo };
        pnlFiltros.Controls.Add(dtDe);
        pnlFiltros.Controls.Add(Inputs.Rotulo("ATÉ", 145, 0));
        dtAte = new DateTimePicker { Left = 145, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1), Font = Tema.FontCorpo };
        pnlFiltros.Controls.Add(dtAte);
        var btnFiltrar = Botoes.Primario("Atualizar", 110, 32);
        btnFiltrar.Top = 18; btnFiltrar.Left = 290;
        btnFiltrar.Click += async (s, e) => await CarregarAsync();
        pnlFiltros.Controls.Add(btnFiltrar);
        var btnExp = Botoes.Ghost("Exportar CSV (aba atual)", 240, 32);
        btnExp.Top = 18; btnExp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnExp.Click += (s, e) => ExportarAtualCsv();
        pnlFiltros.Controls.Add(btnExp);
        pnlFiltros.Resize += (s, e) => btnExp.Left = pnlFiltros.Width - 250;
        filtros.Controls.Add(pnlFiltros);

        _resumo = new Label
        {
            Dock = DockStyle.Bottom, Height = 44,
            Font = new Font(Tema.FontFamily, 11, FontStyle.Bold),
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Tema.CorPrimariaSoft,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(15, 0, 15, 0)
        };

        tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamily, 10),
            DrawMode = TabDrawMode.OwnerDrawFixed,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(180, 40),
            Appearance = TabAppearance.Normal
        };
        EstilizarTabs(tabs);

        gridDia = NovaGrid(new[] { "Dia", "Qtd Vendas", "Total" });
        gridForma = NovaGrid(new[] { "Forma de pagamento", "Qtd", "Total" });
        gridVendedor = NovaGrid(new[] { "Vendedor", "Qtd Vendas", "Total", "Ticket médio" });
        gridAbc = NovaGrid(new[] { "Código", "Descrição", "Quantidade", "Faturamento", "Classe" });
        gridTop = NovaGrid(new[] { "Código", "Descrição", "Quantidade", "Faturamento" });
        gridFluxo = NovaGrid(new[] { "Dia", "Entradas", "Saídas", "Saldo" });

        tabs.TabPages.Add(MontarTab("Vendas por dia", gridDia));
        tabs.TabPages.Add(MontarTab("Por forma de pagamento", gridForma));
        tabs.TabPages.Add(MontarTab("Por vendedor", gridVendedor));
        tabs.TabPages.Add(MontarTab("Curva ABC", gridAbc));
        tabs.TabPages.Add(MontarTab("Top produtos", gridTop));
        tabs.TabPages.Add(MontarTab("Fluxo de caixa", gridFluxo));

        Controls.Add(tabs);
        Controls.Add(_resumo);
        Controls.Add(filtros);
        Controls.Add(header);
    }

    private TabPage MontarTab(string titulo, StyledGrid grid)
    {
        var tp = new TabPage(titulo) { BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        card.Controls.Add(grid);
        tp.Controls.Add(card);
        return tp;
    }

    private static StyledGrid NovaGrid(string[] colunas)
    {
        var g = new StyledGrid();
        foreach (var c in colunas) g.Columns.Add(c, c);
        return g;
    }

    private void EstilizarTabs(TabControl tc)
    {
        tc.DrawItem += (s, e) =>
        {
            var g = e.Graphics;
            var tab = tc.TabPages[e.Index];
            var rect = tc.GetTabRect(e.Index);
            var selected = e.Index == tc.SelectedIndex;
            var bg = selected ? Tema.CorCard : Tema.CorFundo;
            var fg = selected ? Tema.CorPrimaria : Tema.CorTextoMedio;
            using (var brush = new SolidBrush(bg)) g.FillRectangle(brush, rect);
            if (selected)
            {
                using var line = new SolidBrush(Tema.CorPrimaria);
                g.FillRectangle(line, rect.X, rect.Bottom - 3, rect.Width, 3);
            }
            TextRenderer.DrawText(g, tab.Text, new Font(Tema.FontFamily, 10, selected ? FontStyle.Bold : FontStyle.Regular),
                rect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
    }

    private async Task CarregarAsync()
    {
        UseWaitCursor = true;
        try
        {
            var de = dtDe.Value.Date;
            var ate = dtAte.Value.Date.AddDays(1);
            var ptBR = new CultureInfo("pt-BR");

            gridDia.Rows.Clear();
            decimal totalDia = 0;
            foreach (var x in await _svc.VendasPorDiaAsync(de, ate))
            {
                gridDia.Rows.Add(x.Dia.ToString("dd/MM/yyyy"), x.Quantidade, x.Total.ToString("C", ptBR));
                totalDia += x.Total;
            }

            gridForma.Rows.Clear();
            foreach (var x in await _svc.VendasPorFormaPagamentoAsync(de, ate))
                gridForma.Rows.Add(x.Forma, x.Qtd, x.Total.ToString("C", ptBR));

            gridVendedor.Rows.Clear();
            foreach (var x in await _svc.VendasPorVendedorAsync(de, ate))
                gridVendedor.Rows.Add(x.Vendedor, x.QtdVendas, x.Total.ToString("C", ptBR), x.TicketMedio.ToString("C", ptBR));

            gridAbc.Rows.Clear();
            foreach (var x in await _svc.CurvaAbcAsync(de, ate))
            {
                int idx = gridAbc.Rows.Add(x.Codigo, x.Descricao, x.Quantidade.ToString("N3"), x.Faturamento.ToString("C", ptBR), x.Classe);
                var cell = gridAbc.Rows[idx].Cells["Classe"];
                cell.Style.Font = Tema.FontCorpoBold;
                cell.Style.ForeColor = x.Classe switch
                {
                    "A" => Tema.CorSucesso,
                    "B" => Tema.CorAlerta,
                    _ => Tema.CorErro
                };
            }

            gridTop.Rows.Clear();
            foreach (var x in await _svc.TopProdutosAsync(de, ate, 50))
                gridTop.Rows.Add(x.Codigo, x.Descricao, x.Quantidade.ToString("N3"), x.Faturamento.ToString("C", ptBR));

            gridFluxo.Rows.Clear();
            foreach (var x in await _svc.FluxoCaixaAsync(de, ate))
            {
                int idx = gridFluxo.Rows.Add(x.Dia.ToString("dd/MM/yyyy"), x.Entradas.ToString("C", ptBR), x.Saidas.ToString("C", ptBR), x.Saldo.ToString("C", ptBR));
                var cell = gridFluxo.Rows[idx].Cells[3];
                cell.Style.Font = Tema.FontCorpoBold;
                cell.Style.ForeColor = x.Saldo >= 0 ? Tema.CorSucesso : Tema.CorErro;
            }

            _resumo.Text = $"Período {de:dd/MM/yyyy} a {ate.AddDays(-1):dd/MM/yyyy}     •     Total vendido: {totalDia.ToString("C", ptBR)}";
        }
        finally { UseWaitCursor = false; }
    }

    private void ExportarAtualCsv()
    {
        var atual = tabs.SelectedTab;
        var grid = atual?.Controls.OfType<Card>().FirstOrDefault()?.Controls.OfType<StyledGrid>().FirstOrDefault();
        if (grid == null || grid.Rows.Count == 0)
        {
            Toast.Mostrar("Nada para exportar.", TipoToast.Info, owner: this);
            return;
        }

        using var sfd = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = $"{atual!.Text.Trim().Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.csv" };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        var sb = new StringBuilder();
        var headers = new List<string>();
        foreach (DataGridViewColumn c in grid.Columns) headers.Add(EscapeCsv(c.HeaderText));
        sb.AppendLine(string.Join(";", headers));
        foreach (DataGridViewRow r in grid.Rows)
        {
            var cels = new List<string>();
            foreach (DataGridViewCell c in r.Cells) cels.Add(EscapeCsv(c.Value?.ToString() ?? ""));
            sb.AppendLine(string.Join(";", cels));
        }
        File.WriteAllText(sfd.FileName, sb.ToString(), new UTF8Encoding(true));
        Toast.Mostrar($"Exportado: {Path.GetFileName(sfd.FileName)}", TipoToast.Sucesso, owner: this);
    }

    private static string EscapeCsv(string s)
    {
        if (s.Contains(';') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}
