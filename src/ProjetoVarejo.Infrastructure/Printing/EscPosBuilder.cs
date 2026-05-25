using System.Text;

namespace ProjetoVarejo.Infrastructure.Printing;

public class EscPosBuilder
{
    static EscPosBuilder()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private readonly List<byte> _buffer = new();
    private readonly Encoding _encoding = Encoding.GetEncoding(850);

    private const byte ESC = 0x1B;
    private const byte GS = 0x1D;
    private const byte LF = 0x0A;

    public EscPosBuilder()
    {
        _buffer.AddRange(new byte[] { ESC, (byte)'@' });
        _buffer.AddRange(new byte[] { ESC, (byte)'t', 2 });
    }

    public EscPosBuilder Texto(string s) { _buffer.AddRange(_encoding.GetBytes(s)); return this; }
    public EscPosBuilder Linha(string s = "") { Texto(s); _buffer.Add(LF); return this; }
    public EscPosBuilder Pular(int n = 1) { for (int i = 0; i < n; i++) _buffer.Add(LF); return this; }
    public EscPosBuilder Esquerda() { _buffer.AddRange(new byte[] { ESC, (byte)'a', 0 }); return this; }
    public EscPosBuilder Centro() { _buffer.AddRange(new byte[] { ESC, (byte)'a', 1 }); return this; }
    public EscPosBuilder Direita() { _buffer.AddRange(new byte[] { ESC, (byte)'a', 2 }); return this; }
    public EscPosBuilder Negrito(bool on) { _buffer.AddRange(new byte[] { ESC, (byte)'E', (byte)(on ? 1 : 0) }); return this; }
    public EscPosBuilder Sublinhado(bool on) { _buffer.AddRange(new byte[] { ESC, (byte)'-', (byte)(on ? 1 : 0) }); return this; }
    public EscPosBuilder DupSize(bool largura, bool altura)
    {
        byte v = 0;
        if (largura) v |= 0x20;
        if (altura) v |= 0x01;
        _buffer.AddRange(new byte[] { GS, (byte)'!', v });
        return this;
    }
    public EscPosBuilder TamanhoNormal() => DupSize(false, false);
    public EscPosBuilder Separador(int colunas = 48) { Linha(new string('-', colunas)); return this; }

    public EscPosBuilder ColunaDupla(string esq, string dir, int colunas = 48)
    {
        var espacos = Math.Max(1, colunas - esq.Length - dir.Length);
        Linha(esq + new string(' ', espacos) + dir);
        return this;
    }

    public EscPosBuilder QrCode(string data, byte tamanho = 6)
    {
        var len = data.Length + 3;
        var pL = (byte)(len % 256);
        var pH = (byte)(len / 256);

        _buffer.AddRange(new byte[] { GS, (byte)'(', (byte)'k', 4, 0, 49, 65, 50, 0 });
        _buffer.AddRange(new byte[] { GS, (byte)'(', (byte)'k', 3, 0, 49, 67, tamanho });
        _buffer.AddRange(new byte[] { GS, (byte)'(', (byte)'k', 3, 0, 49, 69, 49 });
        _buffer.AddRange(new byte[] { GS, (byte)'(', (byte)'k', pL, pH, 49, 80, 48 });
        _buffer.AddRange(_encoding.GetBytes(data));
        _buffer.AddRange(new byte[] { GS, (byte)'(', (byte)'k', 3, 0, 49, 81, 48 });
        return this;
    }

    public EscPosBuilder Cortar() { _buffer.AddRange(new byte[] { GS, (byte)'V', 1 }); return this; }
    public EscPosBuilder AbrirGaveta() { _buffer.AddRange(new byte[] { ESC, (byte)'p', 0, 25, (byte)250 }); return this; }

    public byte[] Build() => _buffer.ToArray();
}
