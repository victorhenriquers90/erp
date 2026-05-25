using System.Text;
using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Desktop.Forms;

[ModuloRequerido(ModuloSistema.Producao)]
public class FrmChecklistProducao : Form
{
    private readonly ChecklistProducaoService _svc;
    private ChecklistProducaoResumo? _resumo;
    private FlowLayoutPanel _indicadores = null!;
    private StyledGrid _grid = null!;
    private ComboBox _cboGrupo = null!;
    private ComboBox _cboStatus = null!;
    private Label _lblRodape = null!;

    public FrmChecklistProducao(ChecklistProducaoService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Checklist de Producao";
        Size = new Size(1180, 760);
        MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Checklist de Producao", "Prontidao operacional, fiscal e tecnica antes da primeira venda real", 76);

        _indicadores = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 142,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Tema.CorFundo,
            Padding = new Padding(0, 4, 0, 10)
        };

        var toolbar = MontarToolbar();
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(18) };

        _grid = new StyledGrid
        {
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCellsExceptHeaders,
            RowTemplate = { Height = 64 },
            AllowUserToResizeRows = true
        };
        _grid.Columns.Add("Status", "Status");
        _grid.Columns.Add("Grupo", "Grupo");
        _grid.Columns.Add("Titulo", "Item");
        _grid.Columns.Add("Descricao", "Situacao atual");
        _grid.Columns.Add("Acao", "Acao recomendada");
        _grid.Columns["Status"].FillWeight = 54;
        _grid.Columns["Grupo"].FillWeight = 92;
        _grid.Columns["Titulo"].FillWeight = 130;
        _grid.Columns["Descricao"].FillWeight = 220;
        _grid.Columns["Acao"].FillWeight = 220;
        _grid.Columns["Descricao"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _grid.Columns["Acao"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _grid.Columns["Titulo"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        _grid.Columns["Status"].DefaultCellStyle.Font = Tema.FontCorpoBold;

        _lblRodape = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            Font = Tema.FontPequena,
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 0, 0)
        };

        card.Controls.Add(_grid);
        card.Controls.Add(_lblRodape);

        Controls.Add(card);
        Controls.Add(toolbar);
        Controls.Add(_indicadores);
        Controls.Add(header);
    }

    private Control MontarToolbar()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Tema.CorFundo,
            Padding = new Padding(0, 8, 0, 8)
        };

        toolbar.Controls.Add(new Label
        {
            Text = "Grupo",
            Width = 48,
            Height = 36,
            Font = Tema.FontPequenaBold,
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleLeft
        });

        _cboGrupo = new ComboBox
        {
            Width = 210,
            Height = 34,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = Tema.FontCorpo,
            FlatStyle = FlatStyle.Flat
        };
        _cboGrupo.SelectedIndexChanged += (s, e) => PreencherGrid();
        toolbar.Controls.Add(_cboGrupo);

        toolbar.Controls.Add(new Label
        {
            Text = "Status",
            Width = 58,
            Height = 36,
            Margin = new Padding(18, 0, 0, 0),
            Font = Tema.FontPequenaBold,
            ForeColor = Tema.CorTextoMedio,
            TextAlign = ContentAlignment.MiddleLeft
        });

        _cboStatus = new ComboBox
        {
            Width = 150,
            Height = 34,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = Tema.FontCorpo,
            FlatStyle = FlatStyle.Flat
        };
        _cboStatus.Items.AddRange(new object[] { "Todos", "Pendente", "Atencao", "Pronto" });
        _cboStatus.SelectedIndex = 0;
        _cboStatus.SelectedIndexChanged += (s, e) => PreencherGrid();
        toolbar.Controls.Add(_cboStatus);

        var btnAtualizar = Botoes.PrimarioIcone("Atualizar", Tema.IconRefresh, 132, 36);
        btnAtualizar.Margin = new Padding(22, 0, 0, 0);
        btnAtualizar.Click += async (s, e) => await CarregarAsync();
        toolbar.Controls.Add(btnAtualizar);

        var btnConfig = Botoes.GhostIcone("Configuracoes", Tema.IconConfig, 170, 36);
        btnConfig.Click += (s, e) =>
        {
            ScopedFormHelper.AbrirModal<FrmConfigEmpresa>(this);
            _ = CarregarAsync();
        };
        toolbar.Controls.Add(btnConfig);

        var btnBackup = Botoes.GhostIcone("Backup", Tema.IconBackup, 120, 36);
        btnBackup.Click += (s, e) =>
        {
            ScopedFormHelper.AbrirModal<FrmBackup>(this);
            _ = CarregarAsync();
        };
        toolbar.Controls.Add(btnBackup);

        var btnExportar = Botoes.Secundario("Exportar TXT", 132, 36);
        btnExportar.Click += (s, e) => ExportarTxt();
        toolbar.Controls.Add(btnExportar);

        return toolbar;
    }

    private async Task CarregarAsync()
    {
        UseWaitCursor = true;
        try
        {
            _resumo = await _svc.AvaliarAsync();
            AtualizarIndicadores();
            AtualizarFiltros();
            PreencherGrid();
        }
        catch (Exception ex)
        {
            Toast.Mostrar("Falha ao avaliar checklist: " + ex.Message, TipoToast.Erro, owner: this);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void AtualizarIndicadores()
    {
        if (_resumo == null) return;

        _indicadores.Controls.Clear();
        var corProgresso = _resumo.Pendentes > 0
            ? Tema.CorAlerta
            : _resumo.Atencoes > 0 ? Tema.CorInfo : Tema.CorSucesso;

        _indicadores.Controls.Add(new KpiCard("Prontidao", _resumo.PercentualPronto + "%", Tema.IconChecklist, corProgresso));
        _indicadores.Controls.Add(new KpiCard("Prontos", _resumo.Prontos.ToString(), Tema.IconSucesso, Tema.CorSucesso));
        _indicadores.Controls.Add(new KpiCard("Pendentes", _resumo.Pendentes.ToString(), Tema.IconErro, _resumo.Pendentes > 0 ? Tema.CorErro : Tema.CorNeutro));
        _indicadores.Controls.Add(new KpiCard("Atencao", _resumo.Atencoes.ToString(), Tema.IconAlerta, _resumo.Atencoes > 0 ? Tema.CorAlerta : Tema.CorNeutro));
        _indicadores.Controls.Add(new KpiCard("Status geral", _resumo.PodeProduzir ? "Liberavel" : "Bloqueado", Tema.IconInfo, _resumo.PodeProduzir ? Tema.CorSucesso : Tema.CorErro));
    }

    private void AtualizarFiltros()
    {
        if (_resumo == null) return;

        var selecionado = _cboGrupo.SelectedItem as string;
        _cboGrupo.Items.Clear();
        _cboGrupo.Items.Add("Todos");
        foreach (var grupo in _resumo.Itens.Select(i => i.Grupo).Distinct().OrderBy(g => g))
            _cboGrupo.Items.Add(grupo);

        var idx = !string.IsNullOrWhiteSpace(selecionado) && _cboGrupo.Items.Contains(selecionado)
            ? _cboGrupo.Items.IndexOf(selecionado)
            : 0;
        _cboGrupo.SelectedIndex = idx;
    }

    private void PreencherGrid()
    {
        if (_resumo == null || _grid.Columns.Count == 0) return;

        var itens = _resumo.Itens.AsEnumerable();
        var grupo = _cboGrupo.SelectedItem as string;
        var status = _cboStatus.SelectedItem as string;

        if (!string.IsNullOrWhiteSpace(grupo) && grupo != "Todos")
            itens = itens.Where(i => i.Grupo == grupo);

        if (!string.IsNullOrWhiteSpace(status) && status != "Todos")
            itens = itens.Where(i => TextoStatus(i.Status) == status);

        var lista = itens.OrderBy(i => i.Ordem).ToList();
        _grid.Rows.Clear();

        foreach (var item in lista)
        {
            var rowIndex = _grid.Rows.Add(
                TextoStatus(item.Status),
                item.Grupo,
                item.Titulo,
                item.Descricao,
                item.AcaoRecomendada);

            var row = _grid.Rows[rowIndex];
            row.Tag = item;
            row.Height = 64;
            AplicarEstiloLinha(row, item.Status);
            foreach (DataGridViewCell cell in row.Cells)
                cell.ToolTipText = cell.Value?.ToString() ?? "";
        }

        _lblRodape.Text = $"{lista.Count} item(ns) exibido(s) | Gerado em {_resumo.GeradoEm:dd/MM/yyyy HH:mm}";
    }

    private static string TextoStatus(StatusChecklistProducao status) => status switch
    {
        StatusChecklistProducao.Pronto => "Pronto",
        StatusChecklistProducao.Atencao => "Atencao",
        _ => "Pendente"
    };

    private static void AplicarEstiloLinha(DataGridViewRow row, StatusChecklistProducao status)
    {
        var cor = status switch
        {
            StatusChecklistProducao.Pronto => Tema.CorSucesso,
            StatusChecklistProducao.Atencao => Tema.CorAlerta,
            _ => Tema.CorErro
        };

        row.Cells["Status"].Style.ForeColor = cor;
        row.Cells["Status"].Style.SelectionForeColor = cor;
        row.Cells["Status"].Style.Font = Tema.FontCorpoBold;
    }

    private void ExportarTxt()
    {
        if (_resumo == null)
        {
            Toast.Mostrar("Checklist ainda nao carregado.", TipoToast.Aviso, owner: this);
            return;
        }

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Checklist de Producao - ProjetoVarejo ERP");
            sb.AppendLine($"Gerado em: {_resumo.GeradoEm:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Prontidao: {_resumo.PercentualPronto}% | Prontos: {_resumo.Prontos} | Pendentes: {_resumo.Pendentes} | Atencao: {_resumo.Atencoes}");
            sb.AppendLine();

            foreach (var item in _resumo.Itens.OrderBy(i => i.Grupo).ThenBy(i => i.Ordem))
            {
                sb.AppendLine($"[{TextoStatus(item.Status)}] {item.Grupo} - {item.Titulo}");
                sb.AppendLine($"Situacao: {item.Descricao}");
                sb.AppendLine($"Acao: {item.AcaoRecomendada}");
                sb.AppendLine();
            }

            var arquivo = Path.Combine(AppContext.BaseDirectory, $"checklist_producao_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(arquivo, sb.ToString(), Encoding.UTF8);
            Toast.Mostrar("Checklist exportado.", TipoToast.Sucesso, owner: this);
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{arquivo}\"");
        }
        catch (Exception ex)
        {
            Toast.Mostrar("Falha ao exportar: " + ex.Message, TipoToast.Erro, owner: this);
        }
    }
}
