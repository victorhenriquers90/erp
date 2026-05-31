using System.Text;
using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Configuracao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Desktop.Forms;

/// <summary>
/// Fase 8 — Relatório de Configuração do Sistema Modular.
/// Exibe dashboard com KPIs, matriz módulo×tipo e trilha de auditoria da configuração.
/// </summary>
[ModuloRequerido(ModuloSistema.Relatorios)]
public class FrmRelatorioConfiguracao : Form
{
    private readonly ConfiguracaoNegocioService _cfgSvc;
    private readonly AuditLogService _auditSvc;

    private ConfiguracaoNegocio _config = null!;

    // Dashboard
    private KpiCard kpiTipo = null!, kpiAtivos = null!, kpiTotal = null!, kpiStatus = null!;
    private Label lblDescricao = null!, lblDataAtualizado = null!, lblModulosAtivosLista = null!;

    // Matriz
    private DataGridView gridMatriz = null!;

    // Auditoria
    private StyledGrid gridAudit = null!;

    public FrmRelatorioConfiguracao(ConfiguracaoNegocioService cfgSvc, AuditLogService auditSvc)
    {
        _cfgSvc = cfgSvc;
        _auditSvc = auditSvc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    // ────────────────────────────────────────────
    // UI
    // ────────────────────────────────────────────

    private void InitUi()
    {
        Text = "Relatório de Configuração";
        Size = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Relatório de Configuração",
            "Dashboard do sistema modular, matriz de módulos e trilha de auditoria.");
        header.Dock = DockStyle.Top;

        var btnAtualizar = Botoes.Ghost("↻  Atualizar", 130, 32);
        btnAtualizar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnAtualizar.Click += async (s, e) => await CarregarAsync();
        header.Controls.Add(btnAtualizar);
        header.Resize += (s, e) => btnAtualizar.Left = header.Width - 148;

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = Tema.FontCorpo,
            Padding = new Point(16, 6)
        };
        tabs.TabPages.Add(CriarAbaDashboard());
        tabs.TabPages.Add(CriarAbaMatriz());
        tabs.TabPages.Add(CriarAbaAuditoria());
        tabs.SelectedIndexChanged += async (s, e) =>
        {
            if (tabs.SelectedIndex == 1) PopularMatriz();
            if (tabs.SelectedIndex == 2) await CarregarAuditoriaAsync();
        };

