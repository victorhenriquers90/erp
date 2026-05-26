using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Contracts.Services.DTOs;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmCaixa : Form
{
    private readonly ICaixaService _svc;
    private Label _statusIcone = null!, _statusTexto = null!, _statusDetalhe = null!;
    private Card _statusCard = null!;
    private FlowLayoutPanel _kpis = null!;
    private Card _detalhesCard = null!;
    private Label _detalhes = null!;
    private Button _btnAbrir = null!, _btnFechar = null!, _btnSangria = null!, _btnSuprimento = null!;
    private CaixaSessao? _caixa;

    public FrmCaixa(ICaixaService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await AtualizarAsync();
    }

    private void InitUi()
    {
        Text = "Caixa";
        Size = new Size(960, 660);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Gestão de Caixa", "Abertura, sangrias, suprimentos e fechamento");

        // === Card de status (grande) ===
        _statusCard = new Card { Dock = DockStyle.Top, Height = 120, Padding = new Padding(24) };
        var pnlStatus = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        _statusIcone = new Label
        {
            Dock = DockStyle.Left, Width = 70,
            Font = new Font("Segoe MDL2 Assets", 36),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        var pnlTexto = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        _statusTexto = new Label
        {
            Dock = DockStyle.Top, Height = 36,
            Font = new Font(Tema.FontFamily, 20, FontStyle.Bold),
            ForeColor = Tema.CorTextoEscuro
        };
        _statusDetalhe = new Label
        {
            Dock = DockStyle.Top, Height = 24,
            Font = Tema.FontCorpo,
            ForeColor = Tema.CorTextoMedio
        };
        pnlTexto.Controls.Add(_statusDetalhe);
        pnlTexto.Controls.Add(_statusTexto);
        pnlStatus.Controls.Add(pnlTexto);
        pnlStatus.Controls.Add(_statusIcone);
        _statusCard.Controls.Add(pnlStatus);

        // === KPIs (mini cards) ===
        _kpis = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 170,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Tema.CorFundo,
            Padding = new Padding(0, 12, 0, 0)
        };

        // === Detalhes (Card com listagem por forma de pagamento) ===
        _detalhesCard = new Card { Dock = DockStyle.Fill, Padding = new Padding(20) };
        var lblDetTitulo = new Label
        {
            Text = "Vendas por forma de pagamento",
            Dock = DockStyle.Top, Height = 28,
            Font = Tema.FontSubtitulo, ForeColor = Tema.CorTextoEscuro
        };
        _detalhes = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamilyMono, 10),
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.TopLeft
        };
        _detalhesCard.Controls.Add(_detalhes);
        _detalhesCard.Controls.Add(lblDetTitulo);

        // === Botões de ação (bottom) ===
        var pnlBotoes = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Tema.CorFundo, Padding = new Padding(0, 15, 0, 0) };
        _btnAbrir = Botoes.Sucesso("ABRIR CAIXA", 220, 50);
        _btnAbrir.Click += async (s, e) => await AbrirAsync();
        _btnSuprimento = Botoes.Info("Suprimento", 150, 50);
        _btnSuprimento.Click += async (s, e) => await SuprimentoAsync();
        _btnSangria = Botoes.Aviso("Sangria", 150, 50);
        _btnSangria.Click += async (s, e) => await SangriaAsync();
        _btnFechar = Botoes.Perigo("  FECHAR CAIXA", 220, 50);
        _btnFechar.Click += async (s, e) => await FecharAsync();

        var fl = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, BackColor = Tema.CorFundo };
        fl.Controls.Add(_btnAbrir);
        fl.Controls.Add(_btnSuprimento);
        fl.Controls.Add(_btnSangria);
        fl.Controls.Add(_btnFechar);
        pnlBotoes.Controls.Add(fl);

        Controls.Add(_detalhesCard);
        Controls.Add(pnlBotoes);
        Controls.Add(_kpis);
        Controls.Add(_statusCard);
        Controls.Add(header);
    }

    private async Task AtualizarAsync()
    {
        _caixa = await _svc.ObterCaixaAbertoAsync();
        if (_caixa == null)
        {
            _statusIcone.Text = "";  // X / closed
            _statusIcone.ForeColor = Tema.CorErro;
            _statusTexto.Text = "Caixa Fechado";
            _statusTexto.ForeColor = Tema.CorErro;
            _statusDetalhe.Text = "Nenhum caixa aberto para este usuário. Clique em ABRIR CAIXA para iniciar vendas.";
            _btnAbrir.Enabled = true;
            _btnFechar.Enabled = _btnSangria.Enabled = _btnSuprimento.Enabled = false;
            _kpis.Controls.Clear();
            _detalhes.Text = "";
            return;
        }

        _statusIcone.Text = "";  // check / open
        _statusIcone.ForeColor = Tema.CorSucesso;
        _statusTexto.Text = "Caixa Aberto";
        _statusTexto.ForeColor = Tema.CorSucesso;
        _statusDetalhe.Text = $"Aberto em {_caixa.AbertaEm:dd/MM/yyyy} às {_caixa.AbertaEm:HH:mm} • Valor de abertura: {_caixa.ValorAbertura:C}";
        _btnAbrir.Enabled = false;
        _btnFechar.Enabled = _btnSangria.Enabled = _btnSuprimento.Enabled = true;

        var r = await _svc.ResumoAsync(_caixa.Id);
        _kpis.Controls.Clear();
        _kpis.Controls.Add(new KpiCard("Abertura", r.ValorAbertura.ToString("C"), Tema.IconCaixa, Tema.CorInfo));
        _kpis.Controls.Add(new KpiCard("Suprimentos", r.TotalSuprimentos.ToString("C"), Tema.IconArrowUp, Tema.CorSucesso));
        _kpis.Controls.Add(new KpiCard("Sangrias", r.TotalSangrias.ToString("C"), Tema.IconArrowDown, Tema.CorAlerta));
        _kpis.Controls.Add(new KpiCard("Total vendas", r.TotalVendas.ToString("C"), Tema.IconVendas, Tema.CorPrimaria));
        _kpis.Controls.Add(new KpiCard("Esperado em dinheiro", r.SaldoDinheiroEsperado.ToString("C"), Tema.IconFinanceiro, Tema.CorPrimariaDark));

        var sb = new System.Text.StringBuilder();
        foreach (FormaPagamentoTipo f in Enum.GetValues(typeof(FormaPagamentoTipo)))
        {
            var v = r.VendasPorForma.TryGetValue(f, out var vv) ? vv : 0;
            if (v > 0)
                sb.AppendLine($"  {f,-22}  {v,12:C}");
        }
        if (sb.Length == 0) sb.AppendLine("  Nenhuma venda registrada ainda.");
        _detalhes.Text = sb.ToString();
    }

    private async Task AbrirAsync()
    {
        var s = Microsoft.VisualBasic.Interaction.InputBox(
            "Valor de abertura (R$):", "Abrir Caixa", "0,00");
        if (string.IsNullOrWhiteSpace(s)) return;
        if (!decimal.TryParse(s.Replace('.', ','), out var v) || v < 0) { Toast.Mostrar("Valor inválido.", TipoToast.Erro, owner: this); return; }
        var res = await _svc.AbrirAsync(v);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        Toast.Mostrar("Caixa aberto!", TipoToast.Sucesso, owner: this);
        await AtualizarAsync();
    }

    private async Task SuprimentoAsync()
    {
        using var dlg = new FrmMovimentoCaixa("Suprimento — reforço de troco", Tema.CorInfo);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var res = await _svc.SuprimentoAsync(dlg.Valor, dlg.Motivo);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        Toast.Mostrar("Suprimento registrado.", TipoToast.Sucesso, owner: this);
        await AtualizarAsync();
    }

    private async Task SangriaAsync()
    {
        using var dlg = new FrmMovimentoCaixa("Sangria — retirada de dinheiro", Tema.CorAlerta);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var res = await _svc.SangriaAsync(dlg.Valor, dlg.Motivo);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        Toast.Mostrar("Sangria registrada.", TipoToast.Sucesso, owner: this);
        await AtualizarAsync();
    }

    private async Task FecharAsync()
    {
        if (_caixa == null) return;
        var resumo = await _svc.ResumoAsync(_caixa.Id);
        using var dlg = new FrmFechamentoCaixa(resumo);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        var res = await _svc.FecharAsync(_caixa.Id, dlg.ValorInformado, dlg.Observacao);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        var diff = res.Valor!.Diferenca;
        var msg = diff == 0 ? "Caixa fechado — sem divergência."
                : diff > 0 ? $"Caixa fechado com SOBRA de {diff:C}."
                : $"Caixa fechado com FALTA de {Math.Abs(diff):C}.";
        Toast.Mostrar(msg, diff == 0 ? TipoToast.Sucesso : TipoToast.Aviso, owner: this);
        await AtualizarAsync();
    }
}

