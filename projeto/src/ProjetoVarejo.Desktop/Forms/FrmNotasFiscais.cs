using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmNotasFiscais : Form
{
    private readonly NfceService _svc;
    private readonly VendaService _vendas;
    private DateTimePicker dtDe = null!, dtAte = null!;
    private ComboBox cboStatus = null!;
    private StyledGrid grid = null!;
    private Label lblTotal = null!;

    public FrmNotasFiscais(NfceService svc, VendaService vendas)
    {
        _svc = svc; _vendas = vendas;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Notas Fiscais";
        Size = new Size(1250, 720);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        // Header
        var header = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Tema.CorFundo };
        var lblTitulo = new Label
        {
            Text = "Notas Fiscais Eletrônicas",
            Dock = DockStyle.Top, Height = 40,
            Font = Tema.FontTituloGigante, ForeColor = Tema.CorTextoEscuro
        };
        lblTotal = new Label
        {
            Text = "NFC-e emitidas, autorizadas e canceladas",
            Dock = DockStyle.Top, Height = 24,
            Font = Tema.FontCorpo, ForeColor = Tema.CorTextoMedio
        };
        header.Controls.Add(lblTotal);
        header.Controls.Add(lblTitulo);

        // Filtros
        var filtros = new Card { Dock = DockStyle.Top, Height = 80, Padding = new Padding(16) };
        var pnlFiltros = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        AddRotulo(pnlFiltros, "DE", 0, 0);
        dtDe = new DateTimePicker { Left = 0, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30), Font = Tema.FontCorpo };
        pnlFiltros.Controls.Add(dtDe);

        AddRotulo(pnlFiltros, "ATÉ", 145, 0);
        dtAte = new DateTimePicker { Left = 145, Top = 18, Width = 130, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1), Font = Tema.FontCorpo };
        pnlFiltros.Controls.Add(dtAte);

        AddRotulo(pnlFiltros, "STATUS", 290, 0);
        cboStatus = new ComboBox { Left = 290, Top = 18, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = Tema.FontCorpo, FlatStyle = FlatStyle.Flat };
        cboStatus.Items.Add("Todos");
        foreach (StatusNotaFiscal s in Enum.GetValues(typeof(StatusNotaFiscal))) cboStatus.Items.Add(s);
        cboStatus.SelectedIndex = 0;
        pnlFiltros.Controls.Add(cboStatus);

        var btnFiltrar = Botoes.Primario("Filtrar", 110, 32);
        btnFiltrar.Top = 18; btnFiltrar.Left = 490;
        btnFiltrar.Click += async (s, e) => await CarregarAsync();
        pnlFiltros.Controls.Add(btnFiltrar);

        // Botões à direita
        var btnCancelar = Botoes.Perigo("Cancelar nota", 140, 32);
        btnCancelar.Top = 18;
        btnCancelar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnCancelar.Click += async (s, e) => await CancelarSelAsync();
        var btnInutilizar = Botoes.Aviso("Inutilizar faixa", 140, 32);
        btnInutilizar.Top = 18;
        btnInutilizar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnInutilizar.Click += async (s, e) => await InutilizarAsync();
        var btnReenviar = Botoes.Info("Reenviar conting.", 150, 32);
        btnReenviar.Top = 18;
        btnReenviar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnReenviar.Click += async (s, e) => await ReenviarContingenciaAsync();
        pnlFiltros.Controls.Add(btnCancelar);
        pnlFiltros.Controls.Add(btnInutilizar);
        pnlFiltros.Controls.Add(btnReenviar);

        // Posicionar os botões da direita
        pnlFiltros.Resize += (s, e) =>
        {
            btnReenviar.Left = pnlFiltros.Width - 160;
            btnInutilizar.Left = pnlFiltros.Width - 310;
            btnCancelar.Left = pnlFiltros.Width - 460;
        };

        filtros.Controls.Add(pnlFiltros);

        // Grid card
        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Id", "Id");
        grid.Columns.Add("Numero", "Número");
        grid.Columns.Add("Serie", "Série");
        grid.Columns.Add("Data", "Data");
        grid.Columns.Add("Chave", "Chave de acesso");
        grid.Columns.Add("Protocolo", "Protocolo");
        var colStatus = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status" };
        grid.Columns.Add(colStatus);
        grid.Columns.Add("Mensagem", "Mensagem SEFAZ");
        grid.Columns["Id"]!.Visible = false;
        grid.Columns["Chave"]!.FillWeight = 240;
        grid.Columns["Mensagem"]!.FillWeight = 200;
        cardGrid.Controls.Add(grid);

        Controls.Add(cardGrid);
        Controls.Add(filtros);
        Controls.Add(header);
    }

    private static void AddRotulo(Control parent, string txt, int left, int top)
    {
        parent.Controls.Add(new Label
        {
            Text = txt,
            Left = left, Top = top, Width = 100, Height = 16,
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        });
    }

    private async Task CarregarAsync()
    {
        StatusNotaFiscal? status = cboStatus.SelectedItem is StatusNotaFiscal sn ? sn : null;
        var lista = await _svc.ListarAsync(dtDe.Value.Date, dtAte.Value.Date.AddDays(1), status);
        grid.Rows.Clear();
        foreach (var n in lista)
        {
            int idx = grid.Rows.Add(n.Id, n.Numero, n.Serie,
                n.AutorizadaEm?.ToString("dd/MM/yyyy HH:mm") ?? n.CriadoEm.ToString("dd/MM/yyyy HH:mm"),
                n.ChaveAcesso, n.Protocolo, n.Status.ToString(), n.MensagemSefaz);

            var cell = grid.Rows[idx].Cells["Status"];
            cell.Style.Font = Tema.FontCorpoBold;
            cell.Style.ForeColor = n.Status switch
            {
                StatusNotaFiscal.Autorizada => Tema.CorSucesso,
                StatusNotaFiscal.Rejeitada => Tema.CorErro,
                StatusNotaFiscal.Cancelada => Tema.CorNeutro,
                StatusNotaFiscal.Contingencia => Tema.CorAlerta,
                _ => Tema.CorTextoMedio
            };
        }
        lblTotal.Text = $"{lista.Count} nota(s) listada(s) no período";
    }

    private async Task CancelarSelAsync()
    {
        if (grid.SelectedRows.Count == 0)
        {
            Toast.Mostrar("Selecione uma nota.", TipoToast.Info, owner: this);
            return;
        }
        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;

        using var dlg = new FrmJustificativa("Justificativa de Cancelamento",
            "Informe o motivo (mín. 15, máx. 255 caracteres):");
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        UseWaitCursor = true;
        try
        {
            var res = await _svc.CancelarAsync(id, dlg.Justificativa);
            if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Falha", TipoToast.Erro, owner: this); return; }
            Toast.Mostrar("Nota cancelada com sucesso!", TipoToast.Sucesso, owner: this);
            await CarregarAsync();
        }
        finally { UseWaitCursor = false; }
    }

    private async Task ReenviarContingenciaAsync()
    {
        UseWaitCursor = true;
        try
        {
            var res = await _svc.ReenviarContingenciaAsync();
            if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Falha", TipoToast.Erro, owner: this); return; }
            Toast.Mostrar($"{res.Valor} nota(s) autorizada(s).", TipoToast.Sucesso, owner: this);
            await CarregarAsync();
        }
        finally { UseWaitCursor = false; }
    }

    private async Task InutilizarAsync()
    {
        using var dlg = new FrmInutilizacao();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        UseWaitCursor = true;
        try
        {
            var res = await _svc.InutilizarFaixaAsync(dlg.Serie, dlg.NumeroInicial, dlg.NumeroFinal, dlg.Justificativa);
            if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Falha", TipoToast.Erro, owner: this); return; }
            Toast.Mostrar(res.Valor ?? "Sucesso", TipoToast.Sucesso, owner: this);
            await CarregarAsync();
        }
        finally { UseWaitCursor = false; }
    }
}

