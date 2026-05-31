using Microsoft.Data.SqlClient;
using ProjetoVarejo.Desktop.Theme;
using System.Text.Json;

namespace ProjetoVarejo.Desktop.Forms;

/// <summary>
/// Formulário exibido no modo cliente quando a conexão com o servidor SQL não pode ser
/// estabelecida. Permite ao usuário corrigir o IP/instância e testar antes de continuar.
/// Salva diretamente no appsettings.json — sem dependência de DI.
/// </summary>
public class FrmConexaoServidor : Form
{
    // ── Controles ────────────────────────────────────────────────────────────
    private TextBox txtIp       = null!;
    private TextBox txtInstancia = null!;
    private Label   lblStatus   = null!;
    private Panel   pnlStatus   = null!;
    private Button  btnTestar   = null!;
    private Button  btnSalvar   = null!;

    // ── Estado ───────────────────────────────────────────────────────────────
    private bool _conexaoOk;

    // ── Construtor ───────────────────────────────────────────────────────────
    public FrmConexaoServidor(string connectionStringAtual)
    {
        ParseConnectionString(connectionStringAtual, out var ip, out var instancia);
        InitUi(ip, instancia);
    }

    // ── UI ───────────────────────────────────────────────────────────────────
    private void InitUi(string ipInicial, string instanciaInicial)
    {
        Text             = "Conexão com o Servidor";
        Size             = new Size(520, 430);
        MinimumSize      = new Size(520, 430);
        MaximumSize      = new Size(520, 430);
        FormBorderStyle  = FormBorderStyle.FixedDialog;
        MaximizeBox      = false;
        MinimizeBox      = false;
        StartPosition    = FormStartPosition.CenterScreen;
        BackColor        = Tema.CorFundo;
        DoubleBuffered   = true;

        // ── Topbar âmbar ────────────────────────────────────────────────────
        var topbar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 68,
            BackColor = Color.FromArgb(217, 119, 6)   // amber-600
        };

