# AGENTS.md

## Cursor Cloud specific instructions

### What this repo is

`ProjetoVarejo` is a .NET 8 retail/PDV ERP (Portuguese/Brazilian). Projects under `src/`:

- `ProjetoVarejo.Api` — ASP.NET Core minimal-API **and** the web app: it hosts the
  Blazor WebAssembly client (`ProjetoVarejo.Web`) at the same origin. This is the main
  runnable service on Linux.
- `ProjetoVarejo.Web` — Blazor WASM front-end (served by the API).
- `ProjetoVarejo.Domain` / `Application` / `Infrastructure` / `Shared` — class libraries.
- `ProjetoVarejo.Desktop` — **WinForms (`net8.0-windows`); does NOT build/run on Linux.**
- `tests/ProjetoVarejo.Tests` — xUnit tests (unit + `WebApplicationFactory` integration).

### Building (Linux)

- Build the API project, **not** the solution: `dotnet build src/ProjetoVarejo.Api/ProjetoVarejo.Api.csproj`.
  Building `ProjetoVarejo.sln` fails because it includes the Windows-only Desktop project.
- Tests: `dotnet build tests/ProjetoVarejo.Tests/ProjetoVarejo.Tests.csproj`.

### Database / services

- The API uses **SQL Server**. There is no Linux-friendly default: `appsettings.json` ships a
  Windows connection string (`Server=.\SQLEXPRESS;Trusted_Connection=True`). Always override
  `ConnectionStrings__Default` on Linux. A standalone SQL Server 2022 container works:
  `Server=localhost;Database=ProjetoVarejo;User Id=sa;Password=<pwd>;TrustServerCertificate=True;Encrypt=False`.
  (Keep `Database=ProjetoVarejo` in the string — startup string-replaces it with `master` to create the DB.)
- `docker-compose.yml` exists but builds a *production* image (`ASPNETCORE_ENVIRONMENT=Production`);
  for development run the API with `dotnet run` against a standalone SQL Server container instead.
- Redis is **disabled by default** (`appsettings.json` `Redis:Enabled=false`) and is optional.

### Running the API (development)

```
ASPNETCORE_ENVIRONMENT=Development \
ConnectionStrings__Default="Server=localhost;Database=ProjetoVarejo;User Id=sa;Password=<pwd>;TrustServerCertificate=True;Encrypt=False" \
ASPNETCORE_URLS="http://0.0.0.0:5094" \
dotnet run --project src/ProjetoVarejo.Api/ProjetoVarejo.Api.csproj
```

Then: web app at `http://localhost:5094/`, Swagger at `/swagger`, status at `/api/status`.

### Non-obvious caveats

- **15s startup delay:** `Program.cs` `EnsureDatabaseCreatedAsync` always `Task.Delay(15000)`s
  before initializing the DB, on every boot. The app only starts serving after that. This also
  makes the `WebApplicationFactory` integration tests slow (each test class boots the app).
- **Placeholder-key guard:** in any non-`Development` environment the app calls
  `Environment.Exit(1)` if JWT/API keys are placeholders. Integration tests boot the app as the
  `Testing` environment, so to run the test suite without the host crashing you must export
  `Jwt__SecretKey` (≥32 chars, not containing `sua-chave-secreta` or `${`) and
  `ApiKeys__0` (not containing `TROQUE-ESTA`). Also export `ConnectionStrings__Default` pointing
  at a reachable SQL Server: integration tests override the `DbContext` to in-memory SQLite, but
  `Program.cs` still opens the configured connection string at startup, so an unreachable server
  makes every test boot retry for minutes.
- **No admin seed via API:** the API only creates the schema (`EnsureCreated`); it does not seed
  users. For login, insert a `Usuarios` row. Password hash format (`Shared/SenhaHasher.cs`):
  `base64(16-byte salt) + "." + base64(PBKDF2-SHA256, 100000 iterations, 32-byte key)`.
  `PerfilUsuario.Administrador = 1`.
- **Pre-existing (not environment) defects on this branch:**
  - `FornecedorService` is not registered in `Program.cs` DI → `/api/fornecedores` POST/PUT 500s.
  - Some unit tests fail independently of the environment: `TokenServiceTests` (JWT claim-type
    mapping/expiry behavior under `System.IdentityModel.Tokens.Jwt` 8.x) and `EstoqueServiceTests`
    (Moq cannot mock the non-virtual `SessaoApp.UsuarioLogado`).
  - Web login UX quirk: after submitting the login form the main content can render blank for a
    moment; a single page refresh (F5) renders the authenticated dashboard. Login itself works.
