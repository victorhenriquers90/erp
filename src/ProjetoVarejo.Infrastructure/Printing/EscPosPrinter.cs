using System.IO.Ports;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ProjetoVarejo.Infrastructure.Printing;

public enum TipoImpressora
{
    SpoolerWindows = 1,
    Rede = 2,
    Serial = 3,
    Arquivo = 4
}

public class ImpressoraConfig
{
    public TipoImpressora Tipo { get; set; } = TipoImpressora.SpoolerWindows;
    public string Destino { get; set; } = "";
    public int Porta { get; set; } = 9100;
    public int Baud { get; set; } = 9600;
    public int Colunas { get; set; } = 48;
}

public static class EscPosPrinter
{
    public static async Task ImprimirAsync(byte[] dados, ImpressoraConfig cfg)
    {
        switch (cfg.Tipo)
        {
            case TipoImpressora.SpoolerWindows: ImprimirSpooler(dados, cfg.Destino); break;
            case TipoImpressora.Rede: await ImprimirRedeAsync(dados, cfg.Destino, cfg.Porta); break;
            case TipoImpressora.Serial: ImprimirSerial(dados, cfg.Destino, cfg.Baud); break;
            case TipoImpressora.Arquivo: await File.WriteAllBytesAsync(cfg.Destino, dados); break;
        }
    }

    private static async Task ImprimirRedeAsync(byte[] dados, string ip, int porta)
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(ip, porta);
        await using var ns = tcp.GetStream();
        await ns.WriteAsync(dados);
        await ns.FlushAsync();
    }

    private static void ImprimirSerial(byte[] dados, string porta, int baud)
    {
        using var sp = new SerialPort(porta, baud, Parity.None, 8, StopBits.One) { Handshake = Handshake.None, WriteTimeout = 5000 };
        sp.Open();
        sp.Write(dados, 0, dados.Length);
    }

    private static void ImprimirSpooler(byte[] dados, string nomeImpressora)
    {
        if (!RawPrinter.SendBytesToPrinter(nomeImpressora, dados))
            throw new InvalidOperationException("Falha ao enviar para impressora: " + nomeImpressora);
    }

    private static class RawPrinter
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DOCINFOW
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)] public string pDataType;
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPWStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] ref DOCINFOW pDI);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static bool SendBytesToPrinter(string nomeImpressora, byte[] bytes)
        {
            var di = new DOCINFOW { pDocName = "ProjetoVarejo - Cupom", pDataType = "RAW" };
            if (!OpenPrinter(nomeImpressora, out var hPrinter, IntPtr.Zero)) return false;
            try
            {
                if (!StartDocPrinter(hPrinter, 1, ref di)) return false;
                if (!StartPagePrinter(hPrinter)) return false;

                var ptr = Marshal.AllocCoTaskMem(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, ptr, bytes.Length);
                    if (!WritePrinter(hPrinter, ptr, bytes.Length, out _)) return false;
                }
                finally { Marshal.FreeCoTaskMem(ptr); }

                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                return true;
            }
            finally { ClosePrinter(hPrinter); }
        }
    }
}
