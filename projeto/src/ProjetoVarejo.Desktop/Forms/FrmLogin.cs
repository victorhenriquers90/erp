using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmLogin : Form
{
    private readonly AutenticacaoService _auth;
    private TextBox txtLogin = null!;
    private TextBox txtSenha = null!;
    private Button btnEntrar = null!;
    private Panel pnlErro = null!;
    private Label lblErro = null!;

    public FrmLogin(AutenticacaoService auth)
    {
        _auth = auth;
        InitUi();
    }

    private void InitUi()
    {
        Text = "ProjetoVarejo - Acesso";
        Size = new Size(900, 580);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Tema.Branco;
        DoubleBuffered = true;

        // === Lado esquerdo - Branding ===
        var lateral = new Panel { Dock = DockStyle.Left, Width = 360, BackColor = Tema.ShellBarFundo };
        lateral.Paint += (s, e) =>
        {
            // Gradiente sutil
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                lateral.ClientRectangle, Tema.ShellBarFundo, Tema.SidebarFundo,
                System.Drawing.Drawing2D.LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(brush, lateral.ClientRectangle);

            // Círculos decorativos
            using var circulo = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(circulo, -100, -100, 280, 280);
            e.Graphics.FillEllipse(circulo, lateral.Width - 180, lateral.Height - 180, 320, 320);
        };

        var lblLogoIcone = new Label
        {
            Text = "",  // store / shop glyph
            Font = new Font("Segoe MDL2 Assets", 56),
            ForeColor = Tema.Branco,
            Dock = DockStyle.Top,
            Height = 130,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 60, 0, 0)
        };

        var lblLogoTitulo = new Label
        {
            Text = "ProjetoVarejo ERP",
            Font = new Font(Tema.FontFamily, 24, FontStyle.Bold),
            ForeColor = Tema.Branco,
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var lblLogoTagline = new Label
        {
            Text = "Sistema de gestão para varejo",
            Font = new Font(Tema.FontFamily, 11),
            ForeColor = Color.FromArgb(200, 220, 240),
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var lblFeatures = new Label
        {
            Text = "PDV  •  Estoque  •  NFC-e  •  Financeiro\nMulti-empresa  •  Relatórios  •  PIX",
            Font = new Font(Tema.FontFamily, 9),
            ForeColor = Color.FromArgb(180, 200, 220),
            Dock = DockStyle.Bottom,
            Height = 70,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 30)
        };

        var lblVersao = new Label
        {
            Text = "v1.0.0",
            Font = new Font(Tema.FontFamily, 8),
            ForeColor = Color.FromArgb(150, 170, 190),
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        lateral.Controls.Add(lblVersao);
        lateral.Controls.Add(lblFeatures);
        lateral.Controls.Add(lblLogoTagline);
        lateral.Controls.Add(lblLogoTitulo);
        lateral.Controls.Add(lblLogoIcone);

        // === Lado direito - Formulário ===
        var direito = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Branco, Padding = new Padding(56) };

        // Botão fechar (canto superior direito)
        var btnFechar = new Label
        {
            Text = "",  // X glyph
            Font = new Font("Segoe MDL2 Assets", 12),
            ForeColor = Tema.CorTextoMedio,
            Cursor = Cursors.Hand,
            Width = 40, Height = 40,
            TextAlign = ContentAlignment.MiddleCenter
        };
        btnFechar.Click += (s, e) => Close();
        btnFechar.MouseEnter += (s, e) => { btnFechar.ForeColor = Tema.CorErro; btnFechar.BackColor = Color.FromArgb(255, 240, 240); };
        btnFechar.MouseLeave += (s, e) => { btnFechar.ForeColor = Tema.CorTextoMedio; btnFechar.BackColor = Tema.Branco; };

        var pnlFechar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Tema.Branco };
        btnFechar.Location = new Point(direito.Width - 100, 0);
        btnFechar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        pnlFechar.Controls.Add(btnFechar);

        // Conteúdo central
        var conteudo = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Branco };

        var lblBemVindo = new Label
        {
            Text = "Acesso ao ERP",
            Font = new Font(Tema.FontFamily, 20, FontStyle.Bold),
            ForeColor = Tema.CorTextoEscuro,
            Dock = DockStyle.Top,
            Height = 45,
            TextAlign = ContentAlignment.BottomLeft
        };
        var lblSub = new Label
        {
            Text = "Entre com suas credenciais para acessar o sistema",
            Font = new Font(Tema.FontFamily, 10),
            ForeColor = Tema.CorTextoMedio,
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.TopLeft
        };

        // Campo Usuário
        var lblCampoLogin = new Label
        {
            Text = "USUÁRIO",
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(0, 8, 0, 0)
        };

        var pnlLogin = CriarInputContainer(out txtLogin, "");  // user glyph
        txtLogin.Text = "admin";

        // Campo Senha
        var lblCampoSenha = new Label
        {
            Text = "SENHA",
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            Dock = DockStyle.Top,
            Height = 22,
            Padding = new Padding(0, 12, 0, 0)
        };

        var pnlSenha = CriarInputContainer(out txtSenha, "");  // lock glyph
        txtSenha.UseSystemPasswordChar = true;
        txtSenha.Text = "admin";

        // Toggle mostrar senha
        var btnOlho = new Label
        {
            Text = "",  // hide
            Font = new Font("Segoe MDL2 Assets", 12),
            ForeColor = Tema.CorTextoMedio,
            Dock = DockStyle.Right,
            Width = 40,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            BackColor = Tema.CorFundo
        };
        btnOlho.Click += (s, e) =>
        {
            txtSenha.UseSystemPasswordChar = !txtSenha.UseSystemPasswordChar;
            btnOlho.Text = txtSenha.UseSystemPasswordChar ? "" : "";  // hide/show
        };
        pnlSenha.Controls.Add(btnOlho);
        pnlSenha.Controls.SetChildIndex(btnOlho, 0);

        // Link "Esqueceu a senha?"
        var pnlEsqueceu = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Tema.Branco };
        var lblEsqueceu = new Label
        {
            Text = "Esqueceu a senha?",
            Font = new Font(Tema.FontFamily, 9),
            ForeColor = Tema.CorPrimaria,
            Dock = DockStyle.Right,
            Width = 140,
            TextAlign = ContentAlignment.MiddleRight,
            Cursor = Cursors.Hand
        };
        lblEsqueceu.MouseEnter += (s, e) => lblEsqueceu.Font = new Font(Tema.FontFamily, 9, FontStyle.Underline);
        lblEsqueceu.MouseLeave += (s, e) => lblEsqueceu.Font = new Font(Tema.FontFamily, 9);
        lblEsqueceu.Click += (s, e) => Toast.Mostrar("Contate o administrador para redefinir a senha.", TipoToast.Info, owner: this);
        pnlEsqueceu.Controls.Add(lblEsqueceu);

        // Botão Entrar
        var pnlBotao = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Tema.Branco, Padding = new Padding(0, 15, 0, 0) };
        btnEntrar = new Button
        {
            Text = "ENTRAR",
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamily, 11, FontStyle.Bold),
            BackColor = Tema.CorPrimaria,
            ForeColor = Tema.Branco,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false
        };
        btnEntrar.FlatAppearance.BorderSize = 0;
        btnEntrar.FlatAppearance.MouseOverBackColor = Tema.CorPrimariaDark;
        btnEntrar.FlatAppearance.MouseDownBackColor = Tema.ShellBarFundo;
        btnEntrar.Click += async (s, e) => await EntrarAsync();
        pnlBotao.Controls.Add(btnEntrar);
        AcceptButton = btnEntrar;

        // Painel de erro (inicialmente invisível)
        pnlErro = new Panel
        {
            Dock = DockStyle.Top,
            Height = 0,
            BackColor = Color.FromArgb(254, 226, 226),
            Padding = new Padding(15, 8, 15, 8),
            Visible = false
        };
        var lblIconeErro = new Label
        {
            Text = "",  // warning glyph
            Font = new Font("Segoe MDL2 Assets", 12),
            ForeColor = Tema.CorErro,
            Dock = DockStyle.Left,
            Width = 30,
            TextAlign = ContentAlignment.MiddleLeft
        };
        lblErro = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font(Tema.FontFamily, 9),
            ForeColor = Tema.CorErro,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlErro.Controls.Add(lblErro);
        pnlErro.Controls.Add(lblIconeErro);

        // Inversão para Dock.Top empilhar corretamente (último adicionado fica no topo)
        conteudo.Controls.Add(pnlBotao);
        conteudo.Controls.Add(pnlErro);
        conteudo.Controls.Add(pnlEsqueceu);
        conteudo.Controls.Add(pnlSenha);
        conteudo.Controls.Add(lblCampoSenha);
        conteudo.Controls.Add(pnlLogin);
        conteudo.Controls.Add(lblCampoLogin);
        conteudo.Controls.Add(lblSub);
        conteudo.Controls.Add(lblBemVindo);

        direito.Controls.Add(conteudo);
        direito.Controls.Add(pnlFechar);

        Controls.Add(direito);
        Controls.Add(lateral);

        // Permite arrastar a janela pela área branca (já que não tem TitleBar)
        AdicionarArrasto(lateral);

        // Borda sutil ao redor
        Paint += (s, e) =>
        {
            using var pen = new Pen(Tema.CorBorda, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };
    }

    private Panel CriarInputContainer(out TextBox tb, string iconeGlyph)
    {
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 48,
            BackColor = Tema.CorFundo,
            Padding = new Padding(2)
        };

        var lblIcone = new Label
        {
            Text = iconeGlyph,
            Font = new Font("Segoe MDL2 Assets", 14),
            ForeColor = Tema.CorTextoMedio,
            Dock = DockStyle.Left,
            Width = 45,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Tema.CorFundo
        };

        tb = new TextBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            Font = new Font(Tema.FontFamily, 12),
            BackColor = Tema.CorFundo,
            ForeColor = Tema.CorTextoEscuro
        };

        // Container interno para dar padding ao TextBox (que não suporta padding direto)
        var inner = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorFundo, Padding = new Padding(0, 14, 10, 0) };
        inner.Controls.Add(tb);

        var bordaFoco = new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = Tema.CorBorda };
        var tbRef = tb;
        tb.Enter += (s, e) => bordaFoco.BackColor = Tema.CorPrimaria;
        tb.Leave += (s, e) => bordaFoco.BackColor = Tema.CorBorda;

        pnl.Controls.Add(inner);
        pnl.Controls.Add(lblIcone);
        pnl.Controls.Add(bordaFoco);

        return pnl;
    }

    private void AdicionarArrasto(Control alvo)
    {
        bool arrastando = false;
        Point inicio = Point.Empty;
        alvo.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { arrastando = true; inicio = e.Location; } };
        alvo.MouseMove += (s, e) =>
        {
            if (arrastando)
            {
                Location = new Point(Location.X + e.X - inicio.X, Location.Y + e.Y - inicio.Y);
            }
        };
        alvo.MouseUp += (s, e) => arrastando = false;
    }

    private void MostrarErro(string mensagem)
    {
        lblErro.Text = mensagem;
        pnlErro.Visible = true;
        pnlErro.Height = 40;
    }

    private void EscondeErro()
    {
        pnlErro.Visible = false;
        pnlErro.Height = 0;
    }

    private async Task EntrarAsync()
    {
        EscondeErro();
        var textoOriginal = btnEntrar.Text;
        btnEntrar.Enabled = false;
        btnEntrar.Text = "Verificando...";
        try
        {
            var res = await _auth.LoginAsync(txtLogin.Text.Trim(), txtSenha.Text);
            if (res.Sucesso)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MostrarErro(res.Erro ?? "Credenciais inválidas.");
                txtSenha.SelectAll();
                txtSenha.Focus();
            }
        }
        catch (Exception ex)
        {
            MostrarErro("Erro: " + ex.Message);
        }
        finally
        {
            btnEntrar.Enabled = true;
            btnEntrar.Text = textoOriginal;
        }
    }
}
