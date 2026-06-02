using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Gerador de licenças do Projeto ERP (uso interno do fornecedor — NÃO distribuir ao cliente).
//
//   genkeys                                   -> gera o par de chaves RSA em chave-publica.key / chave-privada.key
//   gen <fingerprint> "<cliente>" [PERPETUA|ANUAL] [dias]
//                                             -> assina e imprime a chave de licenca (usa chave-privada.key)
//
// chave-publica.key  -> embutida no ERP (verifica licencas)
// chave-privada.key  -> fica SOMENTE com voce (assina licencas). Nunca distribua.

var cwd = Directory.GetCurrentDirectory();
var pubPath = Path.Combine(cwd, "chave-publica.key");
var privPath = Path.Combine(cwd, "chave-privada.key");

if (args.Length == 0)
{
    Console.WriteLine("Comandos:");
    Console.WriteLine("  genkeys");
    Console.WriteLine("  gen <fingerprint> \"<cliente>\" [PERPETUA|ANUAL] [dias]");
    return;
}

switch (args[0].ToLowerInvariant())
{
    case "genkeys":
    {
        using var rsa = RSA.Create(2048);
        File.WriteAllText(pubPath, Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()));
        File.WriteAllText(privPath, Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()));
        Console.WriteLine("Chaves geradas:");
        Console.WriteLine("  " + pubPath);
        Console.WriteLine("  " + privPath);
        break;
    }

    case "gen":
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Uso: gen <fingerprint> \"<cliente>\" [PERPETUA|ANUAL] [dias]");
            return;
        }
        if (!File.Exists(privPath))
        {
            Console.WriteLine($"chave-privada.key nao encontrada em {cwd}. Rode 'genkeys' primeiro.");
            return;
        }

        var fingerprint = args[1].Trim();
        var cliente = args[2].Trim();
        var tipo = args.Length > 3 ? args[3].Trim().ToUpperInvariant() : "PERPETUA";
        string? exp = null;
        if (tipo == "ANUAL")
        {
            var dias = args.Length > 4 && int.TryParse(args[4], out var d) ? d : 365;
            exp = DateTime.Today.AddDays(dias).ToString("yyyy-MM-dd");
        }

        var json = JsonSerializer.Serialize(new { fp = fingerprint, cli = cliente, tipo, exp });
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(File.ReadAllText(privPath).Trim()), out _);
        var sig = rsa.SignData(jsonBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var token = Convert.ToBase64String(jsonBytes) + "." + Convert.ToBase64String(sig);
        Console.WriteLine("CHAVE DE LICENCA (envie ao cliente):");
        Console.WriteLine(token);
        break;
    }

    default:
        Console.WriteLine("Comando desconhecido.");
        break;
}