public class FrmJustificativa : Form
{
    public string Justificativa { get; private set; } = "";
    private TextBox txtJust = null!;
    private Label lblContador = null!;

    public FrmJustificativa(string titulo, string instrucao)
    {
        Text = titulo;
        Size = new Size(580, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Tema.CorFundo };
        header.Controls.Add(new Label
        {
            Text = titulo, Dock = DockStyle.Fill,
            Font = Tema.FontTituloGrande, ForeColor = Tema.CorTextoEscuro,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(20) };

        var lbl = new Label { Text = instrucao, Dock = DockStyle.Top, Height = 24, Font = Tema.FontCorpo, ForeColor = Tema.CorTextoMedio };
        txtJust = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true,
            Font = Tema.FontCorpo, MaxLength = 255, AcceptsReturn = true,
            BorderStyle = BorderStyle.FixedSingle
        };
        txtJust.TextChanged += (s, e) => AtualizarContador();
        lblContador = new Label
        {
            Dock = DockStyle.Bottom, Height = 22,
            Font = new Font(Tema.FontFamily, 9, FontStyle.Italic),
            ForeColor = Tema.CorTextoMedio
        };
        card.Controls.Add(txtJust);
        card.Controls.Add(lblContador);
        card.Controls.Add(lbl);

        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Tema.CorFundo, Padding = new Padding(0, 15, 0, 0) };
        var btnOk = Botoes.Sucesso("Confirmar", 150, 40);
        btnOk.Dock = DockStyle.Right;
        btnOk.Click += (s, e) =>
        {
            if (txtJust.Text.Trim().Length < 15) { Toast.Mostrar("Mínimo 15 caracteres.", TipoToast.Aviso, owner: this); return; }
            Justificativa = txtJust.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        };
        var btnCanc = Botoes.Ghost("Cancelar", 130, 40);
        btnCanc.Dock = DockStyle.Right;
        btnCanc.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        var spacer = new Panel { Dock = DockStyle.Right, Width = 10 };
        rodape.Controls.Add(btnOk);
        rodape.Controls.Add(spacer);
        rodape.Controls.Add(btnCanc);
        CancelButton = btnCanc;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);

        AtualizarContador();
    }

    private void AtualizarContador()
    {
        var len = txtJust.Text.Trim().Length;
        lblContador.Text = $"{len} / 255 caracteres (mínimo 15)";
        lblContador.ForeColor = len < 15 ? Tema.CorErro : Tema.CorSucesso;
    }
}

