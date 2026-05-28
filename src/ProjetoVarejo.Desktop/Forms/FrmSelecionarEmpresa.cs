using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmSelecionarEmpresa : Form
{
    private readonly INfceService _svc;
    private readonly SessaoApp _sessao;
    private FlowLayoutPanel pnlEmpresas = null!;

    public FrmSelecionarEmpresa(INfceService svc, SessaoApp sessao)
    {
        _svc = svc;
        _sessao = sessao;
        InitUi();
        Shown += async (s, e) => await CarregarAsync();
    }

    private void InitUi()
    {
        Text = "Selecionar Empresa";
        Size = new Size(720, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Tema.CorFundo;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var header = Inputs.HeaderPagina("Empresa Ativa", "Selecione qual empresa deseja operar nesta sessão");

        var card = new Card { Dock = DockStyle.Fill, Padding = new Padding(20) };
        pnlEmpresas = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, BackColor = Tema.CorCard };
        card.Controls.Add(pnlEmpresas);

        Controls.Add(card);
        Controls.Add(header);
    }

    private async Task CarregarAsync()
    {
        var empresas = await _svc.ListarEmpresasAsync();
        pnlEmpresas.Controls.Clear();
        foreach (var emp in empresas)
            pnlEmpresas.Controls.Add(CardEmpresa(emp));
    }

    private Control CardEmpresa(EmpresaConfig emp)
    {
        var pnl = new Panel
        {
            Width = 620,
            Height = 90,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Tema.CorCard,
            Cursor = Cursors.Hand
        };
        pnl.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1);
            using var path = Tema.PathArredondado(rect, 6);
            using var brush = new SolidBrush(pnl.BackColor);
            g.FillPath(brush, path);
            using var pen = new Pen(Tema.CorBorda, 1);
            g.DrawPath(pen, path);
        };
        var icone = new Label
        {
            Text = "",  // building
            Dock = DockStyle.Left, Width = 80,
            Font = new Font("Segoe MDL2 Assets", 24),
            ForeColor = Tema.CorPrimaria,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        var info = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(10, 14, 0, 14) };
        var lblNome = new Label
        {
            Text = string.IsNullOrWhiteSpace(emp.NomeFantasia) ? emp.RazaoSocial : emp.NomeFantasia,
            Dock = DockStyle.Top, Height = 26,
            Font = new Font(Tema.FontFamily, 13, FontStyle.Bold),
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent
        };
        var lblDados = new Label
        {
            Text = $"CNPJ {emp.Cnpj}   •   {emp.Cidade}/{emp.Uf}",
            Dock = DockStyle.Top, Height = 22,
            Font = Tema.FontCorpo,
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        };
        info.Controls.Add(lblDados);
        info.Controls.Add(lblNome);
        var chevron = new Label
        {
            Text = "",  // chevron right
            Dock = DockStyle.Right, Width = 50,
            Font = new Font("Segoe MDL2 Assets", 14),
            ForeColor = Tema.CorTextoClaro,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        pnl.Controls.Add(info);
        pnl.Controls.Add(chevron);
        pnl.Controls.Add(icone);

        EventHandler clk = (s, e) =>
        {
            _sessao.EmpresaAtiva = emp;
            DialogResult = DialogResult.OK;
            Close();
        };
        pnl.Click += clk;
        foreach (Control c in pnl.Controls) c.Click += clk;

        pnl.MouseEnter += (s, e) => { pnl.BackColor = Tema.CorPrimariaSoft; foreach (Control c in pnl.Controls) c.BackColor = Tema.CorPrimariaSoft; };
        pnl.MouseLeave += (s, e) => { pnl.BackColor = Tema.CorCard; foreach (Control c in pnl.Controls) c.BackColor = Color.Transparent; };

        return pnl;
    }
}
