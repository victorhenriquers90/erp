using System.Drawing.Drawing2D;

namespace ProjetoVarejo.Desktop.Theme;

public enum TipoBadge { Sucesso, Erro, Alerta, Info, Neutro, Primaria }

/// <summary>
/// Chip/pill colorido para indicar status. Ex: "Aprovado", "Pendente".
/// </summary>
public class Badge : Label
{
    private TipoBadge _tipo = TipoBadge.Neutro;

    public TipoBadge Tipo
    {
        get => _tipo;
        set { _tipo = value; AplicarCor(); Invalidate(); }
    }

    public Badge()
    {
        AutoSize = false;
        TextAlign = ContentAlignment.MiddleCenter;
        Font = Tema.FontPequenaBold;
        Height = 22;
        Width = 88;
        Padding = new Padding(8, 0, 8, 0);
        BackColor = Color.Transparent;
        AplicarCor();
    }

    private void AplicarCor()
    {
        ForeColor = _tipo switch
        {
            TipoBadge.Sucesso => Tema.CorSucesso,
            TipoBadge.Erro => Tema.CorErro,
            TipoBadge.Alerta => Tema.CorAlerta,
            TipoBadge.Info => Tema.CorInfo,
            TipoBadge.Primaria => Tema.CorPrimaria,
            _ => Tema.CorTextoMedio
        };
    }

    private Color FundoSoft => _tipo switch
    {
        TipoBadge.Sucesso => Tema.CorSucessoSoft,
        TipoBadge.Erro => Tema.CorErroSoft,
        TipoBadge.Alerta => Tema.CorAlertaSoft,
        TipoBadge.Info => Tema.CorInfoSoft,
        TipoBadge.Primaria => Tema.CorPrimariaSoft,
        _ => Tema.CorNeutroSoft
    };

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Tema.PathArredondado(rect, Tema.RaioBadge);
        using var brush = new SolidBrush(FundoSoft);
        g.FillPath(brush, path);
        using var pen = new Pen(Color.FromArgb(35, ForeColor), 1);
        g.DrawPath(pen, path);

        TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }

    protected override void OnPaintBackground(PaintEventArgs e) { /* transparente */ }

    // Factory helpers
    public static Badge De(string texto, TipoBadge tipo) => new() { Text = texto, Tipo = tipo };
}