        var lblIcone = new Label
        {
            Text      = "⚠",
            Left      = 20, Top = 14,
            Width     = 40, Height = 40,
            Font      = new Font("Segoe UI", 22, FontStyle.Regular),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        var lblTitulo = new Label
        {
            Text      = "Servidor SQL não encontrado",
            Left      = 68, Top = 12,
            Width     = 420, Height = 24,
            Font      = new Font(Tema.FontFamily, 13, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent
        };

        var lblSub = new Label
        {
            Text      = "Corrija o endereço do servidor e teste a conexão antes de continuar.",
            Left      = 68, Top = 38,
            Width     = 420, Height = 20,
            Font      = new Font(Tema.FontFamily, 9),
            ForeColor = Color.FromArgb(254, 243, 199),
            BackColor = Color.Transparent
        };

        topbar.Controls.Add(lblIcone);
        topbar.Controls.Add(lblTitulo);
        topbar.Controls.Add(lblSub);

        // ── Corpo ────────────────────────────────────────────────────────────
        var corpo = new Panel
        {
            Left    = 0, Top = 68,
            Width   = 520, Height = 290,
            Padding = new Padding(28, 20, 28, 0),
            BackColor = Tema.CorFundo
        };

        // Descrição
        var lblDesc = new Label
        {
            Text      = "Informe o IP ou nome de rede do servidor onde o\nProjetoVarejo está instalado como SERVIDOR:",
            Left      = 28, Top = 20,
            Width     = 460, Height = 40,
            Font      = new Font(Tema.FontFamily, 10),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        };

        // Campo IP
        var lblIp = Inputs.Rotulo("IP ou hostname do servidor", 28, 68, 220);
        txtIp = new TextBox
        {
            Left        = 28, Top = 90,
            Width       = 290, Height = Tema.AlturaInput,
            Font        = Tema.FontCorpo,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor   = Tema.CorCard,
            ForeColor   = Tema.CorTextoEscuro,
            Text        = ipInicial
        };
        txtIp.TextChanged += (s, e) => ResetarStatus();

        // Campo Instância
        var lblInst = Inputs.Rotulo("Instância SQL Server", 332, 68, 160);
        txtInstancia = new TextBox
        {
            Left        = 332, Top = 90,
            Width       = 160, Height = Tema.AlturaInput,
            Font        = Tema.FontCorpo,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor   = Tema.CorCard,
            ForeColor   = Tema.CorTextoEscuro,
            Text        = instanciaInicial,
            PlaceholderText = "SQLEXPRESS"
        };
        txtInstancia.TextChanged += (s, e) => ResetarStatus();

        // Botão Testar
        btnTestar = new Button
        {
            Text      = "Testar conexão",
            Left      = 28, Top = 136,
            Width     = 160, Height = 38,
            Font      = new Font(Tema.FontFamily, 10, FontStyle.Bold),
            BackColor = Color.FromArgb(217, 119, 6),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand
        };
        btnTestar.FlatAppearance.BorderSize = 0;
        btnTestar.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 95, 0);
        btnTestar = Botoes.Aviso("Testar conex\u00e3o", 160, 38);
        btnTestar.Left = 28;
        btnTestar.Top = 136;
        btnTestar.Click += async (s, e) => await TestarAsync();

        // Painel de status (inicialmente oculto)
        pnlStatus = new Panel
        {
            Left      = 28, Top = 186,
            Width     = 464, Height = 46,
            BackColor = Color.FromArgb(241, 245, 249),
            Visible   = false
        };
        pnlStatus.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(200, 200, 200), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pnlStatus.Width - 1, pnlStatus.Height - 1);
        };

        lblStatus = new Label
        {
            Left      = 12, Top = 0,
            Width     = 440, Height = 46,
            Font      = new Font(Tema.FontFamily, 10),
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlStatus.Controls.Add(lblStatus);

        // Nota sobre Trusted_Connection
        var lblNota = new Label
        {
            Text      = "A conexão usa Autenticação Windows (Trusted Connection). " +
                        "O usuário de serviço do SQL Server precisa aceitar conexões da rede.",
            Left      = 28, Top = 244,
            Width     = 464, Height = 36,
            Font      = new Font(Tema.FontFamily, 8),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        };

        corpo.Controls.Add(lblDesc);
        corpo.Controls.Add(lblIp);
        corpo.Controls.Add(txtIp);
        corpo.Controls.Add(lblInst);
        corpo.Controls.Add(txtInstancia);
        corpo.Controls.Add(btnTestar);
        corpo.Controls.Add(pnlStatus);
        corpo.Controls.Add(lblNota);

        // ── Rodapé ───────────────────────────────────────────────────────────
        var rodape = new Panel
        {
            Left      = 0, Top = 358,
            Width     = 520, Height = 72,
            BackColor = Tema.CorFundo,
            Padding   = new Padding(28, 16, 28, 0)
        };

        // Separador
        rodape.Paint += (s, e) =>
        {
            using var pen = new Pen(Tema.CorBorda, 1);
            e.Graphics.DrawLine(pen, 0, 0, rodape.Width, 0);
        };

        btnSalvar = new Button
        {
            Text      = "Salvar e Continuar",
            Left      = 228, Top = 16,
            Width     = 160, Height = 40,
            Font      = new Font(Tema.FontFamily, 10, FontStyle.Bold),
            BackColor = Tema.CorSucesso,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand,
            Enabled   = false
        };
        btnSalvar.FlatAppearance.BorderSize = 0;
        btnSalvar.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 128, 61);
        btnSalvar = Botoes.Sucesso("Salvar e Continuar", 160, 40);
        btnSalvar.Left = 228;
        btnSalvar.Top = 16;
        btnSalvar.Enabled = false;
        btnSalvar.Click += BtnSalvar_Click;

        var btnCancelar = new Button
        {
            Text      = "Cancelar",
            Left      = 400, Top = 16,
            Width     = 92, Height = 40,
            Font      = Tema.FontCorpo,
            BackColor = Tema.CorCard,
            ForeColor = Tema.CorTextoEscuro,
            FlatStyle = FlatStyle.Flat,
            Cursor    = Cursors.Hand
        };
        btnCancelar.FlatAppearance.BorderSize = 1;
        btnCancelar.FlatAppearance.BorderColor = Tema.CorBorda;
        btnCancelar = Botoes.Ghost("Cancelar", 92, 40);
        btnCancelar.Left = 400;
        btnCancelar.Top = 16;
        btnCancelar.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

        rodape.Controls.Add(btnSalvar);
        rodape.Controls.Add(btnCancelar);

        Controls.Add(topbar);
        Controls.Add(corpo);
        Controls.Add(rodape);

        // Enter = testar
        AcceptButton = btnTestar;
    }

    // ── Lógica ───────────────────────────────────────────────────────────────

    private async Task TestarAsync()
    {
        var ip = txtIp.Text.Trim();
        var instancia = txtInstancia.Text.Trim();

        if (string.IsNullOrWhiteSpace(ip))
        {
            MostrarStatus("Informe o IP ou hostname do servidor.", false, aguardando: false);
            return;
        }

        MostrarStatus("Testando conexão... aguarde até 8 segundos.", false, aguardando: true);
        btnTestar.Enabled = false;
        btnSalvar.Enabled = false;
        _conexaoOk = false;

        try
        {
            var dataSource = string.IsNullOrWhiteSpace(instancia)
                ? ip
                : $@"{ip}\{instancia}";

            var connStr = new SqlConnectionStringBuilder
            {
                DataSource          = dataSource,
                InitialCatalog      = "master",   // testa com master — DB pode não existir ainda
                IntegratedSecurity  = true,
                TrustServerCertificate = true,
                Encrypt             = false,
                ConnectTimeout      = 8
            }.ConnectionString;

            await Task.Run(() =>
            {
                using var conn = new SqlConnection(connStr);
                conn.Open();
                conn.Close();
            });

            _conexaoOk = true;
            MostrarStatus($"✓ Servidor alcançado em {dataSource}", sucesso: true, aguardando: false);
            btnSalvar.Enabled = true;
        }
        catch (Exception ex)
        {
            _conexaoOk = false;
            var msg = ex.InnerException?.Message ?? ex.Message;
            // Encurtar mensagens longas do SqlClient
            if (msg.Length > 120) msg = msg[..120] + "…";
            MostrarStatus($"✗ {msg}", sucesso: false, aguardando: false);
        }
        finally
        {
            btnTestar.Enabled = true;
        }
    }

    private void MostrarStatus(string texto, bool sucesso, bool aguardando)
    {
        pnlStatus.Visible = true;

        if (aguardando)
        {
            pnlStatus.BackColor = Color.FromArgb(254, 243, 199);  // âmbar claro
            lblStatus.ForeColor = Color.FromArgb(120, 80, 0);
        }
        else if (sucesso)
        {
            pnlStatus.BackColor = Color.FromArgb(220, 252, 231);  // verde claro
            lblStatus.ForeColor = Color.FromArgb(20, 83, 45);
        }
        else
        {
            pnlStatus.BackColor = Color.FromArgb(254, 226, 226);  // vermelho claro
            lblStatus.ForeColor = Color.FromArgb(127, 29, 29);
        }

        lblStatus.Text = texto;
        pnlStatus.Invalidate();
    }

    private void ResetarStatus()
    {
        _conexaoOk = false;
        btnSalvar.Enabled = false;
        pnlStatus.Visible = false;
    }

    private void BtnSalvar_Click(object? sender, EventArgs e)
    {
        if (!_conexaoOk) return;

        try
        {
            SalvarConnectionString();
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao salvar configuração:\n{ex.Message}",
                "Erro",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void SalvarConnectionString()
    {
        var ip        = txtIp.Text.Trim();
        var instancia = txtInstancia.Text.Trim();

        var dataSource = string.IsNullOrWhiteSpace(instancia)
            ? ip
            : $@"{ip}\{instancia}";

        // Monta connection string igual ao padrão do instalador
        var connStr = $@"Server={dataSource};Database=ProjetoVarejo;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";

        // JSON mínimo (mesmo formato gerado pelo setup-cliente.iss)
        var json = "{\r\n" +
                   "  \"ConnectionStrings\": {\r\n" +
                   $"    \"Default\": \"{connStr}\"\r\n" +
                   "  },\r\n" +
                   "  \"Logging\": {\r\n" +
                   "    \"LogLevel\": {\r\n" +
                   "      \"Default\": \"Warning\",\r\n" +
                   "      \"Microsoft\": \"Error\",\r\n" +
                   "      \"System\": \"Error\"\r\n" +
                   "    }\r\n" +
                   "  }\r\n" +
                   "}";

        var base64     = AppContext.BaseDirectory;
        var arqBase    = Path.Combine(base64, "appsettings.json");
        var arqProd    = Path.Combine(base64, "appsettings.Production.json");

        File.WriteAllText(arqBase, json, System.Text.Encoding.UTF8);
        File.WriteAllText(arqProd, json, System.Text.Encoding.UTF8);
    }

    // ── Helpers estáticos ─────────────────────────────────────────────────────

    /// <summary>
    /// Retorna true quando a connection string aponta para um servidor remoto
    /// (modo cliente). Servidores locais (., localhost, 127.0.0.1) são ignorados.
    /// </summary>
    public static bool IsModoCliente(string connectionString)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var servidor = builder.DataSource.Split('\\')[0].Trim().ToLowerInvariant();
            return servidor != "."
                && servidor != "localhost"
                && servidor != "127.0.0.1"
                && servidor != "(local)";
        }
        catch { return false; }
    }

    /// <summary>
    /// Testa a conexão com o SQL Server de forma síncrona.
    /// Usa timeout curto para não travar a inicialização.
    /// </summary>
    public static bool TestarConexao(string connectionString, int timeoutSegundos = 5)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = timeoutSegundos,
                InitialCatalog = "master"
            };
            using var conn = new SqlConnection(builder.ConnectionString);
            conn.Open();
            conn.Close();
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Extrai IP e instância de uma connection string existente.
    /// </summary>
    private static void ParseConnectionString(string cs, out string ip, out string instancia)
    {
        ip        = "";
        instancia = "SQLEXPRESS";
        try
        {
            var builder = new SqlConnectionStringBuilder(cs);
            var partes  = builder.DataSource.Split('\\');
            ip        = partes[0];
            instancia = partes.Length > 1 ? partes[1] : "";
        }
        catch { }
    }
}
