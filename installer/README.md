# Como gerar o instalador

## Pré-requisitos
- .NET 8 SDK
- [Inno Setup 6+](https://jrsoftware.org/isinfo.php) instalado

## Passo 1 — Publicar os binários

```powershell
powershell -ExecutionPolicy Bypass -File installer\publish.ps1
```

Isso gera `publish/desktop/` e `publish/api/` na raiz do projeto.

## Passo 2 — Compilar o instalador

Opção A — pelo Inno Setup Compiler (GUI):
1. Abra `installer\setup.iss` no Inno Setup
2. Menu **Build → Compile** (F9)
3. Saída: `installer\output\ProjetoVarejo-Setup-1.0.0.exe`

Opção B — linha de comando:
```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\setup.iss
```

## Distribuição

O `.exe` gerado é um instalador único que:
- Pede privilégio de admin (instala em Program Files)
- Verifica .NET 8 Desktop Runtime; abre página de download se não tiver
- Instala desktop + (opcional) API
- Cria atalhos no menu Iniciar e (opcional) área de trabalho
- Inclui desinstalador automático

## Notas

- **Banco de dados não é incluído** no instalador. O usuário precisa ter SQL Server LocalDB ou Express instalado separadamente (ver [docs/INSTALL.md](../docs/INSTALL.md))
- **Certificado A1 e CSC** são configurados no primeiro uso pelo próprio aplicativo
- Para distribuir como ZIP simples, basta empacotar `publish/desktop/` em vez de gerar instalador
