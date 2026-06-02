using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Nfce;
using ProjetoVarejo.Infrastructure.Pix;

namespace ProjetoVarejo.Desktop.Forms;

public class FrmPix : Form
{
    private readonly decimal _valor;
    private readonly EmpresaConfig _empresa;

    public FrmPix(decimal valor, EmpresaConfig empresa)
    {
        _valor = valor;
        _empresa = empresa;
        InitUi();
    }

    private void InitUi()
    {
        Text = "Pagamento via PIX";
        Size = new Size(520, 680);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Color.White;
        MaximizeBox = false; MinimizeBox = false;

        var topo = new Panel { Dock = DockStyle.Top, Height = 80, BackColor = Color.FromArgb(50, 165, 130) };
        topo.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"PIX  —  R$ {_valor:N2}",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.White
        });

        try
        {
            if (string.IsNullOrWhiteSpace(_empresa.PixChave))
            {
                var lblErro = new Label
                {
                    Dock = DockStyle.Fill, Padding = new Padding(20),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Text = "Configure a chave PIX em Configurações → Dados da Empresa",
                    Font = new Font("Segoe UI", 11), ForeColor = Color.Firebrick
                };
                Controls.Add(lblErro);
                Controls.Add(topo);
                return;
            }

            var txid = "V" + DateTime.Now.ToString("yyMMddHHmmss");
            var payload = PixBrCodeBuilder.Gerar(
                _empresa.PixChave,
                string.IsNullOrWhiteSpace(_empresa.PixNomeRecebedor) ? _empresa.RazaoSocial : _empresa.PixNomeRecebedor,
                string.IsNullOrWhiteSpace(_empresa.PixCidade) ? _empresa.Cidade : _empresa.PixCidade,
                _valor,
                txid);

            var png = QrCodeNfce.GerarImagemPng(payload, 10);
            using var ms = new MemoryStream(png);
            var img = Image.FromStream(ms);

            var pic = new PictureBox
            {
                Image = new Bitmap(img),
                SizeMode = PictureBoxSizeMode.AutoSize,
                Dock = DockStyle.Top,
                Anchor = AnchorStyles.None
            };

            var pnlQr = new Panel { Dock = DockStyle.Top, Height = pic.Height + 20, BackColor = Color.White };
            pic.Left = (pnlQr.Width - pic.Width) / 2;
            pic.Top = 10;
            pnlQr.Controls.Add(pic);
            pnlQr.Resize += (s, e) => pic.Left = (pnlQr.Width - pic.Width) / 2;

            var pnlCopia = new Panel { Dock = DockStyle.Top, Height = 130, Padding = new Padding(20, 5, 20, 5) };
            pnlCopia.Controls.Add(new Label { Text = "PIX Copia e Cola:", Left = 0, Top = 0, Width = 200, Font = new Font("Segoe UI", 9, FontStyle.Bold) });
            var txt = new TextBox { Left = 0, Top = 22, Width = 460, Height = 60, Multiline = true, ReadOnly = true, Font = new Font("Consolas", 8), Text = payload, ScrollBars = ScrollBars.Vertical };
            pnlCopia.Controls.Add(txt);
            var btnCopiar = new Button { Text = "Copiar", Left = 0, Top = 90, Width = 230, Height = 30, BackColor = Color.FromArgb(52, 152, 219), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnCopiar.Click += (s, e) => { Clipboard.SetText(payload); MessageBox.Show("Código copiado!", "OK"); };
            pnlCopia.Controls.Add(btnCopiar);

            var btnConfirmar = new Button
            {
                Text = "PAGAMENTO RECEBIDO (confirmar)",
                Dock = DockStyle.Bottom, Height = 60,
                BackColor = Color.FromArgb(39, 174, 96), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold)
            };
            btnConfirmar.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };

            var btnCanc = new Button
            {
                Text = "Cancelar PIX",
                Dock = DockStyle.Bottom, Height = 40,
                BackColor = Color.FromArgb(149, 165, 166), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnCanc.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(pnlCopia);
            Controls.Add(pnlQr);
            Controls.Add(btnCanc);
            Controls.Add(btnConfirmar);
            Controls.Add(topo);
        }
        catch (Exception ex)
        {
            Controls.Add(new Label
            {
                Dock = DockStyle.Fill, Padding = new Padding(20),
                Text = "Erro: " + ex.Message,
                Font = new Font("Segoe UI", 10), ForeColor = Color.Firebrick
            });
            Controls.Add(topo);
        }
    }
}
