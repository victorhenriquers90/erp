using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Enums;
using ProjetoVarejo.Domain.Configuracao;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;
using static ProjetoVarejo.Application.Configuracao.TemasNegocio;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmMain : Form
{
    private readonly SessaoApp _sessao;
    private readonly ImplantacaoService _implantacaoService;
    private readonly ConfiguracaoNegocioService _configuracaoService;
    private ImplantacaoConfig _implantacao;
    private ProjetoVarejo.Domain.Configuracao.ConfiguracaoNegocio _configuracaoNegocio = null!;
    private Panel _conteudo = null!;
    private FlowLayoutPanel _kpis = null!;
    private LineChart _grafico = null!;

    public FrmMain(SessaoApp sessao, ImplantacaoService implantacaoService, ConfiguracaoNegocioService configuracaoService)
    {
        _sessao = sessao;
        _implantacaoService = implantacaoService;
        _configuracaoService = configuracaoService;

        // Use defaults initially - load async in Load event
        _implantacao = new ImplantacaoConfig();
        _configuracaoNegocio = new ProjetoVarejo.Domain.Configuracao.ConfiguracaoNegocio
        {
            Id = 1,
            TipoNegocio = (TipoNegocio)0,
            ConfiguracaoInicial = false,
            ModulosAtivos = ModuloSistema.PDV | ModuloSistema.Estoque | ModuloSistema.Cadastros | ModuloSistema.Financeiro,
            DataAtualizacao = DateTime.Now,
            Versao = 1
        };

        InitUi();

        // Load actual configuration asynchronously after UI is created
        Shown += async (s, e) => await CarregarConfiguraçaoAsync();
    }

    private async Task CarregarConfiguraçaoAsync()
    {
        try
        {
            _implantacao = await _implantacaoService.ObterAsync();
            _configuracaoNegocio = await _configuracaoService.ObterConfiguracao();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Aviso ao carregar configurações:\n\n{ex.Message}",
                "Aviso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            // Continue with defaults
        }
    }

    private void InitUi()
    {
        Text = Tema.NomeProduto;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Tema.CorFundo;
        Font = Tema.FontCorpo;

        // Usar marca com nome e ícone customizados do tipo de negócio
        string marcaTexto, marcaIcone;
        if (_configuracaoNegocio?.ConfiguracaoInicial == true && _configuracaoNegocio.TipoNegocio != (TipoNegocio)0)
        {
            marcaTexto = _configuracaoNegocio.ObterDescricaoTipo();
            marcaIcone = ObterIcone(_configuracaoNegocio.TipoNegocio);
        }
        else
        {
            marcaTexto = Tema.NomeProdutoCurto;
            marcaIcone = "PV";
        }

        var sidebar = new Sidebar(ConstruirSecoes(), marcaTexto: marcaTexto, marcaIcone: marcaIcone);

        // === Topbar moderna ===
        var topbar = new Topbar(
            nomeUsuario: _sessao.UsuarioLogado?.Nome ?? "Usuário",
            nomeEmpresa: _sessao.EmpresaAtiva?.NomeFantasia ?? _sessao.EmpresaAtiva?.RazaoSocial ?? "",
            notificacoes: 0)
        {
            OnSearch = MostrarDashboard,
            OnLogout = () => Close(),
            OnNotificacoes = () => Toast.Mostrar("Sem notificações novas.", TipoToast.Info, owner: this)
        };

        _conteudo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(18) };
        var statusbar = ConstruirStatusBar();

        Controls.Add(_conteudo);
        Controls.Add(statusbar);
        Controls.Add(topbar);
        Controls.Add(sidebar);

        MostrarDashboard();
    }

    private List<SidebarSecao> ConstruirSecoes()
    {
        var secoes = new List<SidebarSecao>
        {
            new()
            {
                Titulo = "Principal",
                Itens = new()
                {
                    new() { Icone = Tema.IconHome, Texto = "Painel", OnClick = MostrarDashboard }
                }
            }
        };

        var vendas = new List<SidebarItem>();
        AdicionarSeAtivo(vendas, ModuloSistema.PDV, Tema.IconVendas, "PDV", () => ScopedFormHelper.AbrirModal<FrmPdv>(this));
        AdicionarSeAtivo(vendas, ModuloSistema.PDV, Tema.IconCaixa, "Caixa", () => ScopedFormHelper.AbrirModal<FrmCaixa>(this));
        AdicionarSeAtivo(vendas, ModuloSistema.Fiscal, Tema.IconNotas, "Notas Fiscais", () => ScopedFormHelper.AbrirModal<FrmNotasFiscais>(this));
        AdicionarSecaoSeTiverItens(secoes, "Vendas", vendas);

        var cadastros = new List<SidebarItem>();
        AdicionarSeAtivo(cadastros, ModuloSistema.Cadastros, Tema.IconProdutos, "Produtos", () => ScopedFormHelper.AbrirModal<FrmProdutos>(this));
        AdicionarSeAtivo(cadastros, ModuloSistema.Cadastros, Tema.IconClientes, "Clientes", () => ScopedFormHelper.AbrirModal<FrmClientes>(this));
        AdicionarSeAtivo(cadastros, ModuloSistema.Cadastros, Tema.IconFornecedores, "Fornecedores", () => ScopedFormHelper.AbrirModal<FrmFornecedores>(this));
        AdicionarSecaoSeTiverItens(secoes, "Cadastros", cadastros);

        var suprimentos = new List<SidebarItem>();
        AdicionarSeAtivo(suprimentos, ModuloSistema.Estoque, Tema.IconEstoque, "Estoque", () => ScopedFormHelper.AbrirModal<FrmEstoque>(this));
        AdicionarSeAtivo(suprimentos, ModuloSistema.Fiscal, Tema.IconUpload, "Importar NF-e", () => ScopedFormHelper.AbrirModal<FrmImportarNfe>(this));
        AdicionarSecaoSeTiverItens(secoes, "Suprimentos", suprimentos);

        var gestao = new List<SidebarItem>();
        AdicionarSeAtivo(gestao, ModuloSistema.Financeiro, Tema.IconFinanceiro, "Financeiro", () => ScopedFormHelper.AbrirModal<FrmFinanceiro>(this));
        AdicionarSeAtivo(gestao, ModuloSistema.Relatorios, Tema.IconRelatorios, "Relatorios", () => ScopedFormHelper.AbrirModal<FrmRelatorios>(this));
        AdicionarSeAtivo(gestao, ModuloSistema.PDV | ModuloSistema.Financeiro, "📋", "Fechamento do Dia", () => ScopedFormHelper.AbrirModal<FrmFechamentoDia>(this));
        AdicionarSecaoSeTiverItens(secoes, "Gestao", gestao);

        var sistema = new List<SidebarItem>();
        AdicionarSeAtivo(sistema, ModuloSistema.Backup, Tema.IconBackup, "Backup", () => ScopedFormHelper.AbrirModal<FrmBackup>(this));
        AdicionarSeAtivo(sistema, ModuloSistema.Auditoria, Tema.IconAuditoria, "Auditoria", () => ScopedFormHelper.AbrirModal<FrmAuditoria>(this));
        AdicionarSeAtivo(sistema, ModuloSistema.Producao, Tema.IconChecklist, "Checklist de Producao", () => ScopedFormHelper.AbrirModal<FrmChecklistProducao>(this));
        sistema.Add(new SidebarItem { Icone = "🏢", Texto = "Filiais", OnClick = () => ScopedFormHelper.AbrirModal<FrmFiliais>(this) });
        sistema.Add(new SidebarItem { Icone = Tema.IconUsuario, Texto = "Usuarios", OnClick = () => ScopedFormHelper.AbrirModal<FrmUsuarios>(this) });
        sistema.Add(new SidebarItem { Icone = "⚙", Texto = "Gerenciar Módulos", OnClick = () => ScopedFormHelper.AbrirModal<FrmGerenciadorModulos>(this) });
        AdicionarSeAtivo(sistema, ModuloSistema.Relatorios, "📊", "Relat. Configuracao", () => ScopedFormHelper.AbrirModal<FrmRelatorioConfiguracao>(this));
        sistema.Add(new SidebarItem { Icone = Tema.IconConfig, Texto = "Configuracoes", OnClick = () => ScopedFormHelper.AbrirModal<FrmConfigEmpresa>(this) });
        sistema.Add(new SidebarItem { Icone = Tema.IconConfig, Texto = "Perfil de Implantacao", OnClick = AbrirImplantacao });
        AdicionarSecaoSeTiverItens(secoes, "Sistema", sistema);

        return secoes;
    }

    private void AdicionarSeAtivo(List<SidebarItem> itens, ModuloSistema modulo, string icone, string texto, Action onClick)
    {
        if (!ModuloAtivo(modulo)) return;
        itens.Add(new SidebarItem { Icone = icone, Texto = texto, OnClick = onClick });
    }

    private static void AdicionarSecaoSeTiverItens(List<SidebarSecao> secoes, string titulo, List<SidebarItem> itens)
    {
        if (itens.Count == 0) return;
        secoes.Add(new SidebarSecao { Titulo = titulo, Itens = itens });
    }

    private bool ModuloAtivo(ModuloSistema modulo)
    {
        // Verificar se está configurado na nova tabela ConfiguracaoNegocio
        if (_configuracaoNegocio?.ConfiguracaoInicial == true)
        {
            return _configuracaoNegocio.EstaModuloAtivo(modulo);
        }

        // Fallback para o sistema antigo (ImplantacaoService)
        return _implantacaoService.ModuloAtivo(_implantacao, modulo);
    }

    private async void AbrirImplantacao()
    {
        using var scope = Program.Services.CreateScope();
        using var form = scope.ServiceProvider.GetRequiredService<FrmImplantacao>();
        if (form.ShowDialog(this) == DialogResult.OK)
            await RecarregarShellAsync();
    }

    private async Task RecarregarShellAsync()
    {
        _implantacao = await _implantacaoService.ObterAsync();
        Controls.Clear();
        InitUi();
    }

    private Panel ConstruirStatusBar()
    {
        var empresa = _sessao.EmpresaAtiva?.NomeFantasia ?? _sessao.EmpresaAtiva?.RazaoSocial ?? "Empresa nao selecionada";
        var usuario = _sessao.UsuarioLogado?.Nome ?? "Usuario";
        var perfil = _implantacaoService.NomePerfil(_implantacao.Perfil);
        var statusbar = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            BackColor = Tema.Branco
        };

        var contexto = new Label
        {
            Text = $"Empresa: {empresa}",
            Dock = DockStyle.Left,
            Width = 420,
            Padding = new Padding(18, 0, 0, 0),
            Font = Tema.FontMicro,
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblDb = new Label
        {
            Text = "● DB",
            Dock = DockStyle.Right,
            Width = 52,
            Font = Tema.FontMicro,
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Help
        };
        lblDb.MouseHover += (s, e) =>
            new ToolTip().SetToolTip(lblDb, lblDb.Tag?.ToString() ?? "Verificando banco de dados...");

        var ambiente = new Label
        {
            Text = $"Usuario: {usuario}   |   Perfil: {perfil}   |   Desktop   |   {Tema.NomeProduto}",
            Dock = DockStyle.Right,
            Width = 620,
            Padding = new Padding(0, 0, 8, 0),
            Font = Tema.FontMicro,
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleRight
        };

        statusbar.Controls.Add(contexto);
        statusbar.Controls.Add(lblDb);
        statusbar.Controls.Add(ambiente);
        statusbar.Paint += (s, e) =>
        {
            using var pen = new Pen(Tema.CorBorda, 1);
            e.Graphics.DrawLine(pen, 0, 0, statusbar.Width, 0);
        };

        // Verificar DB em background ao abrir
        _ = Task.Run(async () =>
        {
            await Task.Delay(1500);
            try
            {
                using var scope = Program.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ProjetoVarejo.Infrastructure.Data.AppDbContext>();
                var ok = await db.Database.CanConnectAsync();
                if (lblDb.IsDisposed) return;
                lblDb.Invoke(() =>
                {
                    lblDb.Text = ok ? "● DB" : "● DB";
                    lblDb.ForeColor = ok ? Tema.CorSucesso : Tema.CorErro;
                    lblDb.Tag = ok ? $"Banco conectado — {DateTime.Now:HH:mm:ss}" : "Banco INDISPONÍVEL";
                });
            }
            catch
            {
                if (!lblDb.IsDisposed)
                    lblDb.Invoke(() => { lblDb.ForeColor = Tema.CorErro; lblDb.Tag = "Erro ao verificar banco."; });
            }
        });

        return statusbar;
    }

    private void MostrarDashboard()
    {
        _conteudo.Controls.Clear();

        // === Cabeçalho ===
        var header = Inputs.HeaderPagina(
            "Painel operacional",
            "Indicadores, processos e módulos principais para operação diária",
            84);

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

        // === Gráfico, mapa de módulos e atalhos ===
        var corpo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(0, 6, 0, 0) };

        var colunaPrincipal = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo };

        var cardGrafico = new Card { Dock = DockStyle.Fill, Padding = new Padding(18) };
        _grafico = new LineChart { Dock = DockStyle.Fill, Titulo = "Faturamento dos ultimos 14 dias" };
        cardGrafico.Controls.Add(_grafico);

        var mapaModulos = ConstruirMapaModulos();
        var spacerVertical = new Panel { Dock = DockStyle.Bottom, Height = 14, BackColor = Tema.CorFundo };

        colunaPrincipal.Controls.Add(cardGrafico);
        colunaPrincipal.Controls.Add(spacerVertical);
        colunaPrincipal.Controls.Add(mapaModulos);

        var atalhos = new Card { Dock = DockStyle.Right, Width = 332, Padding = new Padding(18) };
        var lblAtalhos = new Label
        {
            Text = "Processos frequentes",
            Dock = DockStyle.Top, Height = 28,
            Font = Tema.FontSubtitulo,
            ForeColor = Tema.CorTextoEscuro
        };
        var lblContexto = new Label
        {
            Text = "Ações de balcão, caixa e gestão",
            Dock = DockStyle.Top,
            Height = 24,
            Font = Tema.FontPequena,
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        };
        var pnlAtalhos = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent
        };
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.PDV, Tema.IconVendas, "Nova venda", () => ScopedFormHelper.AbrirModal<FrmPdv>(this));
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.PDV, Tema.IconCaixa, "Abrir / fechar caixa", () => ScopedFormHelper.AbrirModal<FrmCaixa>(this));
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.Cadastros, Tema.IconProdutos, "Consultar produtos", () => ScopedFormHelper.AbrirModal<FrmProdutos>(this));
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.Estoque, Tema.IconEstoque, "Movimentar estoque", () => ScopedFormHelper.AbrirModal<FrmEstoque>(this));
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.Financeiro, Tema.IconFinanceiro, "Contas financeiras", () => ScopedFormHelper.AbrirModal<FrmFinanceiro>(this));
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.Fiscal, Tema.IconNotas, "Notas fiscais", () => ScopedFormHelper.AbrirModal<FrmNotasFiscais>(this));
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.Backup, Tema.IconBackup, "Backup e auditoria", () => ScopedFormHelper.AbrirModal<FrmBackup>(this));
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.Producao, Tema.IconChecklist, "Checklist de producao", () => ScopedFormHelper.AbrirModal<FrmChecklistProducao>(this));
        // Usuarios e Relatorios não têm gating de módulos
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.PDV, Tema.IconUsuario, "Usuarios e acessos", () => ScopedFormHelper.AbrirModal<FrmUsuarios>(this));
        AdicionarAtalhoSeAtivo(pnlAtalhos, ModuloSistema.Relatorios, Tema.IconRelatorios, "Relatorios", () => ScopedFormHelper.AbrirModal<FrmRelatorios>(this));
        atalhos.Controls.Add(pnlAtalhos);
        atalhos.Controls.Add(lblContexto);
        atalhos.Controls.Add(lblAtalhos);

        var spacer = new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Tema.CorFundo };

        corpo.Controls.Add(colunaPrincipal);
        corpo.Controls.Add(spacer);
        corpo.Controls.Add(atalhos);

        _conteudo.Controls.Add(corpo);
        _conteudo.Controls.Add(_kpis);
        _conteudo.Controls.Add(header);

        _ = CarregarDadosAsync();
    }

    private Card ConstruirMapaModulos()
    {
        var card = new Card { Dock = DockStyle.Bottom, Height = 198, Padding = new Padding(18) };
        var header = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.Transparent };
        var titulo = new Label
        {
            Text = "Mapa de modulos",
            Dock = DockStyle.Top,
            Height = 24,
            Font = Tema.FontSubtitulo,
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent
        };
        var subtitulo = new Label
        {
            Text = "Acesso rapido as areas que compoem a operacao do ERP",
            Dock = DockStyle.Top,
            Height = 20,
            Font = Tema.FontPequena,
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        };
        header.Controls.Add(subtitulo);
        header.Controls.Add(titulo);

        var grid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        // Obter cor do tema do negócio, com fallback para cores padrão
        var corTema = _configuracaoNegocio?.ConfiguracaoInicial == true && _configuracaoNegocio.TipoNegocio != (TipoNegocio)0
            ? ObterCorPrimaria(_configuracaoNegocio.TipoNegocio)
            : Tema.CorPrimaria;

        if (ModuloAtivo(ModuloSistema.PDV))
            grid.Controls.Add(ModuloTile(Tema.IconVendas, "PDV e Caixa", "Venda, caixa e recebimento", corTema, () => ScopedFormHelper.AbrirModal<FrmPdv>(this)));
        if (ModuloAtivo(ModuloSistema.Cadastros))
            grid.Controls.Add(ModuloTile(Tema.IconProdutos, "Cadastros", "Produtos, clientes e fornecedores", corTema, () => ScopedFormHelper.AbrirModal<FrmProdutos>(this)));
        if (ModuloAtivo(ModuloSistema.Estoque))
            grid.Controls.Add(ModuloTile(Tema.IconEstoque, "Estoque", "Movimentos e alertas", corTema, () => ScopedFormHelper.AbrirModal<FrmEstoque>(this)));
        if (ModuloAtivo(ModuloSistema.Financeiro))
            grid.Controls.Add(ModuloTile(Tema.IconFinanceiro, "Financeiro", "Contas e quitacoes", corTema, () => ScopedFormHelper.AbrirModal<FrmFinanceiro>(this)));
        if (ModuloAtivo(ModuloSistema.Fiscal))
            grid.Controls.Add(ModuloTile(Tema.IconNotas, "Fiscal", "NF-e, NFC-e e XML", corTema, () => ScopedFormHelper.AbrirModal<FrmNotasFiscais>(this)));
        if (ModuloAtivo(ModuloSistema.Auditoria) || ModuloAtivo(ModuloSistema.Backup))
            grid.Controls.Add(ModuloTile(Tema.IconAuditoria, "Governanca", "Backup e auditoria", corTema, () => ScopedFormHelper.AbrirModal<FrmAuditoria>(this)));
        if (ModuloAtivo(ModuloSistema.Producao))
            grid.Controls.Add(ModuloTile(Tema.IconChecklist, "Producao", "Checklist de go-live", corTema, () => ScopedFormHelper.AbrirModal<FrmChecklistProducao>(this)));

        card.Controls.Add(grid);
        card.Controls.Add(header);
        return card;
    }

    private Control ModuloTile(string icone, string titulo, string subtitulo, Color cor, Action onClick)
    {
        var pnl = new Panel
        {
            Width = 226,
            Height = 62,
            Margin = new Padding(0, 0, 10, 10),
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
            using var pen = new Pen(Tema.CorBorda, 1);
            g.FillPath(brush, path);
            g.DrawPath(pen, path);

            var iconRect = new Rectangle(12, 13, 36, 36);
            using var iconBg = new SolidBrush(Color.FromArgb(28, cor));
            using var iconPath = Tema.PathArredondado(iconRect, Tema.RaioBotao);
            g.FillPath(iconBg, iconPath);
            using var fontIcone = Tema.FontIcone(14);
            TextRenderer.DrawText(g, icone, fontIcone, iconRect, cor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        var lblTitulo = new Label
        {
            Text = titulo,
            Left = 58,
            Top = 10,
            Width = 154,
            Height = 22,
            Font = Tema.FontCorpoBold,
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent
        };
        var lblSub = new Label
        {
            Text = subtitulo,
            Left = 58,
            Top = 32,
            Width = 154,
            Height = 20,
            Font = Tema.FontMicro,
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        };
        pnl.Controls.Add(lblSub);
        pnl.Controls.Add(lblTitulo);

        EventHandler click = (s, e) => onClick();
        pnl.Click += click;
        lblTitulo.Click += click;
        lblSub.Click += click;
        pnl.MouseEnter += (s, e) => { pnl.BackColor = Tema.CorPrimariaSoft; lblTitulo.BackColor = Tema.CorPrimariaSoft; lblSub.BackColor = Tema.CorPrimariaSoft; };
        pnl.MouseLeave += (s, e) => { pnl.BackColor = Tema.CorCardAlt; lblTitulo.BackColor = Tema.CorCardAlt; lblSub.BackColor = Tema.CorCardAlt; };
        return pnl;
    }

    private void AdicionarAtalhoSeAtivo(FlowLayoutPanel painel, ModuloSistema modulo, string icone, string texto, Action onClick)
    {
        if (!ModuloAtivo(modulo)) return;
        painel.Controls.Add(BotaoAtalho(icone, texto, onClick));
    }

    private Control BotaoAtalho(string icone, string texto, Action onClick)
    {
        var pnl = new Panel
        {
            Width = 292,
            Height = 46,
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
            Text = "\uE76C",  // chevron right
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
                if (ModuloAtivo(ModuloSistema.PDV))
                {
                    _kpis.Controls.Add(new KpiCard("Vendas hoje", totalHoje.ToString("C"), Tema.IconCaixa, Tema.CorSucesso, variacaoTotal));
                    _kpis.Controls.Add(new KpiCard("Transacoes", qtdHoje.ToString(), Tema.IconVendas, Tema.CorPrimaria, variacaoQtd));
                    _kpis.Controls.Add(new KpiCard("Ticket medio", ticketMedio.ToString("C"), Tema.IconRelatorios, Tema.CorInfo, variacaoTicket));
                }

                if (ModuloAtivo(ModuloSistema.Estoque))
                    _kpis.Controls.Add(new KpiCard("Alertas estoque", alertasEstoque.ToString(), Tema.IconAlerta, alertasEstoque > 0 ? Tema.CorAlerta : Tema.CorNeutro));

                if (ModuloAtivo(ModuloSistema.Financeiro))
                    _kpis.Controls.Add(new KpiCard("A pagar (7d)", contasVencer.ToString("C"), Tema.IconFinanceiro, contasVencer > 0 ? Tema.CorErro : Tema.CorNeutro));

                _kpis.Controls.Add(new KpiCard("Perfil", _implantacaoService.NomePerfil(_implantacao.Perfil), Tema.IconConfig, Tema.CorNeutro));

                _grafico.DefinirDados(serie, rotulos);
            });
        }
        catch
        {
            // dashboard não pode derrubar o app
        }
    }
}
