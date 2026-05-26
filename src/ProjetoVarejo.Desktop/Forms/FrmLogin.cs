using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Desktop.Theme;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmLogin : Form
{
    private readonly IAutenticacaoService _auth;
    private TextBox txtLogin = null!;
    private TextBox txtSenha = null!;
    private Button btnEntrar = null!;
    private Panel pnlErro = null!;
    private Label lblErro = null!;

    public FrmLogin(IAutenticacaoService auth)
    {
        _auth = auth;
        InitUi();
    }

    private void InitUi()
    {
        Text = $"{Tema.NomeProduto} | Acesso";
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
            Text = "\uE719",  // shop glyph
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
            Text = Tema.NomeProduto,
            Font = new Font(Tema.FontFamily, 24, FontStyle.Bold),
            ForeColor = Tema.Branco,
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var lblLogoTagline = new Label
        {
            Text = Tema.TaglineProduto,
            Font = new Font(Tema.FontFamily, 11),
            ForeColor = Color.FromArgb(200, 220, 240),
            Dock = DockStyle.Top,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };

        var lblFeatures = new Label
        {
            Text = "PDV  |  Estoque  |  Fiscal  |  Financeiro\nMultiempresa  |  Relatorios  |  PIX",
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
        var direito = new Panel { Dock = DockStyle.Fill, BackColor = Tema.Branco, Padding = new Padding(56, 62, 56, 56) };

        // Botão fechar (canto superior direito)
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
            Text = "Entre com suas credenciais para operar a empresa",
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
            Height = 26,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(0, 0, 0, 4)
        };

        var pnlLogin = CriarInputContainer(out txtLogin, "\uE77B");  // user glyph
        txtLogin.Text = "admin";

        // Campo Senha
        var lblCampoSenha = new Label
        {
            Text = "SENHA",
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(0, 10, 0, 4)
        };

        var pnlSenha = CriarInputContainer(out txtSenha, "\uE72E");  // lock glyph
        txtSenha.UseSystemPasswordChar = true;
        txtSenha.Text = "admin";

        // Toggle mostrar senha \u2014 posicionado \u00E0 direita do panel, sem usar Dock que conflita com TextBox absoluto
        var btnOlho = new Label
        {
            Text = "\uE890",  // view
            Font = new Font("Segoe MDL2 Assets", 14),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Tema.CorFundo,
            Top = 0, Width = 40, Height = pnlSenha.Height - 2,
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
        };
        btnOlho.Left = pnlSenha.Width - btnOlho.Width;
        btnOlho.Click += (s, e) =>
        {
            txtSenha.UseSystemPasswordChar = !txtSenha.UseSystemPasswordChar;
            btnOlho.Text = txtSenha.UseSystemPasswordChar ? "\uE890" : "\uED1A";  // view/hide
        };
        pnlSenha.Controls.Add(btnOlho);
        pnlSenha.Controls.SetChildIndex(btnOlho, 0);
        // Garantir que o TextBox de senha n\u00E3o invade a \u00E1rea do olho
        txtSenha.Width = pnlSenha.Width - 48 - 48;
        pnlSenha.SizeChanged += (s, e) => txtSenha.Width = pnlSenha.Width - 48 - 48;

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
            Text = "\uE7BA",  // warning glyph
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

        Controls.Add(direito);
        Controls.Add(lateral);

        var btnFechar = new Label
        {
            Text = "\uE8BB",
            Font = new Font("Segoe MDL2 Assets", 12),
            ForeColor = Tema.CorTextoMedio,
            Cursor = Cursors.Hand,
            Width = 36,
            Height = 34,
            Left = ClientSize.Width - 50,
            Top = 8,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Tema.Branco
        };
        btnFechar.Click += (s, e) => Close();
        btnFechar.MouseEnter += (s, e) => { btnFechar.ForeColor = Tema.CorErro; btnFechar.BackColor = Color.FromArgb(255, 240, 240); };
        btnFechar.MouseLeave += (s, e) => { btnFechar.ForeColor = Tema.CorTextoMedio; btnFechar.BackColor = Tema.Branco; };
        Controls.Add(btnFechar);
        btnFechar.BringToFront();
        SizeChanged += (s, e) =>
        {
            btnFechar.Left = ClientSize.Width - 50;
            btnFechar.Top = 8;
        };

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
        // Panel maior pra dar respiro vertical aos textos
        var pnl = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Tema.CorFundo,
            Margin = new Padding(0, 6, 0, 6)
        };

        var lblIcone = new Label
        {
            Text = iconeGlyph,
            Font = new Font("Segoe MDL2 Assets", 16),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Tema.CorFundo,
            Left = 0, Top = 0, Width = 44, Height = pnl.Height - 2,
            TextAlign = ContentAlignment.MiddleCenter,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom
        };

        // TextBox com posição absoluta — a fonte de 13pt tem altura natural ~22px.
        // Centralizo verticalmente no panel de 56px usando Top = (56-22)/2 - 1
        tb = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = new Font(Tema.FontFamily, 12),
            BackColor = Tema.CorFundo,
            ForeColor = Tema.CorTextoEscuro,
            Left = 48,
            Top = 18,
            Width = pnl.Width - 100,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        };

        var bordaFoco = new Panel
        {
            Left = 0, Top = pnl.Height - 2, Width = pnl.Width, Height = 2,
            BackColor = Tema.CorBorda,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };
        tb.Enter += (s, e) => bordaFoco.BackColor = Tema.CorPrimaria;
        tb.Leave += (s, e) => bordaFoco.BackColor = Tema.CorBorda;

        pnl.Controls.Add(bordaFoco);
        pnl.Controls.Add(tb);
        pnl.Controls.Add(lblIcone);

        // Reposiciona o TextBox quando o panel muda de largura
        // (o Anchor cuida do width, mas o handler também ajusta quando o senha tem o botão olho à direita)
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
