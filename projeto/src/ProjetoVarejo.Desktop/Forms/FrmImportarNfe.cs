using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmImportarNfe : Form
{
    private readonly NfeImporterService _svc;
    private TextBox txtArquivo = null!;
    private StyledGrid grid = null!;
    private Card cardCabecalho = null!;
    private Label lblCabecalho = null!;
    private CheckBox chkConta = null!, chkCriarProd = null!;
    private Button btnImportar = null!;
    private NfeImporterService.PreviaImportacao? _previa;

    public FrmImportarNfe(NfeImporterService svc)
    {
        _svc = svc;
        InitUi();
    }

    private void InitUi()
    {
        Text = "Importar XML NF-e";
        Size = new Size(1200, 760);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Importar XML NF-e", "Entrada de mercadoria + criação de produtos + conta a pagar");

        var seletor = new Card { Dock = DockStyle.Top, Height = 80, Padding = new Padding(16) };
        var pnl = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        pnl.Controls.Add(Inputs.Rotulo("ARQUIVO XML", 0, 0));
        txtArquivo = new TextBox { Left = 0, Top = 18, Width = 800, Height = 32, Font = Tema.FontCorpo, BorderStyle = BorderStyle.FixedSingle };
        pnl.Controls.Add(txtArquivo);
        var btnPick = Botoes.Ghost("Selecionar...", 140, 32);
        btnPick.Top = 18; btnPick.Left = 810;
        btnPick.Click += (s, e) =>
        {
            using var ofd = new OpenFileDialog { Filter = "XML NFe (*.xml)|*.xml" };
            if (ofd.ShowDialog(this) == DialogResult.OK) txtArquivo.Text = ofd.FileName;
        };
        pnl.Controls.Add(btnPick);
        var btnAn = Botoes.Primario("Analisar", 130, 32);
        btnAn.Top = 18; btnAn.Left = 960;
        btnAn.Click += async (s, e) => await AnalisarAsync();
        pnl.Controls.Add(btnAn);
        seletor.Controls.Add(pnl);

        cardCabecalho = new Card { Dock = DockStyle.Top, Height = 100, Padding = new Padding(20) };
        lblCabecalho = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamilyMono, 10),
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.TopLeft
        };
        cardCabecalho.Controls.Add(lblCabecalho);

        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Codigo", "Cód. Forn.");
        grid.Columns.Add("Barras", "Cód. Barras");
        grid.Columns.Add("Descricao", "Descrição");
        grid.Columns.Add("Qtd", "Qtd");
        grid.Columns.Add("VlrUnit", "Vl. Unit");
        grid.Columns.Add("VlrTotal", "Vl. Total");
        grid.Columns.Add("Status", "Status");
        grid.Columns["Descricao"]!.FillWeight = 300;
        grid.Columns["Qtd"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["VlrUnit"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.Columns["VlrTotal"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        cardGrid.Controls.Add(grid);

        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 90, BackColor = Tema.CorFundo, Padding = new Padding(0, 15, 0, 15) };
        chkConta = Inputs.CampoCheck(rodape, "Criar conta a pagar para o fornecedor", 0, 5, 380, true);
        chkCriarProd = Inputs.CampoCheck(rodape, "Criar produtos novos automaticamente (margem 30%)", 0, 32, 400, true);
        btnImportar = Botoes.Sucesso("IMPORTAR", 280, 60);
        btnImportar.Dock = DockStyle.Right;
        btnImportar.Font = new Font(Tema.FontFamily, 12, FontStyle.Bold);
        btnImportar.Enabled = false;
        btnImportar.Click += async (s, e) => await ImportarAsync();
        rodape.Controls.Add(btnImportar);

        Controls.Add(cardGrid);
        Controls.Add(rodape);
        Controls.Add(cardCabecalho);
        Controls.Add(seletor);
        Controls.Add(header);
    }

    private async Task AnalisarAsync()
    {
        if (string.IsNullOrWhiteSpace(txtArquivo.Text)) { Toast.Mostrar("Selecione um XML.", TipoToast.Info, owner: this); return; }
        UseWaitCursor = true;
        try
        {
            var res = await _svc.ParsearAsync(txtArquivo.Text);
            if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Falha ao analisar", TipoToast.Erro, owner: this); return; }
            _previa = res.Valor!;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"  Emitente: {_previa.RazaoSocialEmitente}  ({FormatarCnpj(_previa.CnpjEmitente)})");
            sb.AppendLine($"  NF-e {_previa.Numero}    Emissão: {_previa.DataEmissao:dd/MM/yyyy}    Total: {_previa.ValorTotal:C}");
            sb.AppendLine($"  Itens: {_previa.Itens.Count}    Fornecedor: {(_previa.FornecedorExistente != null ? "cadastrado" : "será criado")}    Duplicatas: {_previa.ValoresDuplicatas.Count}");
            lblCabecalho.Text = sb.ToString();

            grid.Rows.Clear();
            foreach (var i in _previa.Itens)
            {
                var status = i.ProdutoIdExistente.HasValue ? "OK" : "NOVO";
                int idx = grid.Rows.Add(i.Codigo, i.CodigoBarras, i.Descricao,
                    i.Quantidade.ToString("N3"), i.ValorUnitario.ToString("N2"),
                    i.ValorTotal.ToString("N2"), status);
                grid.Rows[idx].DefaultCellStyle.BackColor = i.ProdutoIdExistente.HasValue
                    ? Tema.CorSucessoSoft : Tema.CorAlertaSoft;
                grid.Rows[idx].Cells["Status"].Style.ForeColor = i.ProdutoIdExistente.HasValue
                    ? Tema.CorSucesso : Tema.CorAlerta;
                grid.Rows[idx].Cells["Status"].Style.Font = Tema.FontCorpoBold;
            }
            btnImportar.Enabled = _previa.Itens.Any();
        }
        finally { UseWaitCursor = false; }
    }

    private async Task ImportarAsync()
    {
        if (_previa == null) return;
        if (MessageBox.Show($"Confirma importação de {_previa.Itens.Count} itens?",
            "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        UseWaitCursor = true;
        try
        {
            var res = await _svc.ImportarAsync(_previa, chkConta.Checked, chkCriarProd.Checked);
            if (!res.Sucesso) { Toast.Mostrar(res.Erro ?? "Falha", TipoToast.Erro, owner: this); return; }
            Toast.Mostrar($"Importação concluída: {res.Valor} movimentações.", TipoToast.Sucesso, owner: this);
            btnImportar.Enabled = false;
            _previa = null;
            grid.Rows.Clear();
            lblCabecalho.Text = "";
            txtArquivo.Clear();
        }
        finally { UseWaitCursor = false; }
    }

    private static string FormatarCnpj(string cnpj)
    {
        var d = new string(cnpj.Where(char.IsDigit).ToArray());
        return d.Length == 14
            ? $"{d.Substring(0, 2)}.{d.Substring(2, 3)}.{d.Substring(5, 3)}/{d.Substring(8, 4)}-{d.Substring(12, 2)}"
            : cnpj;
    }
}