public class FrmInutilizacao : Form
{
    public int Serie { get; private set; }
    public int NumeroInicial { get; private set; }
    public int NumeroFinal { get; private set; }
    public string Justificativa { get; private set; } = "";

    private TextBox txtSerie = null!, txtIni = null!, txtFim = null!, txtJust = null!;

    public FrmInutilizacao()
    {
        Text = "Inutilização de Numeração";
        Size = new Size(580, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = new Panel { Dock = DockStyle.Top, Height = 60 };
        header.Controls.Add(new Label
        {
            Text = "Inutilização de Faixa", Dock = DockStyle.Fill,
            Font = Tema.FontTituloGrande, ForeColor = Tema.CorTextoEscuro,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(20) };

        var aviso = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Tema.CorAlertaSoft,
            Padding = new Padding(15, 10, 15, 10),
            Margin = new Padding(0, 0, 0, 12)
        };
        aviso.Controls.Add(new Label
        {
            Text = "⚠  Use apenas para números NUNCA emitidos.",
            Dock = DockStyle.Top, Height = 20,
            Font = Tema.FontCorpoBold, ForeColor = Tema.CorAlerta,
            BackColor = Color.Transparent
        });
        aviso.Controls.Add(new Label
        {
            Text = "Quebras de sequência por erro de sistema. Após inutilização os números nunca mais podem ser usados.",
            Dock = DockStyle.Top, Height = 20,
            Font = Tema.FontPequena, ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        });

        var form = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        AddLabel(form, "Série", 0, 0);
        txtSerie = AddInput(form, 0, 20, 100, "1");

        AddLabel(form, "Nº inicial", 120, 0);
        txtIni = AddInput(form, 120, 20, 150);

        AddLabel(form, "Nº final", 290, 0);
        txtFim = AddInput(form, 290, 20, 150);

        AddLabel(form, "Justificativa", 0, 60);
        txtJust = new TextBox
        {
            Left = 0, Top = 80, Width = 530, Height = 80, Multiline = true,
            Font = Tema.FontCorpo, MaxLength = 255,
            BorderStyle = BorderStyle.FixedSingle
        };
        form.Controls.Add(txtJust);

        card.Controls.Add(form);
        card.Controls.Add(aviso);

        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(0, 15, 0, 0) };
        var btnOk = Botoes.Aviso("Inutilizar", 150, 40);
        btnOk.Dock = DockStyle.Right;
        btnOk.Click += (s, e) =>
        {
            if (!int.TryParse(txtSerie.Text, out var sr) || sr < 0) { Toast.Mostrar("Série inválida.", TipoToast.Erro, owner: this); return; }
            if (!int.TryParse(txtIni.Text, out var ini) || ini <= 0) { Toast.Mostrar("Número inicial inválido.", TipoToast.Erro, owner: this); return; }
            if (!int.TryParse(txtFim.Text, out var fim) || fim < ini) { Toast.Mostrar("Número final inválido.", TipoToast.Erro, owner: this); return; }
            if (txtJust.Text.Trim().Length < 15) { Toast.Mostrar("Justificativa mínima 15 caracteres.", TipoToast.Erro, owner: this); return; }
            Serie = sr; NumeroInicial = ini; NumeroFinal = fim; Justificativa = txtJust.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        };
        var btnCanc = Botoes.Ghost("Cancelar", 130, 40);
        btnCanc.Dock = DockStyle.Right;
        btnCanc.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        var spacer = new Panel { Dock = DockStyle.Right, Width = 10 };
        rodape.Controls.Add(btnOk);
        rodape.Controls.Add(spacer);
        rodape.Controls.Add(btnCanc);
        CancelButton = btnCanc;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);
    }

    private static void AddLabel(Control parent, string txt, int left, int top)
    {
        parent.Controls.Add(new Label
        {
            Text = txt.ToUpper(),
            Left = left, Top = top, Width = 120, Height = 16,
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        });
    }

    private static TextBox AddInput(Control parent, int left, int top, int width, string defaultValue = "")
    {
        var tb = new TextBox
        {
            Left = left, Top = top, Width = width, Height = 28,
            Font = new Font(Tema.FontFamily, 10),
            BorderStyle = BorderStyle.FixedSingle,
            Text = defaultValue
        };
        parent.Controls.Add(tb);
        return tb;
    }
}
