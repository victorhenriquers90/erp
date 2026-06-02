using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmConfigEmpresa : Form
{
    private readonly NfceService _svc;
    private EmpresaConfig _emp = null!;

    private TextBox txtRazao = null!, txtFantasia = null!, txtCnpj = null!, txtIe = null!, txtIm = null!;
    private TextBox txtCep = null!, txtLog = null!, txtNum = null!, txtCompl = null!, txtBairro = null!, txtCidade = null!, txtUf = null!, txtIbge = null!;
    private TextBox txtTel = null!, txtEmail = null!;
    private ComboBox cboRegime = null!;
    private TextBox txtCertCaminho = null!, txtCertSenha = null!, txtCscId = null!, txtCscToken = null!;
    private ComboBox cboAmbiente = null!;
    private TextBox txtProxNum = null!, txtSerie = null!;
    private ComboBox cboImpTipo = null!;
    private TextBox txtImpDestino = null!, txtImpPorta = null!, txtImpBaud = null!, txtImpColunas = null!;
    private CheckBox chkImprAuto = null!;
    private TextBox txtPixChave = null!, txtPixNome = null!, txtPixCidade = null!;

    private TabControl tabs = null!;

    public FrmConfigEmpresa(NfceService svc)
    {
        _svc = svc;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Configurações da Empresa";
        Size = new Size(1100, 760);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Configurações", "Dados da empresa, fiscal, impressora e PIX");

        tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            DrawMode = TabDrawMode.OwnerDrawFixed,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(180, 40),
            Appearance = TabAppearance.Normal
        };
        EstilizarTabs(tabs);

        tabs.TabPages.Add(MontarTabEmpresa());
        tabs.TabPages.Add(MontarTabEndereco());
        tabs.TabPages.Add(MontarTabNfce());
        tabs.TabPages.Add(MontarTabImpressora());
        tabs.TabPages.Add(MontarTabPix());

        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Tema.CorFundo, Padding = new Padding(0, 15, 0, 0) };
        var btnSalvar = Botoes.Primario("Salvar configurações", 220, 40);
        btnSalvar.Dock = DockStyle.Right;
        btnSalvar.Click += async (s, e) => await SalvarAsync();
        var btnCancelar = Botoes.Ghost("Cancelar", 130, 40);
        btnCancelar.Dock = DockStyle.Right;
        btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        var spacer = new Panel { Dock = DockStyle.Right, Width = 10 };
        rodape.Controls.Add(btnSalvar);
        rodape.Controls.Add(spacer);
        rodape.Controls.Add(btnCancelar);

        Controls.Add(tabs);
        Controls.Add(rodape);
        Controls.Add(header);
    }

    private TabPage MontarTabEmpresa()
    {
        var tp = new TabPage("  Dados da empresa") { BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard, AutoScroll = true };

        int y = 0;
        txtRazao = Inputs.CampoTexto(pnl, "Razão Social*", 0, y, 700); y += 60;
        txtFantasia = Inputs.CampoTexto(pnl, "Nome Fantasia", 0, y, 700); y += 60;
        txtCnpj = Inputs.CampoTexto(pnl, "CNPJ*", 0, y, 220);
        txtIe = Inputs.CampoTexto(pnl, "Inscrição Estadual", 240, y, 220);
        txtIm = Inputs.CampoTexto(pnl, "Inscrição Municipal", 480, y, 220);
        y += 60;
        cboRegime = Inputs.CampoCombo(pnl, "Regime Tributário", 0, y, 380);
        cboRegime.Items.Add("1 — Simples Nacional");
        cboRegime.Items.Add("2 — Simples Nacional / excesso sublimite");
        cboRegime.Items.Add("3 — Regime Normal");
        cboRegime.Items.Add("4 — MEI");
        y += 60;
        txtTel = Inputs.CampoTexto(pnl, "Telefone", 0, y, 220);
        txtEmail = Inputs.CampoTexto(pnl, "Email", 240, y, 460);

        card.Controls.Add(pnl);
        tp.Controls.Add(card);
        return tp;
    }

    private TabPage MontarTabEndereco()
    {
        var tp = new TabPage("  Endereço") { BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;
        txtCep = Inputs.CampoTexto(pnl, "CEP*", 0, y, 140);
        txtLog = Inputs.CampoTexto(pnl, "Logradouro*", 160, y, 440);
        txtNum = Inputs.CampoTexto(pnl, "Número*", 620, y, 100);
        y += 60;
        txtCompl = Inputs.CampoTexto(pnl, "Complemento", 0, y, 360);
        txtBairro = Inputs.CampoTexto(pnl, "Bairro*", 380, y, 340);
        y += 60;
        txtCidade = Inputs.CampoTexto(pnl, "Cidade*", 0, y, 360);
        txtUf = Inputs.CampoTexto(pnl, "UF*", 380, y, 80);
        txtIbge = Inputs.CampoTexto(pnl, "Cód. IBGE Município*", 480, y, 240);

        card.Controls.Add(pnl);
        tp.Controls.Add(card);
        return tp;
    }

    private TabPage MontarTabNfce()
    {
        var tp = new TabPage("  NFC-e (Fiscal)") { BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        // Aviso
        var aviso = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Tema.CorAlertaSoft, Padding = new Padding(15, 10, 15, 10), Margin = new Padding(0, 0, 0, 12) };
        aviso.Controls.Add(new Label
        {
            Text = "⚠  Configure com cuidado. Senha do certificado e CSC nunca devem ser compartilhados.",
            Dock = DockStyle.Fill,
            Font = Tema.FontCorpoBold,
            ForeColor = Tema.CorAlerta,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        });

        var form = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard, Padding = new Padding(0, 16, 0, 0) };

        int y = 0;
        cboAmbiente = Inputs.CampoCombo(form, "Ambiente*", 0, y, 280);
        cboAmbiente.Items.Add("Homologação (testes)");
        cboAmbiente.Items.Add("Produção (real)");
        y += 60;
        txtCertCaminho = Inputs.CampoTexto(form, "Certificado A1 (.pfx)*", 0, y, 600);
        var btnPick = Botoes.Ghost("...", 40, 28);
        btnPick.Left = 610; btnPick.Top = y + 20;
        btnPick.Click += (s, ev) =>
        {
            using var ofd = new OpenFileDialog { Filter = "Certificado A1 (*.pfx;*.p12)|*.pfx;*.p12|Todos|*.*" };
            if (ofd.ShowDialog(this) == DialogResult.OK) txtCertCaminho.Text = ofd.FileName;
        };
        form.Controls.Add(btnPick);
        y += 60;
        txtCertSenha = Inputs.CampoTexto(form, "Senha do certificado*", 0, y, 300);
        txtCertSenha.UseSystemPasswordChar = true;
        y += 60;
        txtCscId = Inputs.CampoTexto(form, "CSC ID", 0, y, 120);
        txtCscToken = Inputs.CampoTexto(form, "CSC Token", 140, y, 500);
        y += 60;
        txtProxNum = Inputs.CampoTexto(form, "Próximo nNF", 0, y, 140);
        txtSerie = Inputs.CampoTexto(form, "Série", 160, y, 100);

        pnl.Controls.Add(form);
        pnl.Controls.Add(aviso);
        card.Controls.Add(pnl);
        tp.Controls.Add(card);
        return tp;
    }

    private TabPage MontarTabImpressora()
    {
        var tp = new TabPage("  Impressora") { BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;
        cboImpTipo = Inputs.CampoCombo(pnl, "Tipo de conexão", 0, y, 320);
        cboImpTipo.Items.Add("Spooler Windows (nome da impressora)");
        cboImpTipo.Items.Add("Rede TCP (IP)");
        cboImpTipo.Items.Add("Serial (COM)");
        cboImpTipo.Items.Add("Arquivo (debug)");
        y += 60;
        txtImpDestino = Inputs.CampoTexto(pnl, "Destino (IP / COM / nome impressora)", 0, y, 700); y += 60;
        txtImpPorta = Inputs.CampoTexto(pnl, "Porta TCP", 0, y, 120, right: true);
        txtImpBaud = Inputs.CampoTexto(pnl, "Baud Serial", 140, y, 120, right: true);
        txtImpColunas = Inputs.CampoTexto(pnl, "Colunas", 280, y, 100, right: true);
        y += 60;
        chkImprAuto = Inputs.CampoCheck(pnl, "Imprimir automaticamente após finalizar venda", 0, y, 450, true);
        y += 50;
        var btnTeste = Botoes.Aviso("Testar impressão", 220, 40);
        btnTeste.Top = y; btnTeste.Left = 0;
        btnTeste.Click += async (s, ev) => await TestarImpressaoAsync();
        pnl.Controls.Add(btnTeste);

        card.Controls.Add(pnl);
        tp.Controls.Add(card);
        return tp;
    }

    private TabPage MontarTabPix()
    {
        var tp = new TabPage("  PIX") { BackColor = Tema.CorFundo, Padding = new Padding(0, 12, 0, 0) };
        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(24) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        int y = 0;
        txtPixChave = Inputs.CampoTexto(pnl, "Chave PIX (CPF, CNPJ, email, telefone ou aleatória)", 0, y, 600); y += 60;
        txtPixNome = Inputs.CampoTexto(pnl, "Nome do recebedor (até 25 chars)", 0, y, 360); y += 60;
        txtPixCidade = Inputs.CampoTexto(pnl, "Cidade do recebedor (até 15 chars)", 0, y, 280);

        card.Controls.Add(pnl);
        tp.Controls.Add(card);
        return tp;
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
        _emp = await _svc.ObterEmpresaAsync() ?? new EmpresaConfig();
        txtRazao.Text = _emp.RazaoSocial;
        txtFantasia.Text = _emp.NomeFantasia;
        txtCnpj.Text = _emp.Cnpj;
        txtIe.Text = _emp.InscricaoEstadual;
        txtIm.Text = _emp.InscricaoMunicipal ?? "";
        txtCep.Text = _emp.Cep;
        txtLog.Text = _emp.Logradouro;
        txtNum.Text = _emp.Numero;
        txtCompl.Text = _emp.Complemento ?? "";
        txtBairro.Text = _emp.Bairro;
        txtCidade.Text = _emp.Cidade;
        txtUf.Text = _emp.Uf;
        txtIbge.Text = _emp.CodigoMunicipioIbge;
        txtTel.Text = _emp.Telefone;
        txtEmail.Text = _emp.Email;
        cboRegime.SelectedIndex = _emp.RegimeTributario switch { "1" => 0, "2" => 1, "3" => 2, "4" => 3, _ => 0 };
        cboAmbiente.SelectedIndex = _emp.AmbienteHomologacao ? 0 : 1;
        txtCertCaminho.Text = _emp.CertificadoCaminho;
        txtCertSenha.Text = _emp.CertificadoSenha;
        txtCscId.Text = _emp.CscId;
        txtCscToken.Text = _emp.CscToken;
        txtProxNum.Text = _emp.ProximoNumeroNfce.ToString();
        txtSerie.Text = _emp.SerieNfce.ToString();
        cboImpTipo.SelectedIndex = Math.Max(0, _emp.ImpressoraTipo - 1);
        txtImpDestino.Text = _emp.ImpressoraDestino;
        txtImpPorta.Text = _emp.ImpressoraPorta.ToString();
        txtImpBaud.Text = _emp.ImpressoraBaud.ToString();
        txtImpColunas.Text = _emp.ImpressoraColunas.ToString();
        chkImprAuto.Checked = _emp.ImprimirAutomatico;
        txtPixChave.Text = _emp.PixChave;
        txtPixNome.Text = _emp.PixNomeRecebedor;
        txtPixCidade.Text = _emp.PixCidade;
    }

    private async Task TestarImpressaoAsync()
    {
        try
        {
            var cfg = new ProjetoVarejo.Infrastructure.Printing.ImpressoraConfig
            {
                Tipo = (ProjetoVarejo.Infrastructure.Printing.TipoImpressora)(cboImpTipo.SelectedIndex + 1),
                Destino = txtImpDestino.Text.Trim(),
                Porta = int.TryParse(txtImpPorta.Text, out var p) ? p : 9100,
                Baud = int.TryParse(txtImpBaud.Text, out var bd) ? bd : 9600,
                Colunas = int.TryParse(txtImpColunas.Text, out var cl) ? cl : 48
            };
            var b = new ProjetoVarejo.Infrastructure.Printing.EscPosBuilder();
            b.Centro().Negrito(true).Linha("TESTE DE IMPRESSAO").Negrito(false);
            b.Linha("ProjetoVarejo").Pular();
            b.Esquerda().Linha($"Data: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            b.Linha("Funcionando OK!");
            b.Pular(3).Cortar();
            await ProjetoVarejo.Infrastructure.Printing.EscPosPrinter.ImprimirAsync(b.Build(), cfg);
            Toast.Mostrar("Teste enviado para impressora.", TipoToast.Sucesso, owner: this);
        }
        catch (Exception ex)
        {
            Toast.Mostrar("Falha: " + ex.Message, TipoToast.Erro, owner: this);
        }
    }

    private async Task SalvarAsync()
    {
        _emp.RazaoSocial = txtRazao.Text.Trim();
        _emp.NomeFantasia = txtFantasia.Text.Trim();
        _emp.Cnpj = txtCnpj.Text.Trim();
        _emp.InscricaoEstadual = txtIe.Text.Trim();
        _emp.InscricaoMunicipal = string.IsNullOrWhiteSpace(txtIm.Text) ? null : txtIm.Text.Trim();
        _emp.Cep = txtCep.Text.Trim();
        _emp.Logradouro = txtLog.Text.Trim();
        _emp.Numero = txtNum.Text.Trim();
        _emp.Complemento = string.IsNullOrWhiteSpace(txtCompl.Text) ? null : txtCompl.Text.Trim();
        _emp.Bairro = txtBairro.Text.Trim();
        _emp.Cidade = txtCidade.Text.Trim();
        _emp.Uf = txtUf.Text.Trim();
        _emp.CodigoMunicipioIbge = txtIbge.Text.Trim();
        _emp.Telefone = txtTel.Text.Trim();
        _emp.Email = txtEmail.Text.Trim();
        _emp.RegimeTributario = (cboRegime.SelectedIndex + 1).ToString();
        _emp.AmbienteHomologacao = cboAmbiente.SelectedIndex == 0;
        _emp.CertificadoCaminho = txtCertCaminho.Text.Trim();
        _emp.CertificadoSenha = txtCertSenha.Text;
        _emp.CscId = txtCscId.Text.Trim();
        _emp.CscToken = txtCscToken.Text.Trim();
        _emp.ProximoNumeroNfce = int.TryParse(txtProxNum.Text, out var pn) ? pn : 1;
        _emp.SerieNfce = int.TryParse(txtSerie.Text, out var sn) ? sn : 1;
        _emp.ImpressoraTipo = cboImpTipo.SelectedIndex + 1;
        _emp.ImpressoraDestino = txtImpDestino.Text.Trim();
        _emp.ImpressoraPorta = int.TryParse(txtImpPorta.Text, out var ip) ? ip : 9100;
        _emp.ImpressoraBaud = int.TryParse(txtImpBaud.Text, out var ib) ? ib : 9600;
        _emp.ImpressoraColunas = int.TryParse(txtImpColunas.Text, out var ic) ? ic : 48;
        _emp.ImprimirAutomatico = chkImprAuto.Checked;
        _emp.PixChave = txtPixChave.Text.Trim();
        _emp.PixNomeRecebedor = txtPixNome.Text.Trim();
        _emp.PixCidade = txtPixCidade.Text.Trim();

        var res = await _svc.SalvarEmpresaAsync(_emp);
        if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Erro", TipoToast.Erro, owner: this); return; }
        Toast.Mostrar("Configurações salvas.", TipoToast.Sucesso, owner: this);
        DialogResult = DialogResult.OK;
        Close();
    }
}