        Controls.Add(tabs);
        Controls.Add(header);
    }

    // ── Aba Dashboard ──────────────────────────────────────────────

    private TabPage CriarAbaDashboard()
    {
        var tab = new TabPage("📊  Dashboard") { BackColor = Tema.CorFundo, Padding = new Padding(16) };

        // KPI row
        var pnlKpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 140,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 8)
        };

        kpiTipo    = new KpiCard("Tipo de Negócio", "–",    "🏪", Tema.CorPrimaria);
        kpiAtivos  = new KpiCard("Módulos Ativos",  "0",    "✓",  Tema.CorSucesso);
        kpiTotal   = new KpiCard("Total Módulos",   "16",   "□",  Tema.CorNeutro);
        kpiStatus  = new KpiCard("Status",          "–",    "!",  Tema.CorAlerta);

        pnlKpis.Controls.AddRange(new Control[] { kpiTipo, kpiAtivos, kpiTotal, kpiStatus });

        // Detail card
        var cardDetalhe = new Card { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;

        pnl.Controls.Add(Inputs.Rotulo("DESCRIÇÃO DO NEGÓCIO", 0, y));
        lblDescricao = new Label
        {
            Left = 0, Top = y + 20, Width = 900, Height = 28,
            Font = Tema.FontSubtitulo, ForeColor = Tema.CorTextoEscuro,
            AutoSize = false, BackColor = Color.Transparent
        };
        pnl.Controls.Add(lblDescricao);
        y += 58;

        pnl.Controls.Add(Inputs.Rotulo("ÚLTIMA ATUALIZAÇÃO", 0, y));
        lblDataAtualizado = new Label
        {
            Left = 0, Top = y + 20, Width = 400, Height = 28,
            Font = Tema.FontCorpo, ForeColor = Tema.CorTextoMedio,
            AutoSize = false, BackColor = Color.Transparent
        };
        pnl.Controls.Add(lblDataAtualizado);
        y += 56;

        pnl.Controls.Add(Inputs.Rotulo("MÓDULOS ATIVOS", 0, y));
        lblModulosAtivosLista = new Label
        {
            Left = 0, Top = y + 20, Width = 900, Height = 300,
            Font = Tema.FontCorpo, ForeColor = Tema.CorTextoEscuro,
            AutoSize = false, BackColor = Color.Transparent
        };
        pnl.Controls.Add(lblModulosAtivosLista);

        cardDetalhe.Controls.Add(pnl);

        tab.Controls.Add(cardDetalhe);
        tab.Controls.Add(pnlKpis);
        return tab;
    }

    // ── Aba Matriz ──────────────────────────────────────────────

    private TabPage CriarAbaMatriz()
    {
        var tab = new TabPage("🗂️  Matriz de Módulos") { BackColor = Tema.CorFundo, Padding = new Padding(16) };

        var pnlTopo = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
        var lbl = new Label
        {
            Text = "Módulos habilitados por padrão para cada tipo de negócio.",
            Font = Tema.FontPequena, ForeColor = Tema.CorTextoMedio,
            Dock = DockStyle.Left, Width = 500, TextAlign = ContentAlignment.MiddleLeft
        };
        var btnExport = Botoes.Ghost("Exportar CSV", 130, 32);
        btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnExport.Click += ExportarMatrizCsv;
        pnlTopo.Controls.Add(lbl);
        pnlTopo.Controls.Add(btnExport);
        pnlTopo.Resize += (s, e) => btnExport.Left = pnlTopo.Width - 140;

        gridMatriz = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            BackgroundColor = Tema.CorCard,
            GridColor = Tema.CorBordaSuave,
            BorderStyle = BorderStyle.None,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            EnableHeadersVisualStyles = false,
            Font = Tema.FontPequena
        };
        gridMatriz.ColumnHeadersDefaultCellStyle.BackColor = Tema.CorCardAlt;
        gridMatriz.ColumnHeadersDefaultCellStyle.ForeColor = Tema.CorTextoMedio;
        gridMatriz.ColumnHeadersDefaultCellStyle.Font = new Font(Tema.FontFamily, 8, FontStyle.Bold);
        gridMatriz.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        gridMatriz.DefaultCellStyle.BackColor = Tema.CorCard;
        gridMatriz.DefaultCellStyle.ForeColor = Tema.CorTextoEscuro;
        gridMatriz.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        gridMatriz.DefaultCellStyle.SelectionBackColor = Tema.CorPrimariaSoft;
        gridMatriz.DefaultCellStyle.SelectionForeColor = Tema.CorTextoEscuro;
        gridMatriz.RowsDefaultCellStyle.BackColor = Tema.CorCard;
        gridMatriz.AlternatingRowsDefaultCellStyle.BackColor = Tema.CorCardAlt;
        gridMatriz.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        gridMatriz.ColumnHeadersHeight = 80;
        gridMatriz.RowTemplate.Height = 32;

        tab.Controls.Add(gridMatriz);
        tab.Controls.Add(pnlTopo);
        return tab;
    }

    // ── Aba Auditoria ──────────────────────────────────────────────

    private TabPage CriarAbaAuditoria()
    {
        var tab = new TabPage("📋  Auditoria") { BackColor = Tema.CorFundo, Padding = new Padding(16) };

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(8) };
        gridAudit = new StyledGrid();
        gridAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Data",     HeaderText = "Data/Hora",   Width = 160 });
        gridAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Usuario",  HeaderText = "Usuário",     Width = 140 });
        gridAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Tipo",     HeaderText = "Tipo",        Width = 100 });
        gridAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Antes",    HeaderText = "Antes",       FillWeight = 40 });
        gridAudit.Columns.Add(new DataGridViewTextBoxColumn { Name = "Depois",   HeaderText = "Depois",      FillWeight = 40 });
        card.Controls.Add(gridAudit);

        tab.Controls.Add(card);
        return tab;
    }

    // ────────────────────────────────────────────
    // Carregamento de dados
    // ────────────────────────────────────────────

    private async Task CarregarAsync()
    {
        _config = await _cfgSvc.ObterConfiguracao();
        AtualizarDashboard();
    }

    private void AtualizarDashboard()
    {
        var todos  = ModulosPorTipo.ObterTodosModulos();
        var ativos = todos.Where(m => _config.EstaModuloAtivo(m)).ToList();
        var total  = todos.Length;
        var pct    = total > 0 ? (decimal)ativos.Count / total * 100 : 0;

        // KPIs
        kpiTipo.AtualizarValor(_config.TipoNegocio != 0
            ? NomeCurtoTipo(_config.TipoNegocio)
            : "Não definido");

        kpiAtivos.AtualizarValor($"{ativos.Count} / {total}");
        kpiTotal.AtualizarValor($"{total}");

        var (statusTexto, corStatus) = _config.ConfiguracaoInicial
            ? ("Configurado", Tema.CorSucesso)
            : ("Pendente", Tema.CorAlerta);
        kpiStatus.AtualizarValor(statusTexto);

        // Detalhes
        lblDescricao.Text = string.IsNullOrWhiteSpace(_config.DescricaoNegocio)
            ? _config.ObterDescricaoTipo()
            : _config.DescricaoNegocio;

        lblDataAtualizado.Text = _config.DataAtualizacao == default
            ? "–"
            : _config.DataAtualizacao.ToString("dd/MM/yyyy HH:mm");

        // Lista de módulos ativos
        var sb = new StringBuilder();
        foreach (var m in todos)
        {
            var ativo = _config.EstaModuloAtivo(m);
            sb.AppendLine($"  {(ativo ? "✔" : "○")}  {ModulosPorTipo.ObterDescricaoModulo(m)}{(ModulosPorTipo.EObrigatorio(m) ? "  [obrigatório]" : "")}");
        }
        lblModulosAtivosLista.Text = sb.ToString();
    }

    private void PopularMatriz()
    {
        if (gridMatriz.Columns.Count > 0) return; // já populado

        var tipos   = Enum.GetValues<TipoNegocio>();
        var modulos = ModulosPorTipo.ObterTodosModulos();

        // Coluna módulo (fixa à esquerda)
        gridMatriz.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Módulo",
            Name = "Modulo",
            Width = 220,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
        });

        // Uma coluna por tipo de negócio
        foreach (var tipo in tipos)
        {
            gridMatriz.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = NomeCurtoTipo(tipo),
                Name = tipo.ToString(),
                Width = 90
            });
        }

        // Linhas
        foreach (var modulo in modulos)
        {
            var row = new DataGridViewRow();
            row.CreateCells(gridMatriz);
            row.Cells[0].Value = ModulosPorTipo.ObterDescricaoModulo(modulo).Split(" –")[0].Split(" (")[0];
            row.Cells[0].Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            row.Cells[0].Style.Font = Tema.FontPequenaBold;

            int col = 1;
            foreach (var tipo in tipos)
            {
                var recomendados = ModulosPorTipo.ObterModulosRecomendados(tipo);
                var marcado = (recomendados & modulo) == modulo;
                row.Cells[col].Value = marcado ? "✔" : "·";
                row.Cells[col].Style.ForeColor = marcado ? Tema.CorSucesso : Tema.CorTextoClaro;
                if (marcado) row.Cells[col].Style.Font = new Font(Tema.FontFamily, 10, FontStyle.Bold);
                col++;
            }

            gridMatriz.Rows.Add(row);
        }

        // Destacar coluna do tipo atual
        if (_config?.TipoNegocio != 0)
        {
            var nomeTipo = _config!.TipoNegocio.ToString();
            if (gridMatriz.Columns[nomeTipo] is { } col)
            {
                col.HeaderCell.Style.BackColor = Tema.CorPrimariaSoft;
                col.HeaderCell.Style.ForeColor = Tema.CorPrimaria;
                col.DefaultCellStyle.BackColor = Tema.CorPrimariaSoft;
            }
        }
    }

    private async Task CarregarAuditoriaAsync()
    {
        if (gridAudit.Rows.Count > 0) return; // já carregado

        var registros = await _auditSvc.ListarAsync(entidade: "ConfiguracaoNegocio");
        gridAudit.Rows.Clear();

        if (registros.Count == 0)
        {
            // sem registros — mostrar linha informativa
            gridAudit.Rows.Add(
                DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                "–", "–", "Sem registros de auditoria para ConfiguracaoNegocio", "");
            return;
        }

        foreach (var r in registros)
        {
            gridAudit.Rows.Add(
                r.Data.ToString("dd/MM/yyyy HH:mm"),
                r.Usuario?.Login ?? "–",
                r.Tipo.ToString(),
                r.ValoresAntes ?? "",
                r.ValoresDepois ?? "");
        }
    }

    // ────────────────────────────────────────────
    // Export CSV
    // ────────────────────────────────────────────

    private void ExportarMatrizCsv(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Exportar Matriz de Módulos",
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"MatrizModulos_{DateTime.Today:yyyyMMdd}.csv"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            var sb = new StringBuilder();

            // Cabeçalho
            sb.Append("Módulo");
            foreach (var tipo in Enum.GetValues<TipoNegocio>())
                sb.Append($";{NomeCurtoTipo(tipo)}");
            sb.AppendLine();

            // Linhas
            foreach (DataGridViewRow row in gridMatriz.Rows)
            {
                for (int i = 0; i < row.Cells.Count; i++)
                {
                    if (i > 0) sb.Append(';');
                    sb.Append(row.Cells[i].Value?.ToString() ?? "");
                }
                sb.AppendLine();
            }

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            Toast.Mostrar($"Exportado: {Path.GetFileName(dlg.FileName)}", TipoToast.Sucesso, owner: this);
        }
        catch (Exception ex)
        {
            Toast.Mostrar($"Erro ao exportar: {ex.Message}", TipoToast.Erro, owner: this);
        }
    }

    // ────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────

    private static string NomeCurtoTipo(TipoNegocio tipo) => tipo switch
    {
        TipoNegocio.Padaria      => "🥐 Padaria",
        TipoNegocio.Acougue      => "🥩 Açougue",
        TipoNegocio.Loja         => "🛍️ Loja",
        TipoNegocio.Industria    => "🏭 Indústria",
        TipoNegocio.Bazar        => "🧺 Bazar",
        TipoNegocio.Supermercado => "🛒 Supermercado",
        TipoNegocio.Farmacia     => "💊 Farmácia",
        TipoNegocio.Restaurante  => "🍽️ Restaurante",
        _                        => tipo.ToString()
    };
}