public class FrmMovimentoCaixa : Form
{
    public decimal Valor { get; private set; }
    public string Motivo { get; private set; } = "";

    public FrmMovimentoCaixa(string titulo, Color cor)
    {
        Text = titulo;
        Size = new Size(520, 360);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderModal(titulo);
        header.Controls.OfType<Label>().First().ForeColor = cor;

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        var txtValor = Inputs.CampoTexto(pnl, "Valor (R$)*", 0, 0, 200, right: true);
        txtValor.Font = new Font(Tema.FontFamily, 14, FontStyle.Bold);
        txtValor.Height = 40;
        var txtMotivo = Inputs.CampoTexto(pnl, "Motivo*", 0, 70, 460);
        txtMotivo.Multiline = true; txtMotivo.Height = 80;

        card.Controls.Add(pnl);

        var (rodape, btnOk, btnCanc) = Inputs.RodapeSalvarCancelar("Confirmar");
        btnOk.BackColor = cor;
        btnOk.FlatAppearance.MouseOverBackColor = Botoes.Misturar(cor, Color.Black, 0.10f);
        btnOk.Click += (s, e) =>
        {
            if (!decimal.TryParse(txtValor.Text.Replace('.', ','), out var v) || v <= 0)
            { Toast.Mostrar("Valor inválido.", TipoToast.Erro, owner: this); return; }
            if (string.IsNullOrWhiteSpace(txtMotivo.Text))
            { Toast.Mostrar("Informe o motivo.", TipoToast.Erro, owner: this); return; }
            Valor = v; Motivo = txtMotivo.Text.Trim();
            DialogResult = DialogResult.OK; Close();
        };
        btnCanc.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        AcceptButton = btnOk;
        CancelButton = btnCanc;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);
    }
}

