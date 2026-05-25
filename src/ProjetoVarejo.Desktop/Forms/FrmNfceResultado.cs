using ProjetoVarejo.Desktop.Theme;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Nfce;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmNfceResultado : Form
{
    private readonly NotaFiscal _nota;
    private readonly EmpresaConfig _empresa;
    private readonly Venda _venda;

    public FrmNfceResultado(NotaFiscal nota, EmpresaConfig empresa, Venda venda)
    {
        _nota = nota; _empresa = empresa; _venda = venda;
        InitUi();
    }

    private void InitUi()
    {
        Text = $"NFC-e #{_nota.Numero}";
        Size = new Size(700, 800);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Tema.CorFundo;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;
        Padding = new Padding(Tema.EspacamentoGrande);

        var (corFundo, corTexto, icone, texto) = _nota.Status switch
        {
            StatusNotaFiscal.Autorizada => (Tema.CorSucesso, Tema.Branco, "", "AUTORIZADA"),
            StatusNotaFiscal.Rejeitada => (Tema.CorErro, Tema.Branco, "", "REJEITADA"),
            StatusNotaFiscal.Cancelada => (Tema.CorNeutro, Tema.Branco, "", "CANCELADA"),
            StatusNotaFiscal.Contingencia => (Tema.CorAlerta, Tema.Branco, "", "CONTINGÊNCIA"),
            _ => (Tema.CorTextoMedio, Tema.Branco, "", _nota.Status.ToString().ToUpper())
        };

        // === Header status ===
        var headerCard = new Card { Dock = DockStyle.Top, Height = 110, Padding = new Padding(0) };
        headerCard.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(3, 3, headerCard.Width - 7, headerCard.Height - 7);
            using var path = Tema.PathArredondado(rect, Tema.RaioCard);
            using var brush = new SolidBrush(corFundo);
            g.FillPath(brush, path);
        };

        var pnlH = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(25, 20, 25, 20) };
        var lblIcone = new Label
        {
            Text = icone, Dock = DockStyle.Left, Width = 90,
            Font = new Font("Segoe MDL2 Assets", 42),
            ForeColor = corTexto,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent
        };
        var pnlInfo = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        pnlInfo.Controls.Add(new Label
        {
            Text = texto, Dock = DockStyle.Top, Height = 36,
            Font = new Font(Tema.FontFamily, 22, FontStyle.Bold),
            ForeColor = corTexto, BackColor = Color.Transparent
        });
        pnlInfo.Controls.Add(new Label
        {
            Text = $"Número {_nota.Numero}  •  Série {_nota.Serie}" + (_nota.Protocolo != null ? $"  •  Protocolo {_nota.Protocolo}" : ""),
            Dock = DockStyle.Top, Height = 22,
            Font = Tema.FontCorpo,
            ForeColor = Color.FromArgb(220, 230, 245),
            BackColor = Color.Transparent
        });
        pnlH.Controls.Add(pnlInfo);
        pnlH.Controls.Add(lblIcone);
        headerCard.Controls.Add(pnlH);

        // === Detalhes ===
        var cardDetalhes = new Card { Dock = DockStyle.Top, Height = 200, Padding = new Padding(20) };
        var pnlD = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };
        int y = 0;
        Adicionar(pnlD, "CHAVE DE ACESSO", FormatarChave(_nota.ChaveAcesso ?? ""), ref y, mono: true);
        if (_nota.AutorizadaEm.HasValue)
            Adicionar(pnlD, "AUTORIZADA EM", _nota.AutorizadaEm.Value.ToString("dd/MM/yyyy HH:mm:ss"), ref y);
        Adicionar(pnlD, "MENSAGEM SEFAZ", _nota.MensagemSefaz ?? "(sem mensagem)", ref y);
        cardDetalhes.Controls.Add(pnlD);

        // === QR Code (se autorizada) ===
        if (_nota.Status == StatusNotaFiscal.Autorizada && !string.IsNullOrWhiteSpace(_nota.ChaveAcesso))
        {
            var cardQr = new Card { Dock = DockStyle.Fill, Padding = new Padding(20) };
            var pnlQ = new Panel { Dock = DockStyle.Fill, BackColor = Tema.CorCard };

            try
            {
                var url = QrCodeNfce.GerarUrl(
                    _nota.ChaveAcesso, _empresa.AmbienteHomologacao,
                    _empresa.CscId, _empresa.CscToken,
                    _nota.AutorizadaEm, _venda.Total);
                var png = QrCodeNfce.GerarImagemPng(url, 6);
                using var ms = new MemoryStream(png);
                var img = Image.FromStream(ms);

                var lblTituloQr = new Label
                {
                    Text = "QR Code para consulta na SEFAZ",
                    Dock = DockStyle.Top, Height = 30,
                    Font = Tema.FontSubtitulo,
                    ForeColor = Tema.CorTextoEscuro,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                var pic = new PictureBox
                {
                    Image = new Bitmap(img),
                    SizeMode = PictureBoxSizeMode.AutoSize,
                    Dock = DockStyle.Top,
                    Anchor = AnchorStyles.None
                };
                var pnlPic = new Panel { Dock = DockStyle.Top, Height = pic.Height + 10, BackColor = Tema.CorCard };
                pic.Left = (pnlPic.Width - pic.Width) / 2;
                pnlPic.Resize += (s, e) => pic.Left = (pnlPic.Width - pic.Width) / 2;
                pnlPic.Controls.Add(pic);
                var lblConsulta = new Label
                {
                    Text = QrCodeNfce.UrlConsulta(_empresa.AmbienteHomologacao),
                    Dock = DockStyle.Top, Height = 22,
                    Font = new Font(Tema.FontFamilyMono, 9),
                    ForeColor = Tema.CorTextoMedio,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                pnlQ.Controls.Add(lblConsulta);
                pnlQ.Controls.Add(pnlPic);
                pnlQ.Controls.Add(lblTituloQr);
            }
            catch (Exception ex)
            {
                pnlQ.Controls.Add(new Label { Text = "Falha ao gerar QR Code: " + ex.Message, Dock = DockStyle.Fill, Font = Tema.FontCorpo, ForeColor = Tema.CorErro });
            }
            cardQr.Controls.Add(pnlQ);
            Controls.Add(cardQr);
        }

        var btnFechar = Botoes.Primario("Fechar", 200, 44);
        btnFechar.Dock = DockStyle.Bottom;
        btnFechar.Click += (s, e) => Close();

        Controls.Add(btnFechar);
        Controls.Add(cardDetalhes);
        Controls.Add(headerCard);
    }

    private static void Adicionar(Control parent, string label, string valor, ref int y, bool mono = false)
    {
        parent.Controls.Add(new Label
        {
            Text = label,
            Left = 0, Top = y, Width = 200, Height = 16,
            Font = new Font(Tema.FontFamily, 8, FontStyle.Bold),
            ForeColor = Tema.CorTextoMedio,
            BackColor = Color.Transparent
        });
        parent.Controls.Add(new Label
        {
            Text = valor,
            Left = 0, Top = y + 18, Width = 620, Height = 24,
            Font = mono ? new Font(Tema.FontFamilyMono, 10) : Tema.FontCorpo,
            ForeColor = Tema.CorTextoEscuro,
            BackColor = Color.Transparent
        });
        y += 50;
    }

    private static string FormatarChave(string ch)
    {
        if (string.IsNullOrEmpty(ch) || ch.Length != 44) return ch;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < ch.Length; i += 4)
            sb.Append(ch.Substring(i, Math.Min(4, ch.Length - i))).Append(' ');
        return sb.ToString().Trim();
    }
}
