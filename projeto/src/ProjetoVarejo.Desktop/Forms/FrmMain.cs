using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmMain : Form
{
    private readonly SessaoApp _sessao;
    private Panel _conteudo = null!;
    private FlowLayoutPanel _kpis = null!;
    private LineChart _grafico = null!;

    public FrmMain(SessaoApp sessao)
    {
        _sessao = sessao;
        InitUi();
    }

    private void InitUi()
    {
        Text = "ProjetoVarejo";
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Tema.CorFundo;
        Font = Tema.FontCorpo;

        // === Sidebar com seções ===
        var sidebar = new Sidebar(new List<SidebarSecao>
        {
            new()
            {
                Titulo = "Principal",
                Itens = new()
                {
                    new() { Icone = Tema.IconHome, Texto = "Cockpit", OnClick = MostrarDashboard }
                }
            },
            new()
            {
                Titulo = "Operacional",
                Itens = new()
                {
                    new() { Icone = Tema.IconVendas, Texto = "PDV", OnClick = () => ScopedFormHelper.AbrirModal<FrmPdv>(this) },
                    new() { Icone = Tema.IconCaixa, Texto = "Caixa", OnClick = () => ScopedFormHelper.AbrirModal<FrmCaixa>(this) },
                    new() { Icone = Tema.IconNotas, Texto = "Notas Fiscais", OnClick = () => ScopedFormHelper.AbrirModal<FrmNotasFiscais>(this) }
                }
            },
            new()
            {
                Titulo = "Cadastros",
                Itens = new()
                {
                    new() { Icone = Tema.IconProdutos, Texto = "Produtos", OnClick = () => ScopedFormHelper.AbrirModal<FrmProdutos>(this) },
                    new() { Icone = Tema.IconClientes, Texto = "Clientes", OnClick = () => ScopedFormHelper.AbrirModal<FrmClientes>(this) },
                    new() { Icone = Tema.IconFornecedores, Texto = "Fornecedores", OnClick = () => ScopedFormHelper.AbrirModal<FrmFornecedores>(this) }
                }
            },
            new()
            {
                Titulo = "Gestão",
                Itens = new()
                {
                    new() { Icone = Tema.IconEstoque, Texto = "Estoque", OnClick = () => ScopedFormHelper.AbrirModal<FrmEstoque>(this) },
                    new() { Icone = Tema.IconUpload, Texto = "Importar XML", OnClick = () => ScopedFormHelper.AbrirModal<FrmImportarNfe>(this) },
                    new() { Icone = Tema.IconFinanceiro, Texto = "Financeiro", OnClick = () => ScopedFormHelper.AbrirModal<FrmFinanceiro>(this) },
                    new() { Icone = Tema.IconRelatorios, Texto = "Relatórios", OnClick = () => ScopedFormHelper.AbrirModal<FrmRelatorios>(this) }
                }
            },
            new()
            {
                Titulo = "Sistema",
                Itens = new()
                {
                    new() { Icone = Tema.IconConfig, Texto = "Configurações", OnClick = () => ScopedFormHelper.AbrirModal<FrmConfigEmpresa>(this) }
                }
            }
        }, marcaTexto: "ProjetoVarejo", marcaIcone: "");  //

        // === Topbar moderna ===
        var topbar = new Topbar(
            nomeUsuario: _sessao.UsuarioLogado?.Nome ?? "Usuário",
            nomeEmpresa: _sessao.EmpresaAtiva?.NomeFantasia ?? _sessao.EmpresaAtiva?.RazaoSocial ?? "",
            notificacoes: 0)
        {
            OnSearch = () => Toast.Mostrar("Busca global em breve.", TipoToast.Info, owner: this),
            OnLogout = () => Close(),
            OnNotificacoes = () => Toast.Mostrar("Sem notificações novas.", TipoToast.Info, owner: this)
        };

        _conteudo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(18) };

        Controls.Add(_conteudo);
        Controls.Add(topbar);
        Controls.Add(sidebar);

        MostrarDashboard();
    }

    private void MostrarDashboard()
    {
        _conteudo.Controls.Clear();

        // === Cabeçalho ===
        var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Tema.CorFundo };
        var titulo = new Label
        {
            Text = "Cockpit operacional",
            Dock = DockStyle.Top, Height = 34,
            Font = Tema.FontTituloGrande,
            ForeColor = Tema.CorTextoEscuro,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var sub = new Label
        {
            Text = "Visão geral do desempenho da sua loja",
            Dock = DockStyle.Top, Height = 24,
            Font = Tema.FontCorpo,
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(sub);
        header.Controls.Add(titulo);

        // === KPIs ===
        _kpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 142,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Tema.CorFundo,
            Padding = new Padding(0, 2, 0, 8)
        };

        // === Gráfico (lado esquerdo) + Atalhos (lado direito) ===
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(0, 6, 0, 0) };

        var cardGrafico = new Card { Dock = DockStyle.Fill, Padding = new Padding(18) };
        _grafico = new LineChart { Dock = DockStyle.Fill, Titulo = "Vendas dos últimos 14 dias" };
        cardGrafico.Controls.Add(_grafico);

        var atalhos = new Card { Dock = DockStyle.Right, Width = 304, Padding = new Padding(18) };
        var lblAtalhos = new Label
        {
            Text = "Atalhos rápidos",
            Dock = DockStyle.Top, Height = 28,
            Font = Tema.FontSubtitulo,
            ForeColor = Tema.CorTextoEscuro
        };
        var pnlAtalhos = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        pnlAtalhos.Controls.Add(BotaoAtalho(Tema.IconVendas, "Nova venda", () => ScopedFormHelper.AbrirModal<FrmPdv>(this)));
        pnlAtalhos.Controls.Add(BotaoAtalho(Tema.IconProdutos, "Novo produto", () => ScopedFormHelper.AbrirModal<FrmProdutos>(this)));
        pnlAtalhos.Controls.Add(BotaoAtalho(Tema.IconCaixa, "Abrir / fechar caixa", () => ScopedFormHelper.AbrirModal<FrmCaixa>(this)));
        pnlAtalhos.Controls.Add(BotaoAtalho(Tema.IconRelatorios, "Ver relatórios", () => ScopedFormHelper.AbrirModal<FrmRelatorios>(this)));
        atalhos.Controls.Add(pnlAtalhos);
        atalhos.Controls.Add(lblAtalhos);

        var spacer = new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Tema.CorFundo };

        corpo.Controls.Add(cardGrafico);
        corpo.Controls.Add(spacer);
        corpo.Controls.Add(atalhos);

        _conteudo.Controls.Add(corpo);
        _conteudo.Controls.Add(_kpis);
        _conteudo.Controls.Add(header);

        _ = CarregarDadosAsync();
    }

    private Control BotaoAtalho(string icone, string texto, Action onClick)
    {
        var pnl = new Panel
        {
            Width = 262,
            Height = 44,
            Margin = new Padding(0, 3, 0, 5),
            BackColor = Tema.CorCardAlt,
            Cursor = Cursors.Hand
        };
        pnl.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
            using var path = Tema.PathArredondado(rect, Tema.RaioBotao);
            using var brush = new SolidBrush(pnl.BackColor);
            g.FillPath(brush, path);
            using var pen = new Pen(Tema.CorBorda, 1);
            g.DrawPath(pen, path);
        };
        var lblIcone = new Label
        {
            Text = icone,
            Dock = DockStyle.Left, Width = 46,
            Font = new Font("Segoe MDL2 Assets", 14),
            ForeColor = Tema.CorPrimaria,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        var lblTexto = new Label
        {
            Text = texto,
            Dock = DockStyle.Fill,
            Font = Tema.FontCorpoBold,
            ForeColor = Tema.CorTextoEscuro,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent
        };
        var lblSeta = new Label
        {
            Text = "",  // chevron right
            Dock = DockStyle.Right, Width = 30,
            Font = new Font("Segoe MDL2 Assets", 10),
            ForeColor = Tema.CorTextoClaro,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        pnl.Controls.Add(lblTexto);
        pnl.Controls.Add(lblSeta);
        pnl.Controls.Add(lblIcone);

        EventHandler click = (s, e) => onClick();
        pnl.Click += click;
        lblIcone.Click += click;
        lblTexto.Click += click;
        lblSeta.Click += click;

        pnl.MouseEnter += (s, e) => { pnl.BackColor = Tema.CorPrimariaSoft; lblTexto.BackColor = Tema.CorPrimariaSoft; lblIcone.BackColor = Tema.CorPrimariaSoft; lblSeta.BackColor = Tema.CorPrimariaSoft; };
        pnl.MouseLeave += (s, e) => { pnl.BackColor = Tema.CorCardAlt; lblTexto.BackColor = Tema.CorCardAlt; lblIcone.BackColor = Tema.CorCardAlt; lblSeta.BackColor = Tema.CorCardAlt; };
        return pnl;
    }

    private async Task CarregarDadosAsync()
    {
        try
        {
            using var scope = Program.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hojeIni = DateTime.Today;
            var hojeFim = hojeIni.AddDays(1);
            var ontemIni = hojeIni.AddDays(-1);

            // Período atual
            var vendasHoje = await db.Vendas
                .Where(v => v.Status == StatusVenda.Finalizada
                         && v.FinalizadaEm >= hojeIni && v.FinalizadaEm < hojeFim)
                .ToListAsync();
            var totalHoje = vendasHoje.Sum(v => v.Total);
            var qtdHoje = vendasHoje.Count;
            var ticketMedio = qtdHoje > 0 ? totalHoje / qtdHoje : 0;

            // Período anterior (ontem) — para calcular variação
            var vendasOntem = await db.Vendas
                .Where(v => v.Status == StatusVenda.Finalizada
                         && v.FinalizadaEm >= ontemIni && v.FinalizadaEm < hojeIni)
                .ToListAsync();
            var totalOntem = vendasOntem.Sum(v => v.Total);
            var qtdOntem = vendasOntem.Count;
            var ticketOntem = qtdOntem > 0 ? totalOntem / qtdOntem : 0;

            decimal? variacaoTotal = totalOntem > 0 ? ((totalHoje - totalOntem) / totalOntem * 100m) : null;
            decimal? variacaoQtd = qtdOntem > 0 ? ((qtdHoje - qtdOntem) / (decimal)qtdOntem * 100m) : null;
            decimal? variacaoTicket = ticketOntem > 0 ? ((ticketMedio - ticketOntem) / ticketOntem * 100m) : null;

            var alertasEstoque = await db.Produtos
                .Where(p => p.Ativo && p.ControlaEstoque && p.Estoque <= p.EstoqueMinimo)
                .CountAsync();

            var contasVencer = await db.ContasFinanceiras
                .Where(c => c.Status == StatusConta.EmAberto
                         && c.Tipo == TipoConta.Pagar
                         && c.DataVencimento >= hojeIni
                         && c.DataVencimento < hojeIni.AddDays(7))
                .SumAsync(c => (decimal?)c.Valor) ?? 0;

            // Série 14 dias para o gráfico
            var inicio14 = hojeIni.AddDays(-13);
            var vendas14 = await db.Vendas
                .Where(v => v.Status == StatusVenda.Finalizada
                         && v.FinalizadaEm >= inicio14 && v.FinalizadaEm < hojeFim)
                .GroupBy(v => v.FinalizadaEm!.Value.Date)
                .Select(g => new { Dia = g.Key, Total = g.Sum(v => v.Total) })
                .ToListAsync();

            var dias = Enumerable.Range(0, 14).Select(i => inicio14.AddDays(i)).ToList();
            var serie = dias.Select(d => vendas14.FirstOrDefault(v => v.Dia == d)?.Total ?? 0m).ToList();
            var rotulos = dias.Select(d => d.ToString("dd/MM")).ToList();

            if (!IsHandleCreated || IsDisposed) return;
            Invoke(() =>
            {
                _kpis.Controls.Clear();
                _kpis.Controls.Add(new KpiCard("Vendas hoje", totalHoje.ToString("C"), Tema.IconCaixa, Tema.CorSucesso, variacaoTotal));
                _kpis.Controls.Add(new KpiCard("Transações", qtdHoje.ToString(), Tema.IconVendas, Tema.CorPrimaria, variacaoQtd));
                _kpis.Controls.Add(new KpiCard("Ticket médio", ticketMedio.ToString("C"), Tema.IconRelatorios, Tema.CorInfo, variacaoTicket));
                _kpis.Controls.Add(new KpiCard("Alertas estoque", alertasEstoque.ToString(), Tema.IconAlerta, alertasEstoque > 0 ? Tema.CorAlerta : Tema.CorNeutro));
                _kpis.Controls.Add(new KpiCard("A pagar (7d)", contasVencer.ToString("C"), Tema.IconFinanceiro, contasVencer > 0 ? Tema.CorErro : Tema.CorNeutro));

                _grafico.DefinirDados(serie, rotulos);
            });
        }
        catch
        {
            // dashboard não pode derrubar o app
        }
    }
}
