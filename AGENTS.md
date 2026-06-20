# AGENTS.md

## Cursor Cloud specific instructions

This is **ProjetoVarejo**, a Brazilian retail/POS system built on **.NET 8 (C#)**. It is a layered
solution (`Domain` → `Application` → `Infrastructure` → `Shared`) consumed by an ASP.NET Core API,
a Blazor WebAssembly web UI, and a Windows-only WinForms desktop client.

### Services & how to run them
- **`ProjetoVarejo.Api`** (required) — ASP.NET Core minimal API; also hosts the Blazor WASM web UI at
  the same origin. Dev run: `dotnet run --project src/ProjetoVarejo.Api --launch-profile http`
  (see `src/ProjetoVarejo.Api/Properties/launchSettings.json`, HTTP port `5094`, Swagger at `/swagger`).
- **SQL Server** (required by the API) — run via Docker; it is NOT installed on the host:
  `docker run -d --name projetovarejo-sqlserver -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=AdminPassword123!" -e "MSSQL_PID=Developer" -p 127.0.0.1:1433:1433 mcr.microsoft.com/mssql/server:2022-latest`.
  On startup the API connects to `master`, creates the `ProjetoVarejo` database, and calls
  `EnsureCreatedAsync()` (no migrations/seed on the API path — `DbInitializer` seeding is only used by
  the desktop client).
- **Redis** (optional) — caching, disabled by default (`Redis:Enabled=false` in `appsettings.json`).
- **`ProjetoVarejo.Desktop`** (WinForms, `net8.0-windows`) — Windows-only, cannot build/run on Linux.

### Non-obvious caveats
- **Do not build the whole solution on Linux.** `ProjetoVarejo.sln` references two projects that do not
  exist on disk (`src/ProjetoVarejo.DesktopShell`, `src/ProjetoVarejo.Desktop.Wpf`), so
  `dotnet restore`/`build` on the `.sln` fails. Restore/build the specific project files instead
  (e.g. `tests/ProjetoVarejo.Tests/ProjetoVarejo.Tests.csproj` restores all 7 existing projects).
- **Connection string override on Linux.** The default `ConnectionStrings:Default` in
  `src/ProjetoVarejo.Api/appsettings.json` points to Windows `.\SQLEXPRESS`. On Linux, override it via
  the environment variable
  `ConnectionStrings__Default=Server=localhost,1433;Database=ProjetoVarejo;User Id=sa;Password=AdminPassword123!;TrustServerCertificate=True;Encrypt=False`.
- **Keep `ASPNETCORE_ENVIRONMENT=Development`.** In non-Development environments the API calls
  `Environment.Exit(1)` if it detects placeholder `Jwt:SecretKey` / `ApiKeys`.
- **Tests use in-memory SQLite**, so the test suite does NOT need SQL Server. Run with
  `dotnet test tests/ProjetoVarejo.Tests/ProjetoVarejo.Tests.csproj`.
- **Docker on this VM:** Docker 29 must run with `fuse-overlayfs` storage driver and the
  `containerd-snapshotter` feature disabled (see `/etc/docker/daemon.json`); start the daemon with
  `sudo dockerd` if it is not already running.

### KNOWN BUILD BLOCKER (branch `modernization/foundation`)
This branch is an **incomplete modernization WIP and currently does not compile.** Commit `b3bf926`
added references without their definitions:
- `src/ProjetoVarejo.Domain/Entities/Usuario.cs` references a `Filial` entity that is not defined
  anywhere (also referenced by `AppDbContext`, `UnitOfWork`, `DbInitializer`, and the migration snapshot).
- `src/ProjetoVarejo.Web` references the missing `ProjetoVarejo.Web.Shared` namespace, plus
  `JwtAuthStateProvider` and a `RedirectToLogin` component that are not defined.

Until these missing types are added, the API, the Web UI, and the test suite cannot be built or run.
This is a source-code defect, not an environment/dependency problem — the toolchain (.NET 8 SDK),
Docker, and SQL Server are all set up and working.
