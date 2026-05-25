using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmPagamento : Form
{
    private readonly decimal _total;
    public List<PagamentoVenda> Pagamentos { get; } = new();

    private ComboBox cboForma = null!;
    private TextBox txtValor = null!;
    private StyledGrid grid = null!;
    private Label lblFalta = null!;
    private Label lblTroco = null!;
    private Button btnConfirmar = null!;

    public FrmPagamento(decimal total)
    {
        _total = total;
        InitUi();
        AtualizarSaldos();
        txtValor.Focus();
        txtValor.SelectAll();
    }

    private void InitUi()
    {
        Text = "Pagamento";
        Size = new Size(820, 620);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = Tema.CorFundo;
        Padding = new Padding(Tema.EspacamentoGrande);

        // === Header com total grande ===
        var header = new Card
        {
            Dock = DockStyle.Top,
            Height = 130,
            Padding = new Padding(20),
            ComSombra = true
        };
        header.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(3, 3, header.Width - 7, header.Height - 7);
            using var path = Tema.PathArredondado(rect, Tema.RaioCard);
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(rect, Tema.CorPrimaria, Tema.CorPrimariaDark, System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            g.FillPath(brush, path);
        };

        var pnlHdr = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        pnlHdr.Controls.Add(new Label
        {
            Text = "TOTAL A PAGAR",
            Dock = DockStyle.Top, Height = 26,
            Font = new Font(Tema.FontFamily, 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 200, 230),
            BackColor = Color.Transparent
        });
        pnlHdr.Controls.Add(new Label
        {
            Text = _total.ToString("C"),
            Dock = DockStyle.Top, Height = 64,
            Font = new Font(Tema.FontFamily, 32, FontStyle.Bold),
            ForeColor = Tema.Branco,
            BackColor = Color.Transparent
        });
        header.Controls.Add(pnlHdr);

        // === Entrada de forma + valor ===
        var entradaCard = new Card { Dock = DockStyle.Top, Height = 110, Padding = new Padding(20) };
        var pnlEntrada = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

        pnlEntrada.Controls.Add(Inputs.Rotulo("FORMA DE PAGAMENTO", 0, 0));
        cboForma = new ComboBox
        {
            Left = 0, Top = 20, Width = 280, Height = 40,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font(Tema.FontFamily, 12),
            FlatStyle = FlatStyle.Flat
        };
        foreach (FormaPagamentoTipo f in Enum.GetValues(typeof(FormaPagamentoTipo)))
            cboForma.Items.Add(f);
        cboForma.SelectedIndex = 0;
        pnlEntrada.Controls.Add(cboForma);

        pnlEntrada.Controls.Add(Inputs.Rotulo("VALOR (R$)", 300, 0));
        txtValor = new TextBox
        {
            Left = 300, Top = 20, Width = 180, Height = 40,
            Font = new Font(Tema.FontFamily, 16, FontStyle.Bold),
            TextAlign = HorizontalAlignment.Right,
            BorderStyle = BorderStyle.FixedSingle,
            Text = _total.ToString("N2")
        };
        pnlEntrada.Controls.Add(txtValor);

        var btnAdd = Botoes.Sucesso("Adicionar (Enter)", 220, 40);
        btnAdd.Top = 20; btnAdd.Left = 500;
        btnAdd.Click += (s, e) => AdicionarPagamento();
        pnlEntrada.Controls.Add(btnAdd);
        AcceptButton = btnAdd;

        entradaCard.Controls.Add(pnlEntrada);

        // === Grid de pagamentos ===
        var cardGrid = new Card { Dock = DockStyle.Fill, Padding = new Padding(0) };
        grid = new StyledGrid();
        grid.Columns.Add("Forma", "Forma");
        grid.Columns.Add("Valor", "Valor");
        grid.Columns["Valor"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        grid.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Delete && grid.SelectedRows.Count > 0)
            {
                Pagamentos.RemoveAt(grid.SelectedRows[0].Index);
                grid.Rows.RemoveAt(grid.SelectedRows[0].Index);
                AtualizarSaldos();
            }
        };
        cardGrid.Controls.Add(grid);

        // === Rodapé: falta / troco / confirmar ===
        var rodape = new Panel { Dock = DockStyle.Bottom, Height = 140, BackColor = Tema.CorFundo, Padding = new Padding(0, 15, 0, 0) };

        var cardFalta = new Card { Dock = DockStyle.Left, Width = 270, Padding = new Padding(15) };
        var pnlF = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        pnlF.Controls.Add(new Label { Text = "FALTA", Dock = DockStyle.Top, Height = 22, Font = new Font(Tema.FontFamily, 9, FontStyle.Bold), ForeColor = Tema.CorErro });
        lblFalta = new Label { Dock = DockStyle.Fill, Font = new Font(Tema.FontFamily, 22, FontStyle.Bold), ForeColor = Tema.CorErro, TextAlign = ContentAlignment.MiddleLeft };
        pnlF.Controls.Add(lblFalta);
        cardFalta.Controls.Add(pnlF);

        var cardTroco = new Card { Dock = DockStyle.Left, Width = 270, Padding = new Padding(15) };
        var pnlT = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        pnlT.Controls.Add(new Label { Text = "TROCO", Dock = DockStyle.Top, Height = 22, Font = new Font(Tema.FontFamily, 9, FontStyle.Bold), ForeColor = Tema.CorSucesso });
        lblTroco = new Label { Dock = DockStyle.Fill, Font = new Font(Tema.FontFamily, 22, FontStyle.Bold), ForeColor = Tema.CorSucesso, TextAlign = ContentAlignment.MiddleLeft };
        pnlT.Controls.Add(lblTroco);
        cardTroco.Controls.Add(pnlT);

        btnConfirmar = Botoes.Sucesso("  CONFIRMAR (F10)", 220, 110);
        btnConfirmar.Dock = DockStyle.Right;
        btnConfirmar.Font = new Font(Tema.FontFamily, 14, FontStyle.Bold);
        btnConfirmar.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

        rodape.Controls.Add(btnConfirmar);
        rodape.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 10 });
        rodape.Controls.Add(cardTroco);
        rodape.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 10 });
        rodape.Controls.Add(cardFalta);

        Controls.Add(cardGrid);
        Controls.Add(rodape);
        Controls.Add(entradaCard);
        Controls.Add(header);

        KeyPreview = true;
        KeyDown += (s, e) => { if (e.KeyCode == Keys.F10) { DialogResult = DialogResult.OK; Close(); } };
    }

    private void AdicionarPagamento()
    {
        if (!decimal.TryParse(txtValor.Text.Replace('.', ','), out var v) || v <= 0)
        {
            Toast.Mostrar("Valor inválido.", TipoToast.Erro, owner: this);
            txtValor.Focus(); txtValor.SelectAll();
            return;
        }
        var forma = (FormaPagamentoTipo)cboForma.SelectedItem!;
        Pagamentos.Add(new PagamentoVenda { FormaPagamento = forma, Valor = v });
        grid.Rows.Add(forma.ToString(), v.ToString("N2"));
        AtualizarSaldos();
        var falta = _total - Pagamentos.Sum(p => p.Valor);
        txtValor.Text = (falta > 0 ? falta : 0).ToString("N2");
        txtValor.Focus(); txtValor.SelectAll();
    }

    private void AtualizarSaldos()
    {
        var pago = Pagamentos.Sum(p => p.Valor);
        var falta = _total - pago;
        if (falta > 0)
        {
            lblFalta.Text = falta.ToString("C");
            lblTroco.Text = "R$ 0,00";
            btnConfirmar.Enabled = false;
        }
        else
        {
            lblFalta.Text = "R$ 0,00";
            lblTroco.Text = (-falta).ToString("C");
            btnConfirmar.Enabled = true;
        }
    }
}