public class FrmFechamentoCaixa : Form
{
    public decimal ValorInformado { get; private set; }
    public string? Observacao { get; private set; }

    public FrmFechamentoCaixa(ResumoCaixa resumo)
    {
        Text = "Fechamento de Caixa";
        Size = new Size(620, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderModal("Fechamento de Caixa");

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };

        var pnlResumo = new Panel
        {
            Dock = DockStyle.Top, Height = 200,
            BackColor = Tema.CorFundo,
            Padding = new Padding(20)
        };
        pnlResumo.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var path = Tema.PathArredondado(new Rectangle(0, 0, pnlResumo.Width - 1, pnlResumo.Height - 1), 6);
            using var brush = new SolidBrush(Tema.CorFundo);
            g.FillPath(brush, path);
        };

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"  Abertura ............... {resumo.ValorAbertura,12:C}");
        sb.AppendLine($"  Suprimentos ........... {resumo.TotalSuprimentos,12:C}");
        sb.AppendLine($"  Sangrias .............. {resumo.TotalSangrias,12:C}");
        decimal vendasDinheiro = resumo.VendasPorForma.TryGetValue(FormaPagamentoTipo.Dinheiro, out var d) ? d : 0;
        sb.AppendLine($"  Vendas em dinheiro ... {vendasDinheiro,12:C}");
        sb.AppendLine($"  Total vendas (todas) . {resumo.TotalVendas,12:C}");
        sb.AppendLine();
        sb.AppendLine($"  ESPERADO EM CAIXA .... {resumo.SaldoDinheiroEsperado,12:C}");

        var lblResumo = new Label
        {
            Text = sb.ToString(),
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamilyMono, 10),
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent
        };
        pnlResumo.Controls.Add(lblResumo);

        var pnlForm = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard, Padding = new Padding(0, 20, 0, 0) };
        var txtValor = Inputs.CampoTexto(pnlForm, "Valor contado em dinheiro*", 0, 0, 250, right: true);
        txtValor.Font = new Font(Tema.FontFamily, 14, FontStyle.Bold);
        txtValor.Height = 40;
        txtValor.Text = resumo.SaldoDinheiroEsperado.ToString("N2");
        var txtObs = Inputs.CampoTexto(pnlForm, "Observação", 0, 80, 540);
        txtObs.Multiline = true; txtObs.Height = 80;

        card.Controls.Add(pnlForm);
        card.Controls.Add(pnlResumo);

        var (rodape, btnOk, btnCanc) = Inputs.RodapeSalvarCancelar("Fechar Caixa");
        btnOk.BackColor = Tema.CorErro;
        btnOk.FlatAppearance.MouseOverBackColor = Botoes.Misturar(Tema.CorErro, Color.Black, 0.10f);
        btnOk.Click += (s, e) =>
        {
            if (!decimal.TryParse(txtValor.Text.Replace('.', ','), out var v) || v < 0)
            { Toast.Mostrar("Valor inválido.", TipoToast.Erro, owner: this); return; }
            ValorInformado = v;
            Observacao = string.IsNullOrWhiteSpace(txtObs.Text) ? null : txtObs.Text.Trim();
            DialogResult = DialogResult.OK; Close();
        };
        btnCanc.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        CancelButton = btnCanc;

        Controls.Add(card);
        Controls.Add(rodape);
        Controls.Add(header);
    }
}
