using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace ProjetoVarejo.Desktop.Wpf.Licensing;

public sealed class LicenseInfo
{
    public bool Valida { get; init; }
    public string Motivo { get; init; } = "";
    public string Cliente { get; init; } = "";
    public string Tipo { get; init; } = "";
    public DateTime? Expira { get; init; }
}

/// <summary>
/// Licenciamento offline: a licença é uma chave assinada (RSA) presa ao
/// "código da máquina" (fingerprint). O app só tem a chave PÚBLICA (verifica).
/// </summary>
public static class LicenseService
{
    public static string CaminhoLicenca => Path.Combine(AppContext.BaseDirectory, "licenca.key");
    private static string CaminhoFingerprint => Path.Combine(AppContext.BaseDirectory, "fingerprint.txt");

    /// <summary>Código estável da máquina (MachineGuid + nome), formatado em grupos.</summary>
    public static string ObterFingerprint()
    {
        string guid = "";
        try
        {
            guid = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography", "MachineGuid", "")?.ToString() ?? "";
        }
        catch { /* fallback abaixo */ }

        var bruto = $"{guid}|{Environment.MachineName}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(bruto));
        var hex = Convert.ToHexString(hash)[..16]; // 16 chars
        return $"{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}";
    }

    public static LicenseInfo Validar()
    {
        var fp = ObterFingerprint();
        try { File.WriteAllText(CaminhoFingerprint, fp); } catch { /* best-effort p/ suporte */ }

        if (!File.Exists(CaminhoLicenca))
            return new LicenseInfo { Valida = false, Motivo = "Nenhuma licença encontrada nesta máquina." };

        var token = File.ReadAllText(CaminhoLicenca).Trim();
        return ValidarToken(token, fp);
    }

    /// <summary>Valida um token e, se OK, grava em licenca.key. Usado pela tela de ativação.</summary>
    public static LicenseInfo Ativar(string token)
    {
        var info = ValidarToken(token.Trim(), ObterFingerprint());
        if (info.Valida)
            File.WriteAllText(CaminhoLicenca, token.Trim());
        return info;
    }

    private static LicenseInfo ValidarToken(string token, string fingerprintAtual)
    {
        try
        {
            var partes = token.Split('.');
            if (partes.Length != 2)
                return new LicenseInfo { Valida = false, Motivo = "Formato de chave inválido." };

            var payloadBytes = Convert.FromBase64String(partes[0]);
            var assinatura = Convert.FromBase64String(partes[1]);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(ChavePublicaB64()), out _);
            var ok = rsa.VerifyData(payloadBytes, assinatura, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            if (!ok)
                return new LicenseInfo { Valida = false, Motivo = "Assinatura da licença inválida (chave adulterada)." };

            var doc = JsonDocument.Parse(payloadBytes);
            var root = doc.RootElement;
            var fp = root.GetProperty("fp").GetString() ?? "";
            var cliente = root.TryGetProperty("cli", out var c) ? c.GetString() ?? "" : "";
            var tipo = root.TryGetProperty("tipo", out var t) ? t.GetString() ?? "" : "";
            var expStr = root.TryGetProperty("exp", out var ex) && ex.ValueKind != JsonValueKind.Null
                ? ex.GetString() : null;

            if (!string.Equals(fp, fingerprintAtual, StringComparison.OrdinalIgnoreCase))
                return new LicenseInfo { Valida = false, Motivo = "Licença emitida para outra máquina." };

            DateTime? expira = null;
            if (!string.IsNullOrWhiteSpace(expStr) && DateTime.TryParse(expStr, out var dt))
            {
                expira = dt;
                if (dt.Date < DateTime.Today)
                    return new LicenseInfo { Valida = false, Motivo = $"Licença expirada em {dt:dd/MM/yyyy}.", Cliente = cliente, Tipo = tipo, Expira = expira };
            }

            return new LicenseInfo { Valida = true, Motivo = "OK", Cliente = cliente, Tipo = tipo, Expira = expira };
        }
        catch (Exception ex)
        {
            return new LicenseInfo { Valida = false, Motivo = "Não foi possível ler a licença: " + ex.Message };
        }
    }

    private static string ChavePublicaB64()
    {
        var asm = Assembly.GetExecutingAssembly();
        var nome = asm.GetManifestResourceNames().First(n => n.EndsWith("chave-publica.key", StringComparison.OrdinalIgnoreCase));
        using var s = asm.GetManifestResourceStream(nome)!;
        using var r = new StreamReader(s);
        return r.ReadToEnd().Trim();
    }
}
