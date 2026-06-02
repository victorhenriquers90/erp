namespace ProjetoVarejo.Desktop.Theme;

public enum TipoToast { Sucesso, Erro, Aviso, Info }

/// <summary>
/// Notificação flutuante no canto inferior direito. Substitui MessageBox para feedback rápido.
/// </summary>
public class Toast : Form
{
    private readonly System.Windows.Forms.Timer _timer;
    private double _opacidade = 0;
    private bool _saindo = false;

    public static void Mostrar(string mensagem, TipoToast tipo = TipoToast.Info, int duracaoMs = 3000, IWin32Window? owner = null)
    {
        var t = new Toast(mensagem, tipo, duracaoMs);
        t.Show(owner);
    }

    private Toast(string mensagem, TipoToast tipo, int duracaoMs)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Size = new Size(360, 80);
        Opacity = 0;
        BackColor = TipoCor(tipo);

        var screen = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point(screen.Right - Width - 20, screen.Bottom - Height - 20 - DeslocamentoOcupados());

        var pIcon = new Panel { Dock = DockStyle.Left, Width = 56, BackColor = Misturar(BackColor, Color.Black, 0.15f) };
        var lblIcone = new Label
        {
            Text = TipoIcone(tipo),
            Dock = DockStyle.Fill,
            Font = new Font("Segoe MDL2 Assets", 22),
            ForeColor = Tema.Branco,
            TextAlign = ContentAlignment.MiddleCenter
        };
        pIcon.Controls.Add(lblIcone);

        var lblMsg = new Label
        {
            Text = mensagem,
            Dock = DockStyle.Fill,
            ForeColor = Tema.Branco,
            Font = new Font(Tema.FontFamily, 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(10, 0, 10, 0)
        };

        Controls.Add(lblMsg);
        Controls.Add(pIcon);

        Click += (s, e) => SairAgora();
        lblMsg.Click += (s, e) => SairAgora();

        Registrar(this);

        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (s, e) => Animar();
        _timer.Start();

        var sair = new System.Windows.Forms.Timer { Interval = duracaoMs };
        sair.Tick += (s, e) => { sair.Stop(); _saindo = true; };
        sair.Start();
    }

    private void Animar()
    {
        if (!_saindo)
        {
            if (_opacidade < 0.95) { _opacidade += 0.08; Opacity = _opacidade; }
        }
        else
        {
            _opacidade -= 0.08;
            if (_opacidade <= 0)
            {
                _timer.Stop();
                Close();
                return;
            }
            Opacity = _opacidade;
        }
    }

    private void SairAgora() { _saindo = true; }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Desregistrar(this);
        base.OnFormClosed(e);
    }

    private static Color TipoCor(TipoToast t) => t switch
    {
        TipoToast.Sucesso => Tema.CorSucesso,
        TipoToast.Erro => Tema.CorErro,
        TipoToast.Aviso => Tema.CorAlerta,
        _ => Tema.CorInfo
    };

    private static string TipoIcone(TipoToast t) => t switch
    {
        TipoToast.Sucesso => "",
        TipoToast.Erro => "",
        TipoToast.Aviso => "",
        _ => ""
    };

    private static Color Misturar(Color a, Color b, float pct) => Color.FromArgb(
        (int)(a.R * (1 - pct) + b.R * pct),
        (int)(a.G * (1 - pct) + b.G * pct),
        (int)(a.B * (1 - pct) + b.B * pct));

    private static readonly List<Toast> _ativos = new();
    private static void Registrar(Toast t) { lock (_ativos) _ativos.Add(t); }
    private static void Desregistrar(Toast t)
    {
        lock (_ativos) _ativos.Remove(t);
        ReposicionarTodos();
    }
    private static int DeslocamentoOcupados()
    {
        lock (_ativos) return _ativos.Count * 90;
    }
    private static void ReposicionarTodos()
    {
        lock (_ativos)
        {
            var screen = Screen.PrimaryScreen!.WorkingArea;
            for (int i = 0; i < _ativos.Count; i++)
            {
                var t = _ativos[i];
                if (t.IsHandleCreated && !t.IsDisposed)
                    t.Location = new Point(screen.Right - t.Width - 20, screen.Bottom - t.Height - 20 - i * 90);
            }
        }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x80; // WS_EX_TOOLWINDOW (não aparece em Alt+Tab)
            return cp;
        }
    }
}
